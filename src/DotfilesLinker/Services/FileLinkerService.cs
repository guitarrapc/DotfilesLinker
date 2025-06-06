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
    private static readonly HashSet<string> _defaultIgnorePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common OS specific files
        ".DS_Store",       // macOS
        "._.DS_Store",     // macOS
        "Thumbs.db",       // Windows
        "Desktop.ini",     // Windows
        "desktop.ini",     // Windows
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
    };

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
        var ignorePatterns = LoadIgnoreList(ignorePath);
        _logger.Verbose($"Loaded {ignorePatterns.Count} user-defined ignore patterns from {ignorePath}");
        _logger.Verbose($"Using {_defaultIgnorePatterns.Count} default ignore patterns");

        // Process each directory
        ProcessRepositoryRoot(repoRoot, userHome, ignorePatterns, overwrite, dryRun);
        ProcessHomeDirectory(repoRoot, userHome, ignorePatterns, overwrite, dryRun);
        ProcessRootDirectory(repoRoot, ignorePatterns, overwrite, dryRun);

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
    /// <param name="ignorePatterns">Set of file patterns to ignore.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessRepositoryRoot(string repoRoot, string userHome, HashSet<string> ignorePatterns, bool overwrite, bool dryRun)
    {
        var allFiles = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false).ToList();
        _logger.Verbose($"Total files in repository root: {allFiles.Count}");

        // Filter files based on ignore patterns and default ignore patterns
        var files = new List<string>();
        var ignoredFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var fileName = Path.GetFileName(file);
            if (ShouldIgnoreFile(fileName, ignorePatterns))
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
    /// <param name="ignorePatterns">Set of file patterns to ignore.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessHomeDirectory(string repoRoot, string userHome, HashSet<string> ignorePatterns, bool overwrite, bool dryRun)
    {
        ProcessDirectory(repoRoot, "HOME", userHome, ignorePatterns, overwrite, dryRun);
    }

    /// <summary>
    /// Processes and links files in the ROOT directory (Linux/macOS only).
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="ignorePatterns">Set of file patterns to ignore.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessRootDirectory(string repoRoot, HashSet<string> ignorePatterns, bool overwrite, bool dryRun)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            _logger.Info("Skipping ROOT directory processing on non-Unix platforms");
            return;
        }
        ProcessDirectory(repoRoot, "ROOT", "/", ignorePatterns, overwrite, dryRun);
    }

    /// <summary>
    /// Processes and links files in a specified directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="srcDir">The source directory path.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="ignorePatterns">Set of file patterns to ignore.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private void ProcessDirectory(string repoRoot, string srcDir, string destDir, HashSet<string> ignorePatterns, bool overwrite, bool dryRun)
    {
        var srcPath = Path.Combine(repoRoot, srcDir);
        if (!fileSystem.DirectoryExists(srcPath))
        {
            _logger.Info($"{srcDir} directory not found: {srcPath}");
            return;
        }

        _logger.Info($"Processing {srcDir} directory: {srcPath}");
        var allFiles = fileSystem.EnumerateFiles(srcPath, "*", recursive: true).ToList();

        // Filter files based on ignore patterns
        var files = new List<string>();
        var ignoredFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var fileName = Path.GetFileName(file);
            if (ShouldIgnoreFile(fileName, ignorePatterns))
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
            _logger.Info($"Ignoring {ignoredFiles.Count} files from {srcDir} directory based on ignore patterns:");
            foreach (var file in ignoredFiles)
            {
                _logger.Verbose($"  Ignored file: {file} (matched ignore pattern)");
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
        bool exists = fileSystem.FileExists(target) || fileSystem.DirectoryExists(target);

        if (exists)
        {
            var currentLinkTarget = fileSystem.GetLinkTarget(target);

            // If the target is a symlink and points to the same file, do nothing
            if (currentLinkTarget is not null && PathUtilities.PathEquals(currentLinkTarget, source))
            {
                if (dryRun)
                {
                    _logger.Success($"[DRY-RUN] Would skip already linked: {target} -> {source}");
                }
                else
                {
                    _logger.Success($"Skipping already linked: {target} -> {source}");
                }
                return;
            }

            if (!overwrite)
            {
                _logger.Verbose($"Target {target} exists and overwrite=false, aborting");
                throw new InvalidOperationException($"'{target}' already exists; use --force=y to overwrite.");
            }

            if (dryRun)
            {
                _logger.Verbose($"[DRY-RUN] Would delete existing target: {target}");
            }
            else
            {
                _logger.Verbose($"Deleting existing target: {target}");
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
                    _logger.Success($"[DRY-RUN] Would create directory symlink: {target} -> {source}");
                }
                else
                {
                    _logger.Success($"Creating directory symlink: {target} -> {source}");
                    fileSystem.CreateDirectorySymlink(target, source);
                }
            }
            else
            {
                if (dryRun)
                {
                    _logger.Success($"[DRY-RUN] Would create file symlink: {target} -> {source}");
                }
                else
                {
                    _logger.Success($"Creating file symlink: {target} -> {source}");
                    fileSystem.CreateFileSymlink(target, source);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create symlink from {source} to {target}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether a file should be ignored based on patterns.
    /// </summary>
    /// <param name="fileName">The name of the file to check.</param>
    /// <param name="userIgnorePatterns">User-defined patterns to ignore.</param>
    /// <returns>True if the file should be ignored; otherwise, false.</returns>
    private bool ShouldIgnoreFile(string fileName, HashSet<string> userIgnorePatterns)
    {
        // Check default ignore patterns
        if (_defaultIgnorePatterns.Contains(fileName))
        {
            return true;
        }

        // Check for wildcards in default ignore patterns
        foreach (var pattern in _defaultIgnorePatterns)
        {
            if (pattern.Contains('*') && IsWildcardMatch(fileName, pattern))
            {
                return true;
            }
        }

        // Check user-defined ignore patterns
        if (userIgnorePatterns.Contains(fileName))
        {
            return true;
        }

        // Check for wildcards in user-defined ignore patterns
        foreach (var pattern in userIgnorePatterns)
        {
            if (pattern.Contains('*') && IsWildcardMatch(fileName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Simple wildcard matching for file patterns.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns>True if the file name matches the pattern; otherwise, false.</returns>
    private static bool IsWildcardMatch(string fileName, string pattern)
    {
        // Simple implementation for patterns like "*.bak", ".*.swp"
        if (pattern.StartsWith("*"))
        {
            return fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        }
        else if (pattern.EndsWith("*"))
        {
            return fileName.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        }
        else if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*');
            return fileName.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                   fileName.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var lines = fileSystem.ReadAllLines(ignoreFilePath);
        _logger.Verbose($"Loaded {lines.Length} lines from ignore file");

        var ignoreList = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Debug output for each ignored pattern
        foreach (var pattern in ignoreList)
        {
            _logger.Verbose($"Ignoring pattern: '{pattern}'");
        }

        return ignoreList;
    }
}
