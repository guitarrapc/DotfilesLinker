using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Provides functionality to link dotfiles from a repository to the user's home directory or system root.
/// </summary>
public sealed class FileLinkerService(IFileSystem fileSystem, ILogger? logger = null)
{
    private readonly ILogger _logger = logger ?? new NullLogger();

    /*-----------------------------------------------------------
     * public APIs
     *----------------------------------------------------------*/

    /// <summary>
    /// Links dotfiles from the specified repository to the user's home directory or system root.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreFileName">The name of the ignore file containing patterns to exclude.</param>
    /// <param name="overwrite">Whether to overwrite existing files or directories.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a target file or directory already exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    public void LinkDotfiles(string repoRoot, string userHome, string ignoreFileName, bool overwrite)
    {
        _logger.Info($"Starting to link dotfiles from {repoRoot} to {userHome}");
        _logger.Info($"Using ignore file: {ignoreFileName}");

        // Filter files in the root of the repository
        var ignorePath = Path.Combine(repoRoot, ignoreFileName);
        var ignore = LoadIgnoreList(ignorePath);
        _logger.Verbose($"Loaded {ignore.Count} ignore patterns from {ignorePath}");

        // Process each directory
        ProcessRepositoryRoot(repoRoot, userHome, ignore, overwrite);
        ProcessHomeDirectory(repoRoot, userHome, overwrite);
        ProcessRootDirectory(repoRoot, overwrite);

        _logger.Info("Dotfiles linking completed");
    }

    /*-----------------------------------------------------------
     * private helpers
     *----------------------------------------------------------*/

    /// <summary>
    /// Processes and links files in the repository root.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignore">Set of file patterns to ignore.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    private void ProcessRepositoryRoot(string repoRoot, string userHome, HashSet<string> ignore, bool overwrite)
    {
        var files = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false)
            .Where(p => !ignore.Contains(Path.GetFileName(p)));

        _logger.Info($"Linking {files.Count()} files from repository root to home directory");

        foreach (var src in files)
        {
            var dst = Path.Combine(userHome, Path.GetFileName(src));
            _logger.Verbose($"Linking {src} to {dst}");
            LinkFile(src, dst, overwrite);
        }
    }

    /// <summary>
    /// Processes and links files in the HOME directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    private void ProcessHomeDirectory(string repoRoot, string userHome, bool overwrite)
    {
        ProcessDirectory(repoRoot, "HOME", userHome, overwrite);
    }

    /// <summary>
    /// Processes and links files in the ROOT directory (Linux/macOS only).
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    private void ProcessRootDirectory(string repoRoot, bool overwrite)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            _logger.Info("Skipping ROOT directory processing on non-Unix platforms");
            return;
        }
        ProcessDirectory(repoRoot, "ROOT", "/", overwrite);
    }

    /// <summary>
    /// Processes and links files in the HOME directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="srcDir">The source directory path.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    private void ProcessDirectory(string repoRoot, string srcDir, string destDir, bool overwrite)
    {
        var srcPath = Path.Combine(repoRoot, srcDir);
        if (!fileSystem.DirectoryExists(srcPath))
        {
            _logger.Info($"{srcDir} directory not found: {srcPath}");
            return;
        }

        _logger.Info($"Processing {srcDir} directory: {srcPath}");
        var files = fileSystem.EnumerateFiles(srcPath, "*", recursive: true).ToList();
        _logger.Info($"Found {files.Count} files to link from {srcDir} directory");

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(srcPath, file);
            var dst = Path.Combine(destDir, rel);

            var dstDir = Path.GetDirectoryName(dst)!;
            _logger.Verbose($"Ensuring directory exists: {dstDir}");
            fileSystem.EnsureDirectory(dstDir);

            _logger.Verbose($"Linking {file} to {dst}");
            LinkFile(file, dst, overwrite);
        }
    }

    /// <summary>
    /// Creates a symbolic link from the source to the target path.
    /// </summary>
    /// <param name="source">The source file or directory path.</param>
    /// <param name="target">The target file or directory path.</param>
    /// <param name="overwrite">Whether to overwrite the target if it already exists.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the target exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    private void LinkFile(string source, string target, bool overwrite)
    {
        bool exists = fileSystem.FileExists(target) || fileSystem.DirectoryExists(target);

        if (exists)
        {
            var currentLinkTarget = fileSystem.GetLinkTarget(target);

            // If the target is a symlink and points to the same file, do nothing
            if (currentLinkTarget is not null && PathUtilities.PathEquals(currentLinkTarget, source))
            {
                _logger.Success($"Skipping already linked: {target} -> {source}");
                return;
            }

            if (!overwrite)
            {
                _logger.Verbose($"Target {target} exists and overwrite=false, aborting");
                throw new InvalidOperationException($"'{target}' already exists; use --force=y to overwrite.");
            }

            _logger.Verbose($"Deleting existing target: {target}");
            fileSystem.Delete(target);
        }

        // Create the link
        try
        {
            if (fileSystem.DirectoryExists(source))
            {
                _logger.Success($"Creating directory symlink: {target} -> {source}");
                fileSystem.CreateDirectorySymlink(target, source);
            }
            else
            {
                _logger.Success($"Creating file symlink: {target} -> {source}");
                fileSystem.CreateFileSymlink(target, source);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create symlink from {source} to {target}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads the ignore list from the specified file.
    /// </summary>
    /// <param name="ignoreFilePath">The path to the ignore file.</param>
    /// <returns>A set of file or directory names to ignore.</returns>
    private HashSet<string> LoadIgnoreList(string ignoreFilePath)
    {
        if (!fileSystem.FileExists(ignoreFilePath))
        {
            _logger.Verbose($"Ignore file not found: {ignoreFilePath}");
            return new(StringComparer.Ordinal);
        }

        var lines = fileSystem.ReadAllLines(ignoreFilePath);
        _logger.Verbose($"Loaded {lines.Length} lines from ignore file");

        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.Ordinal);
    }
}
