using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Discovers linkable repository entries and validates the complete link plan before mutation.
/// </summary>
internal sealed class LinkPlanBuilder(IFileSystem fileSystem, ILogger logger)
{
    private static readonly string[] _defaultIgnorePatterns =
    [
        ".DS_Store",
        "._.DS_Store",
        "Thumbs.db",
        "Desktop.ini",
        "ehthumbs.db",
        "ehthumbs_vista.db",
        "*~",
        ".*.swp",
        ".*.swo",
        "*.bak",
        "*.tmp",
        ".git",
        ".svn",
        ".hg"
    ];

    private static readonly GitignoreMatcher _defaultIgnoreMatcher = new(_defaultIgnorePatterns);

    public List<ValidatedLinkOperation> Build(
        string repoRoot,
        string userHome,
        string ignoreFileName,
        bool overwrite)
    {
        var ignorePath = Path.Combine(repoRoot, ignoreFileName);
        var ignoreMatcher = LoadIgnoreList(ignorePath);
        logger.Log(LogLevel.Verbose, $"Loaded {ignoreMatcher.Count} user-defined ignore patterns from {ignorePath}");
        logger.Log(LogLevel.Verbose, $"Using {_defaultIgnorePatterns.Length} default ignore patterns");

        var operations = new List<LinkOperation>();
        CollectRepositoryRootOperations(repoRoot, userHome, ignoreMatcher, operations);
        CollectDirectoryOperations(repoRoot, "HOME", userHome, ignoreMatcher, operations);
        CollectRootOperations(repoRoot, ignoreMatcher, operations);

        return ValidateLinkPlan(repoRoot, operations, overwrite);
    }

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
            var relativePath = GetRepositoryRelativePath(repoRoot, file);
            if (ShouldIgnorePath(relativePath, isDirectory: false, ignoreMatcher))
            {
                ignoredCount++;
                logger.Log(LogLevel.Verbose, $"  Ignored file: {Path.GetFileName(file)} (matched ignore pattern)");
                continue;
            }

            linkedCount++;
            operations.Add(new(file, Path.Combine(userHome, Path.GetFileName(file)), SourceIsDirectory: false));
        }

        logger.Log(LogLevel.Verbose, $"Total files in repository root: {totalCount}");
        if (ignoredCount > 0)
        {
            logger.Log(LogLevel.Info, $"Ignored {ignoredCount} files from repository root based on ignore patterns");
        }

        logger.Log(LogLevel.Info, $"Found {linkedCount} files to link from repository root directory to {userHome}");
    }

    private void CollectRootOperations(
        string repoRoot,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            logger.Log(LogLevel.Info, "Skipping ROOT directory processing on non-Unix platforms"u8);
            return;
        }

        CollectDirectoryOperations(repoRoot, "ROOT", "/", ignoreMatcher, operations);
    }

    private void CollectDirectoryOperations(
        string repoRoot,
        string sourceDirectory,
        string destinationDirectory,
        GitignoreMatcher ignoreMatcher,
        List<LinkOperation> operations)
    {
        var sourcePath = Path.Combine(repoRoot, sourceDirectory);
        if (!fileSystem.DirectoryExists(sourcePath))
        {
            logger.Log(LogLevel.Info, $"{sourceDirectory} directory not found: {sourcePath}");
            return;
        }

        if (fileSystem.IsSymbolicLink(sourcePath))
        {
            throw new InvalidOperationException($"'{sourcePath}' must not be a symbolic link.");
        }

        logger.Log(LogLevel.Info, $"Processing {sourceDirectory} directory: {sourcePath}");
        var entries = new List<SourceEntry>();
        var ignoredPaths = new List<string>();
        CollectFiles(repoRoot, sourcePath, ignoreMatcher, entries, ignoredPaths);

        if (ignoredPaths.Count > 0)
        {
            logger.Log(LogLevel.Info, $"Ignoring {ignoredPaths.Count} paths from {sourceDirectory} directory based on ignore patterns:");
            foreach (var path in ignoredPaths)
            {
                logger.Log(LogLevel.Verbose, $"  Ignored path: {path} (matched ignore pattern)");
            }
        }

        logger.Log(LogLevel.Info, $"Found {entries.Count} entries to link from {sourceDirectory} directory to {destinationDirectory}");
        foreach (var entry in entries)
        {
            var relativePath = Path.GetRelativePath(sourcePath, entry.Path);
            operations.Add(new(
                entry.Path,
                Path.Combine(destinationDirectory, relativePath),
                entry.IsDirectory));
        }
    }

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
                }
                else if (fileSystem.IsSymbolicLink(directory))
                {
                    entries.Add(new(directory, IsDirectory: true));
                }
                else
                {
                    pendingDirectories.Push(directory);
                }
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

    private static string GetRepositoryRelativePath(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path);

    private static bool ShouldIgnorePath(string path, bool isDirectory, GitignoreMatcher userIgnoreMatcher) =>
        _defaultIgnoreMatcher.IsIgnored(path, isDirectory) ||
        userIgnoreMatcher.IsIgnored(path, isDirectory);

    private GitignoreMatcher LoadIgnoreList(string ignoreFilePath)
    {
        try
        {
            if (!fileSystem.PathExists(ignoreFilePath))
            {
                logger.Log(LogLevel.Verbose, $"Ignore file not found: {ignoreFilePath}");
                return new(Array.Empty<string>());
            }

            var lines = fileSystem.ReadAllLines(ignoreFilePath);
            logger.Log(LogLevel.Verbose, $"Loaded {lines.Length} lines from ignore file");
            var ignoreMatcher = new GitignoreMatcher(lines);

            foreach (var pattern in lines)
            {
                logger.Log(LogLevel.Verbose, $"Ignoring pattern: '{pattern}'");
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
}
