using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Provides functionality to link dotfiles from a repository to the user's home directory or system root.
/// </summary>
public sealed class FileLinkerService(IFileSystem fileSystem, ILogger? logger = null)
{
    private readonly ILogger _logger = logger ?? new NullLogger();

    // Default patterns to ignore in all directories, common for all platforms
    private static readonly string[] _defaultIgnorePatterns =
    [
        // Common OS specific files
        ".DS_Store",       // macOS
        "._.DS_Store",     // macOS
        "Thumbs.db",       // Windows
        "Desktop.ini",     // Windows
        "ehthumbs.db",     // Windows
        "ehthumbs_vista.db", // Windows

        // Common backup/temporary files
        "*~",              // Linux/Unix backup files
        ".*.swp",          // Vim swap files
        ".*.swo",          // Vim swap files
        "*.bak",           // Backup files
        "*.tmp",           // Temporary files

        // Version control system folders
        ".git",
        ".svn",
        ".hg"
    ];

    private static readonly GitignoreMatcher _defaultIgnoreMatcher = new(_defaultIgnorePatterns);

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
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a target file or directory already exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    public void LinkDotfiles(string repoRoot, string userHome, string ignoreFileName, bool overwrite, bool dryRun = false)
    {
        if (dryRun)
        {
            _logger.Info("DRY RUN MODE: No files will be actually linked");
        }

        _logger.Info($"Starting to link dotfiles from {repoRoot} to {userHome}");
        _logger.Info($"Using ignore file: {ignoreFileName}");

        // Filter files in the root of the repository
        var ignorePath = Path.Combine(repoRoot, ignoreFileName);
        var ignoreMatcher = LoadIgnoreList(ignorePath);
        _logger.Verbose($"Loaded {ignoreMatcher.Count} user-defined ignore patterns from {ignorePath}");
        _logger.Verbose($"Using {_defaultIgnorePatterns.Length} default ignore patterns");

        // Process each directory
        ProcessRepositoryRoot(repoRoot, userHome, ignoreMatcher, overwrite, dryRun);
        ProcessHomeDirectory(repoRoot, userHome, ignoreMatcher, overwrite, dryRun);
        ProcessRootDirectory(repoRoot, ignoreMatcher, overwrite, dryRun);

        if (dryRun)
        {
            _logger.Info("DRY RUN COMPLETED: No files were actually linked");
        }
        else
        {
            _logger.Info("Dotfiles linking completed");
        }
    }

    /*-----------------------------------------------------------
     * private helpers
     *----------------------------------------------------------*/
    /// <summary>
    /// Processes and links files in the repository root.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessRepositoryRoot(string repoRoot, string userHome, GitignoreMatcher ignoreMatcher, bool overwrite, bool dryRun)
    {
        var allFiles = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false).ToList();
        _logger.Verbose($"Total files in repository root: {allFiles.Count}");

        // Filter files based on ignore patterns and default ignore patterns
        var files = new List<string>();
        var ignoredFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var relPath = Path.GetRelativePath(repoRoot, file);

