using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

internal readonly record struct LinkSummary(int Created, int Replaced, int Skipped, int Failed)
{
    public int Total => Created + Replaced + Skipped + Failed;
}

internal readonly record struct LinkResult(LinkSummary Summary, int CleanupFailed)
{
    public bool HasErrors => Summary.Failed > 0 || CleanupFailed > 0;
}

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
    public LinkResult LinkDotfiles(
        string repoRoot,
        string userHome,
        string ignoreFileName,
        bool overwrite,
        bool dryRun = false)
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
            _logger.Log(LogLevel.Info, "DRY RUN MODE: No files will be actually linked"u8);
        }

        _logger.Log(LogLevel.Info, $"Starting to link dotfiles from {repoRoot} to {userHome}");
        _logger.Log(LogLevel.Info, $"Using ignore file: {ignoreFileName}");

        // Filter files in the root of the repository
        var ignorePath = Path.Combine(repoRoot, ignoreFileName);
        var ignoreMatcher = LoadIgnoreList(ignorePath);
        _logger.Log(LogLevel.Verbose, $"Loaded {ignoreMatcher.Count} user-defined ignore patterns from {ignorePath}");
        _logger.Log(LogLevel.Verbose, $"Using {_defaultIgnorePatterns.Length} default ignore patterns");

        var operations = new List<LinkOperation>();
        CollectRepositoryRootOperations(repoRoot, userHome, ignoreMatcher, operations);
        CollectHomeOperations(repoRoot, userHome, ignoreMatcher, operations);
        CollectRootOperations(repoRoot, ignoreMatcher, operations);

        if (operations.Count == 0)
        {
            _logger.Log(
                LogLevel.Error,
                $"No linkable dotfiles were found in '{repoRoot}'. Verify the repository path with --root or run the command from the dotfiles repository.");
            return default;
        }

        var validatedOperations = ValidateLinkPlan(repoRoot, operations, overwrite);
        var result = ApplyLinkPlan(validatedOperations, dryRun);

        if (dryRun)
        {
            _logger.Log(LogLevel.Info, "DRY RUN COMPLETED: No files were actually linked"u8);
        }
        else if (result.HasErrors)
        {
            _logger.Log(LogLevel.Info, "Dotfiles linking completed with errors"u8);
        }
        else
        {
            _logger.Log(LogLevel.Info, "Dotfiles linking completed"u8);
        }

        return result;
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
        var totalCount = 0;
        var linkedCount = 0;
        var ignoredCount = 0;

        foreach (var file in fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false))
        {
            totalCount++;
            var relPath = GetRepositoryRelativePath(repoRoot, file);

            if (ShouldIgnorePath(relPath, isDirectory: false, ignoreMatcher))
            {
                ignoredCount++;
                _logger.Log(LogLevel.Verbose, $"  Ignored file: {Path.GetFileName(file)} (matched ignore pattern)");
            }
            else
            {
                linkedCount++;
                var destination = Path.Combine(userHome, Path.GetFileName(file));
                operations.Add(new(file, destination, SourceIsDirectory: false));
            }
        }

        _logger.Log(LogLevel.Verbose, $"Total files in repository root: {totalCount}");
        if (ignoredCount > 0)
        {
            _logger.Log(LogLevel.Info, $"Ignored {ignoredCount} files from repository root based on ignore patterns");
        }

        _logger.Log(LogLevel.Info, $"Found {linkedCount} files to link from repository root directory to {userHome}");
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
            _logger.Log(LogLevel.Info, "Skipping ROOT directory processing on non-Unix platforms"u8);
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
            _logger.Log(LogLevel.Info, $"{srcDir} directory not found: {srcPath}");
            return;
        }

        if (fileSystem.IsSymbolicLink(srcPath))
        {
            throw new InvalidOperationException($"'{srcPath}' must not be a symbolic link.");
        }

        _logger.Log(LogLevel.Info, $"Processing {srcDir} directory: {srcPath}");
        var entries = new List<SourceEntry>();
        var ignoredPaths = new List<string>();
        CollectFiles(repoRoot, srcPath, ignoreMatcher, entries, ignoredPaths);

        if (ignoredPaths.Count > 0)
        {
            _logger.Log(LogLevel.Info, $"Ignoring {ignoredPaths.Count} paths from {srcDir} directory based on ignore patterns:");
            foreach (var path in ignoredPaths)
            {
                _logger.Log(LogLevel.Verbose, $"  Ignored path: {path} (matched ignore pattern)");
            }
        }

        _logger.Log(LogLevel.Info, $"Found {entries.Count} entries to link from {srcDir} directory to {destDir}");

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

    private static LinkSummary CreateSummary(IReadOnlyList<ValidatedLinkOperation> operations)
    {
        var created = 0;
        var replaced = 0;
        var skipped = 0;

        foreach (var operation in operations)
        {
            switch (operation.Disposition)
            {
                case LinkDisposition.Create:
                    created++;
                    break;
                case LinkDisposition.Replace:
                    replaced++;
                    break;
                case LinkDisposition.Skip:
                    skipped++;
                    break;
            }
        }

        return new(created, replaced, skipped, Failed: 0);
    }

    private LinkResult ApplyLinkPlan(IReadOnlyList<ValidatedLinkOperation> operations, bool dryRun)
    {
        if (dryRun)
        {
            foreach (var operation in operations)
            {
                LogLinkOperation(operation);
                _ = LinkFile(operation, dryRun: true);
            }

            return new(CreateSummary(operations), CleanupFailed: 0);
        }

        var appliedOperations = new List<AppliedLinkOperation>(operations.Count);
        List<Exception>? failures = null;
        var created = 0;
        var replaced = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var operation in operations)
        {
            LogLinkOperation(operation);
            if (operation.Disposition == LinkDisposition.Skip)
            {
                _ = LinkFile(operation, dryRun: false);
                skipped++;
                continue;
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(operation.Operation.Target)!;
                _logger.Log(LogLevel.Verbose, $"Ensuring directory exists: {targetDirectory}");
                fileSystem.EnsureDirectory(targetDirectory);

                var appliedOperation = LinkFile(operation, dryRun: false);
                if (appliedOperation is not null)
                {
                    appliedOperations.Add(appliedOperation.Value);
                }

                if (operation.Disposition == LinkDisposition.Create)
                {
                    created++;
                }
                else
                {
                    replaced++;
                }

                _logger.Log(
                    LogLevel.Success,
                    $"Created symbolic link: {PathUtilities.NormalizePath(operation.Operation.Target)} -> {PathUtilities.NormalizePath(operation.Operation.Source)}");
            }
            catch (Exception ex)
            {
                failed++;
                failures ??= [];
                failures.Add(new IOException(
                    $"Failed to link '{PathUtilities.NormalizePath(operation.Operation.Target)}' from " +
                    $"'{PathUtilities.NormalizePath(operation.Operation.Source)}': {ex.Message}",
                    ex));
            }
        }

        var cleanupFailed = 0;
        foreach (var appliedOperation in appliedOperations)
        {
            if (appliedOperation.BackupPath is null)
            {
                continue;
            }

            try
            {
                fileSystem.DeleteBackup(
                    appliedOperation.BackupPath,
                    appliedOperation.Operation.Target);
            }
            catch (Exception ex)
            {
                cleanupFailed++;
                failures ??= [];
                failures.Add(new IOException(
                    $"Failed to remove replacement backup " +
                    $"'{PathUtilities.NormalizePath(appliedOperation.BackupPath)}': {ex.Message}. " +
                    "The created link remains in place; remove the backup manually if appropriate.",
                    ex));
            }
        }

        if (failures is not null)
        {
            foreach (var failure in failures)
            {
                _logger.Log(LogLevel.Error, $"{failure.Message}");
            }
        }

        return new(
            new LinkSummary(created, replaced, skipped, failed),
            cleanupFailed);
    }

    private void LogLinkOperation(ValidatedLinkOperation operation) =>
        _logger.Log(LogLevel.Verbose, $"Linking {operation.Operation.Source} to {operation.Operation.Target}");

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
            if (dryRun)
            {
                _logger.Log(LogLevel.Success, $"[DRY-RUN] Would skip already linked: {normalizedTarget} -> {normalizedSource}");
            }
            else
            {
                _logger.Log(LogLevel.Success, $"Skipping already linked: {normalizedTarget} -> {normalizedSource}");
            }
            return null;
        }

        if (dryRun)
        {
            if (disposition == LinkDisposition.Replace)
            {
                _logger.Log(LogLevel.Verbose, $"[DRY-RUN] Would replace existing target: {normalizedTarget}");
            }

            if (sourceIsDirectory)
            {
                _logger.Log(LogLevel.Success, $"[DRY-RUN] Would create directory symlink: {normalizedTarget} -> {normalizedSource}");
            }
            else
            {
                _logger.Log(LogLevel.Success, $"[DRY-RUN] Would create file symlink: {normalizedTarget} -> {normalizedSource}");
            }
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
                    throw combinedException;
                }
            }

            throw;
        }

        return new(operation, backupPath);
    }

    private string MoveTargetAside(string target)
    {
        var backupPath = target + ".dotfileslinker-backup";
        for (var suffix = 1; fileSystem.PathExists(backupPath); suffix++)
        {
            backupPath = $"{target}.dotfileslinker-backup.{suffix}";
        }

        _logger.Log(LogLevel.Verbose, $"Temporarily moving existing target: {target} -> {backupPath}");
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
                _logger.Log(LogLevel.Verbose, $"Ignore file not found: {ignoreFilePath}");
                return new(Array.Empty<string>());
            }

            var lines = fileSystem.ReadAllLines(ignoreFilePath);
            _logger.Log(LogLevel.Verbose, $"Loaded {lines.Length} lines from ignore file");

            var ignoreMatcher = new GitignoreMatcher(lines);

            // Debug output for each ignored pattern
            foreach (var pattern in lines)
            {
                _logger.Log(LogLevel.Verbose, $"Ignoring pattern: '{pattern}'");
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
