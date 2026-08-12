using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Provides functionality to link dotfiles from a repository to the user's home directory or system root.
/// </summary>
internal sealed class FileLinkerService(IFileSystem fileSystem, ILogger? logger = null)
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
    public int LinkDotfiles(string repoRoot, string userHome, string ignoreFileName, bool overwrite, bool dryRun = false)
    {
        if (!Path.IsPathRooted(repoRoot))
        {
            repoRoot = Path.GetFullPath(repoRoot);
        }

        if (!Path.IsPathRooted(userHome))
        {
            userHome = Path.GetFullPath(userHome);
        }

        if (PathUtilities.IsSameOrDescendant(userHome, repoRoot))
        {
            throw new InvalidOperationException(
                $"User home '{userHome}' must not be the repository root or one of its descendants.");
        }

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

        var operations = new List<LinkOperation>();
        CollectRepositoryRootOperations(repoRoot, userHome, ignoreMatcher, operations);
        CollectHomeOperations(repoRoot, userHome, ignoreMatcher, operations);
        CollectRootOperations(repoRoot, ignoreMatcher, operations);

        if (operations.Count == 0)
        {
            _logger.Error(
                $"No linkable dotfiles were found in '{repoRoot}'. " +
                "Verify the repository path with --root or run the command from the dotfiles repository.");
            return 0;
        }

        var validatedOperations = ValidateLinkPlan(repoRoot, operations, overwrite);
        ApplyLinkPlan(validatedOperations, dryRun);

        if (dryRun)
        {
            _logger.Info("DRY RUN COMPLETED: No files were actually linked");
        }
        else
        {
            _logger.Info("Dotfiles linking completed");
        }

        return operations.Count;
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
    private void CollectRepositoryRootOperations(
        string repoRoot,
        string userHome,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        var allFiles = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false).ToList();
        _logger.Verbose($"Total files in repository root: {allFiles.Count}");

        // Filter files based on ignore patterns and default ignore patterns
        var files = new List<string>();
        var ignoredFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var relPath = GetRepositoryRelativePath(repoRoot, file);

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
            operations.Add(new(src, dst, SourceIsDirectory: false));
        }
    }

    /// <summary>
    /// Processes and links files in the HOME directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    private void CollectHomeOperations(
        string repoRoot,
        string userHome,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        CollectDirectoryOperations(repoRoot, "HOME", userHome, ignoreMatcher, operations);
    }

    /// <summary>
    /// Processes and links files in the ROOT directory (Linux/macOS only).
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    private void CollectRootOperations(
        string repoRoot,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            _logger.Info("Skipping ROOT directory processing on non-Unix platforms");
            return;
        }
        CollectDirectoryOperations(repoRoot, "ROOT", "/", ignoreMatcher, operations);
    }

    /// <summary>
    /// Processes and links files in a specified directory.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="srcDir">The source directory path.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="ignoreMatcher">Ordered user-defined ignore rules.</param>
    private void CollectDirectoryOperations(
        string repoRoot,
        string srcDir,
        string destDir,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        var srcPath = Path.Combine(repoRoot, srcDir);
        if (!fileSystem.DirectoryExists(srcPath))
        {
            _logger.Info($"{srcDir} directory not found: {srcPath}");
            return;
        }

        if (fileSystem.IsSymbolicLink(srcPath))
        {
            throw new InvalidOperationException($"'{srcPath}' must not be a symbolic link.");
        }

        _logger.Info($"Processing {srcDir} directory: {srcPath}");
        var entries = new List<SourceEntry>();
        var ignoredPaths = new List<string>();
        CollectFiles(repoRoot, srcPath, ignoreMatcher, entries, ignoredPaths);

        if (ignoredPaths.Count > 0)
        {
            _logger.Info($"Ignoring {ignoredPaths.Count} paths from {srcDir} directory based on ignore patterns:");
            foreach (var path in ignoredPaths)
            {
                _logger.Verbose($"  Ignored path: {path} (matched ignore pattern)");
            }
        }

        _logger.Info($"Found {entries.Count} entries to link from {srcDir} directory to {destDir}");

        foreach (var entry in entries)
        {
            var rel = Path.GetRelativePath(srcPath, entry.Path);
            var dst = Path.Combine(destDir, rel);
            operations.Add(new(entry.Path, dst, entry.IsDirectory));
        }
    }

    /// <summary>
    /// Collects linkable files without descending into ignored directories.
    /// </summary>
    private void CollectFiles(
        string repoRoot,
        string sourceRoot,
        GitignoreMatcher ignoreMatcher,
        List<SourceEntry> entries,
        List<string> ignoredPaths)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(sourceRoot);

        while (pendingDirectories.TryPop(out var currentDirectory))
        {
            foreach (var directory in fileSystem.EnumerateDirectories(currentDirectory))
            {
                var relativePath = GetRepositoryRelativePath(repoRoot, directory);
                if (ShouldIgnorePath(relativePath, isDirectory: true, ignoreMatcher))
                {
                    ignoredPaths.Add(directory);
                    continue;
                }

                // Preserve directory links as links without traversing their targets. This also
                // prevents junctions, external targets, and link cycles from escaping the repository.
                if (fileSystem.IsSymbolicLink(directory))
                {
                    entries.Add(new(directory, IsDirectory: true));
                    continue;
                }

                pendingDirectories.Push(directory);
            }

            foreach (var file in fileSystem.EnumerateFiles(currentDirectory, "*", recursive: false))
            {
                var relativePath = GetRepositoryRelativePath(repoRoot, file);
                if (ShouldIgnorePath(relativePath, isDirectory: false, ignoreMatcher))
                {
                    ignoredPaths.Add(file);
                }
                else
                {
                    entries.Add(new(file, IsDirectory: false));
                }
            }
        }
    }

    /// <summary>
    /// Converts a source path to the single namespace used by all ignore rules.
    /// HOME and ROOT remain the first path segment so pattern bases never change by source directory.
    /// </summary>
    private static string GetRepositoryRelativePath(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path);

    private static void ValidateLinkPaths(string repoRoot, string source, string target)
    {
        if (PathUtilities.PathEquals(source, target))
        {
            throw new InvalidOperationException(
                $"Source and destination resolve to the same path: '{source}'.");
        }

        if (PathUtilities.PathsOverlap(repoRoot, target))
        {
            throw new InvalidOperationException(
                $"Destination '{target}' overlaps dotfiles repository '{repoRoot}'.");
        }
    }

    private List<ValidatedLinkOperation> ValidateLinkPlan(
        string repoRoot,
        IReadOnlyList<LinkOperation> operations,
        bool overwrite)
    {
        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            ValidateLinkPaths(repoRoot, operation.Source, operation.Target);

            for (var previousIndex = 0; previousIndex < index; previousIndex++)
            {
                var previous = operations[previousIndex];
                if (PathUtilities.PathsOverlap(previous.Target, operation.Target))
                {
                    throw new InvalidOperationException(
                        $"Destinations '{previous.Target}' and '{operation.Target}' overlap.");
                }
            }
        }

        var validatedOperations = new List<ValidatedLinkOperation>(operations.Count);
        foreach (var operation in operations)
        {
            var disposition = LinkDisposition.Create;
            if (fileSystem.PathExists(operation.Target))
            {
                var currentLinkTarget = fileSystem.GetLinkTarget(operation.Target);
                if (currentLinkTarget is not null &&
                    PathUtilities.LinkTargetEquals(operation.Target, currentLinkTarget, operation.Source))
                {
                    disposition = LinkDisposition.Skip;
                }
                else if (overwrite)
                {
                    disposition = LinkDisposition.Replace;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"'{PathUtilities.NormalizePath(operation.Target)}' already exists; use --force to overwrite.");
                }
            }

            validatedOperations.Add(new(operation, disposition));
        }

        return validatedOperations;
    }

    private void ApplyLinkPlan(IReadOnlyList<ValidatedLinkOperation> operations, bool dryRun)
    {
        if (dryRun)
        {
            foreach (var operation in operations)
            {
                LogLinkOperation(operation);
                _ = LinkFile(operation, dryRun: true);
            }

            return;
        }

        foreach (var operation in operations)
        {
            if (operation.Disposition != LinkDisposition.Skip)
            {
                var targetDirectory = Path.GetDirectoryName(operation.Operation.Target)!;
                _logger.Verbose($"Ensuring directory exists: {targetDirectory}");
                fileSystem.EnsureDirectory(targetDirectory);
            }
        }

        var appliedOperations = new List<AppliedLinkOperation>(operations.Count);
        try
        {
            foreach (var operation in operations)
            {
                LogLinkOperation(operation);
                var appliedOperation = LinkFile(operation, dryRun: false);
                if (appliedOperation is not null)
                {
                    appliedOperations.Add(appliedOperation.Value);
                }
            }
        }
        catch (Exception applyException)
        {
            try
            {
                RollbackAppliedOperations(appliedOperations);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "Failed to apply the link plan and roll back earlier operations.",
                    applyException,
                    rollbackException);
            }

            throw;
        }

        try
        {
            foreach (var appliedOperation in appliedOperations)
            {
                if (appliedOperation.BackupPath is not null)
                {
                    fileSystem.Delete(appliedOperation.BackupPath);
                }
            }
        }
        catch (Exception cleanupException)
        {
            try
            {
                RollbackAppliedOperations(appliedOperations);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "Failed to remove replacement backups and roll back the link plan.",
                    cleanupException,
                    rollbackException);
            }

            throw new IOException(
                "Failed to remove a replacement backup; the link plan was rolled back.",
                cleanupException);
        }

        foreach (var appliedOperation in appliedOperations)
        {
            var operation = appliedOperation.Operation;
            _logger.Success(
                $"Created symbolic link: {PathUtilities.NormalizePath(operation.Target)} -> " +
                PathUtilities.NormalizePath(operation.Source));
        }
    }

    private void LogLinkOperation(ValidatedLinkOperation operation) =>
        _logger.Verbose($"Linking {operation.Operation.Source} to {operation.Operation.Target}");

    /// <summary>
    /// Creates a symbolic link from the source to the target path.
    /// </summary>
    /// <param name="validatedOperation">The validated operation to apply.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
    private AppliedLinkOperation? LinkFile(ValidatedLinkOperation validatedOperation, bool dryRun)
    {
        var (operation, disposition) = validatedOperation;
        var (source, target, sourceIsDirectory) = operation;

        // Normalize paths for cross-platform consistency in logs
        string normalizedSource = PathUtilities.NormalizePath(source);
        string normalizedTarget = PathUtilities.NormalizePath(target);

        if (disposition == LinkDisposition.Skip)
        {
            _logger.Success(dryRun
                ? $"[DRY-RUN] Would skip already linked: {normalizedTarget} -> {normalizedSource}"
                : $"Skipping already linked: {normalizedTarget} -> {normalizedSource}");
            return null;
        }

        if (dryRun)
        {
            if (disposition == LinkDisposition.Replace)
            {
                _logger.Verbose($"[DRY-RUN] Would replace existing target: {normalizedTarget}");
            }

            _logger.Success(sourceIsDirectory
                ? $"[DRY-RUN] Would create directory symlink: {normalizedTarget} -> {normalizedSource}"
                : $"[DRY-RUN] Would create file symlink: {normalizedTarget} -> {normalizedSource}");
            return null;
        }

        var backupPath = disposition == LinkDisposition.Replace
            ? MoveTargetAside(target)
            : null;

        try
        {
            if (sourceIsDirectory)
            {
                fileSystem.CreateDirectorySymlink(target, source);
            }
            else
            {
                fileSystem.CreateFileSymlink(target, source);
            }
        }
        catch (Exception ex)
        {
            if (backupPath is not null)
            {
                try
                {
                    RestoreMovedTarget(target, backupPath);
                }
                catch (Exception rollbackException)
                {
                    var combinedException = new AggregateException(
                        $"Failed to create symlink and restore the original target '{normalizedTarget}'. " +
                        $"The original entry may remain at '{PathUtilities.NormalizePath(backupPath)}'.",
                        ex,
                        rollbackException);
                    _logger.Error(combinedException.Message);
                    throw combinedException;
                }
            }

            _logger.Error($"Failed to create symlink from {normalizedSource} to {normalizedTarget}: {ex.Message}");
            throw;
        }

        return new(operation, backupPath);
    }

    private void RollbackAppliedOperations(IReadOnlyList<AppliedLinkOperation> operations)
    {
        List<Exception>? rollbackExceptions = null;
        for (var index = operations.Count - 1; index >= 0; index--)
        {
            var operation = operations[index];
            try
            {
                if (fileSystem.PathExists(operation.Operation.Target))
                {
                    fileSystem.Delete(operation.Operation.Target);
                }

                if (operation.BackupPath is not null)
                {
                    fileSystem.Move(operation.BackupPath, operation.Operation.Target);
                }
            }
            catch (Exception ex)
            {
                rollbackExceptions ??= [];
                rollbackExceptions.Add(new IOException(
                    $"Failed to roll back destination '{operation.Operation.Target}'.",
                    ex));
            }
        }

        if (rollbackExceptions is not null)
        {
            throw new AggregateException(rollbackExceptions);
        }
    }

    private string MoveTargetAside(string target)
    {
        var backupPath = target + ".dotfileslinker-backup";
        for (var suffix = 1; fileSystem.PathExists(backupPath); suffix++)
        {
            backupPath = $"{target}.dotfileslinker-backup.{suffix}";
        }

        _logger.Verbose($"Temporarily moving existing target: {target} -> {backupPath}");
        fileSystem.Move(target, backupPath);
        return backupPath;
    }

    private void RestoreMovedTarget(string target, string backupPath)
    {
        if (fileSystem.PathExists(target))
        {
            fileSystem.Delete(target);
        }

        fileSystem.Move(backupPath, target);
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
        try
        {
            if (!fileSystem.PathExists(ignoreFilePath))
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
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Failed to load ignore file '{ignoreFilePath}'.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Failed to load ignore file '{ignoreFilePath}'.", ex);
        }
    }

    private readonly record struct SourceEntry(string Path, bool IsDirectory);

    private readonly record struct LinkOperation(string Source, string Target, bool SourceIsDirectory);

    private readonly record struct ValidatedLinkOperation(
        LinkOperation Operation,
        LinkDisposition Disposition);

    private readonly record struct AppliedLinkOperation(
        LinkOperation Operation,
        string? BackupPath);

    private enum LinkDisposition
    {
        Create,
        Replace,
        Skip
    }
}