            if (ShouldIgnorePath(relPath, isDirectory: false, ignoreMatcher))
            {
                ignoredFiles.Add(file);
            }
            else
            {
                files.Add(file);
            }
        }

        // Log ignored files
        if (ignoredFiles.Any())
        {
            _logger.Info($"Ignoring {ignoredFiles.Count} files from repository root based on ignore patterns:");
            foreach (var file in ignoredFiles)
            {
                _logger.Verbose($"  Ignored file: {Path.GetFileName(file)} (matched ignore pattern)");
            }
        }

        _logger.Info($"Found {files.Count} files to link from repository root directory to {userHome}");

        foreach (var src in files)
        {
            var dst = Path.Combine(userHome, Path.GetFileName(src));
            _logger.Verbose($"Linking {src} to {dst}");
            LinkFile(src, dst, overwrite, dryRun);
        }
    }

    /// <summary>
    /// Processes and links files in the HOME directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessHomeDirectory(string repoRoot, string userHome, GitignoreMatcher ignoreMatcher, bool overwrite, bool dryRun)
    {
        ProcessDirectory(repoRoot, "HOME", userHome, ignoreMatcher, overwrite, dryRun);
    }

    /// <summary>
    /// Processes and links files in the ROOT directory (Linux/macOS only).
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessRootDirectory(string repoRoot, GitignoreMatcher ignoreMatcher, bool overwrite, bool dryRun)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            _logger.Info("Skipping ROOT directory processing on non-Unix platforms");
            return;
        }
        ProcessDirectory(repoRoot, "ROOT", "/", ignoreMatcher, overwrite, dryRun);
    }

    /// <summary>
    /// Processes and links files in a specified directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="srcDir">The source directory path.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessDirectory(string repoRoot, string srcDir, string destDir, GitignoreMatcher ignoreMatcher, bool overwrite, bool dryRun)
    {
        var srcPath = Path.Combine(repoRoot, srcDir);
        if (!fileSystem.DirectoryExists(srcPath))
        {
            _logger.Info($"{srcDir} directory not found: {srcPath}");
            return;
        }

        _logger.Info($"Processing {srcDir} directory: {srcPath}");
        var files = new List<string>();
        var ignoredPaths = new List<string>();
        CollectFiles(repoRoot, srcPath, ignoreMatcher, files, ignoredPaths);

        if (ignoredPaths.Count > 0)
        {
            _logger.Info($"Ignoring {ignoredPaths.Count} paths from {srcDir} directory based on ignore patterns:");
            foreach (var path in ignoredPaths)
            {
                _logger.Verbose($"  Ignored path: {path} (matched ignore pattern)");
            }
        }

        _logger.Info($"Found {files.Count} files to link from {srcDir} directory to {destDir}");

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(srcPath, file);
            var dst = Path.Combine(destDir, rel);

            var dstDir = Path.GetDirectoryName(dst)!;
            _logger.Verbose($"Ensuring directory exists: {dstDir}");

            // Only actually create the directory if not in dry-run mode
            if (!dryRun)
            {
                fileSystem.EnsureDirectory(dstDir);
            }

            _logger.Verbose($"Linking {file} to {dst}");
            LinkFile(file, dst, overwrite, dryRun);
        }
    }

    /// <summary>
    /// Collects linkable files without descending into ignored directories.
    /// </summary>
    private void CollectFiles(
        string repoRoot,
        string sourceRoot,
        GitignoreMatcher ignoreMatcher,
        List<string> files,
        List<string> ignoredPaths)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(sourceRoot);

        while (pendingDirectories.TryPop(out var currentDirectory))
        {
            foreach (var directory in fileSystem.EnumerateDirectories(currentDirectory))
            {
                var relativePath = Path.GetRelativePath(repoRoot, directory);
                if (ShouldIgnorePath(relativePath, isDirectory: true, ignoreMatcher))
                {
                    ignoredPaths.Add(directory);
                    continue;
                }

                pendingDirectories.Push(directory);
            }

            foreach (var file in fileSystem.EnumerateFiles(currentDirectory, "*", recursive: false))
            {
                var relativePath = Path.GetRelativePath(repoRoot, file);
                if (ShouldIgnorePath(relativePath, isDirectory: false, ignoreMatcher))
                {
                    ignoredPaths.Add(file);
                }
                else
                {
                    files.Add(file);
                }
            }
        }
    }

    /// <summary>
    /// Creates a symbolic link from the source to the target path.
    /// </summary>
    /// <param name="source">The source file or directory path.</param>
    /// <param name="target">The target file or directory path.</param>
    /// <param name="overwrite">Whether to overwrite the target if it already exists.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the target exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    private void LinkFile(string source, string target, bool overwrite, bool dryRun)
    {
        // Normalize paths for cross-platform consistency in logs
        string normalizedSource = PathUtilities.NormalizePath(source);
        string normalizedTarget = PathUtilities.NormalizePath(target);

        bool exists = fileSystem.FileExists(target) || fileSystem.DirectoryExists(target);
        if (exists)
        {
            var currentLinkTarget = fileSystem.GetLinkTarget(target);

            // If the target is a symlink and points to the same file, do nothing
            if (currentLinkTarget is not null && PathUtilities.PathEquals(currentLinkTarget, source))
            {
                if (dryRun)
                {
                    _logger.Success($"[DRY-RUN] Would skip already linked: {normalizedTarget} -> {normalizedSource}");
                }
                else
                {
                    _logger.Success($"Skipping already linked: {normalizedTarget} -> {normalizedSource}");
                }
                return;
            }

            if (!overwrite)
            {
                _logger.Verbose($"Target {normalizedTarget} exists and overwrite=false, aborting");
                throw new InvalidOperationException($"'{normalizedTarget}' already exists; use --force to overwrite.");
            }

            if (dryRun)
            {
                _logger.Verbose($"[DRY-RUN] Would delete existing target: {normalizedTarget}");
            }
            else
            {
                _logger.Verbose($"Deleting existing target: {normalizedTarget}");
                fileSystem.Delete(target);
            }
        }

        // Create the link (or just log what would happen in dry-run mode)
        try
        {
            if (fileSystem.DirectoryExists(source))
            {
                if (dryRun)
                {
                    _logger.Success($"[DRY-RUN] Would create directory symlink: {normalizedTarget} -> {normalizedSource}");
                }
                else
                {
                    _logger.Success($"Creating directory symlink: {normalizedTarget} -> {normalizedSource}");
                    fileSystem.CreateDirectorySymlink(target, source);
                }
            }
            else
            {
                if (dryRun)
                {
                    _logger.Success($"[DRY-RUN] Would create file symlink: {normalizedTarget} -> {normalizedSource}");
                }
                else
                {
                    _logger.Success($"Creating file symlink: {normalizedTarget} -> {normalizedSource}");
                    fileSystem.CreateFileSymlink(target, source);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create symlink from {normalizedSource} to {normalizedTarget}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether a repository-relative path is ignored by a built-in or user rule.
    /// </summary>
    private static bool ShouldIgnorePath(string path, bool isDirectory, GitignoreMatcher userIgnoreMatcher) =>
        _defaultIgnoreMatcher.IsIgnored(path, isDirectory) ||
        userIgnoreMatcher.IsIgnored(path, isDirectory);

    /// <summary>
    /// Loads the ignore list from the specified file.
    /// </summary>
    /// <param name="ignoreFilePath">The path to the ignore file.</param>
    /// <returns>An ordered matcher containing the active rules.</returns>
    private GitignoreMatcher LoadIgnoreList(string ignoreFilePath)
    {
        if (!fileSystem.FileExists(ignoreFilePath))
        {
            _logger.Verbose($"Ignore file not found: {ignoreFilePath}");
            return new(Array.Empty<string>());
        }

        var lines = fileSystem.ReadAllLines(ignoreFilePath);
        _logger.Verbose($"Loaded {lines.Length} lines from ignore file");

        var ignoreMatcher = new GitignoreMatcher(lines);

        // Debug output for each ignored pattern
        foreach (var pattern in lines)
        {
            _logger.Verbose($"Ignoring pattern: '{pattern}'");
        }

        return ignoreMatcher;
    }
}
