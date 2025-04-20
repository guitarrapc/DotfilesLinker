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

        var files = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false)
            .Where(p => !ignore.Contains(Path.GetFileName(p)));

        _logger.Info($"Linking {files.Count()} files from repository root to home directory");

        // Link files in the root of the repository to $HOME
        foreach (var src in files)
        {
            var dst = Path.Combine(userHome, Path.GetFileName(src));
            _logger.Verbose($"Linking {src} to {dst}");
            LinkFile(src, dst, overwrite);
        }

        // Link files in the HOME directory which starts with '.'
        var homeRoot = Path.Combine(repoRoot, "HOME");
        if (fileSystem.DirectoryExists(homeRoot))
        {
            _logger.Info($"Processing HOME directory: {homeRoot}");
            var homeFiles = fileSystem.EnumerateFiles(homeRoot, "*", recursive: true).ToList();
            _logger.Info($"Found {homeFiles.Count} files to link from HOME directory");

            foreach (var src in homeFiles)
            {
                var rel = Path.GetRelativePath(homeRoot, src);
                var dst = Path.Combine(userHome, rel);

                var dstDir = Path.GetDirectoryName(dst)!;
                _logger.Verbose($"Ensuring directory exists: {dstDir}");
                fileSystem.EnsureDirectory(dstDir);

                _logger.Verbose($"Linking {src} to {dst}");
                LinkFile(src, dst, overwrite);
            }
        }
        else
        {
            _logger.Info($"HOME directory not found: {homeRoot}");
        }

        // Link files in the ROOT directory (Linux/macOS only)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var rootRoot = Path.Combine(repoRoot, "ROOT");
            if (fileSystem.DirectoryExists(rootRoot))
            {
                _logger.Verbose($"Processing ROOT directory: {rootRoot}");
                var rootFiles = fileSystem.EnumerateFiles(rootRoot, "*", recursive: true).ToList();
                _logger.Verbose($"Found {rootFiles.Count} files to link from ROOT directory");

                foreach (var src in rootFiles)
                {
                    var rel = Path.GetRelativePath(rootRoot, src);
                    var dst = Path.Combine("/", rel);

                    var dstDir = Path.GetDirectoryName(dst)!;
                    _logger.Verbose($"Ensuring directory exists: {dstDir}");
                    fileSystem.EnsureDirectory(dstDir);

                    _logger.Verbose($"Linking {src} to {dst}");
                    LinkFile(src, dst, overwrite);
                }
            }
            else
            {
                _logger.Info($"ROOT directory not found: {rootRoot}");
            }
        }
        else
        {
            _logger.Info("Skipping ROOT directory processing on non-Unix platforms");
        }

        _logger.Info("Dotfiles linking completed");
    }

    /*-----------------------------------------------------------
     * private helpers
     *----------------------------------------------------------*/

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
