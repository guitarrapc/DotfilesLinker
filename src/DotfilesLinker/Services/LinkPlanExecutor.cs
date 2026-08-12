using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Applies a validated link plan and performs post-commit backup cleanup.
/// </summary>
internal sealed class LinkPlanExecutor(IFileSystem fileSystem, ILogger logger)
{
    public LinkResult Execute(IReadOnlyList<ValidatedLinkOperation> operations, bool dryRun)
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
            if (operation.Disposition == LinkDisposition.Conflict)
            {
                failed++;
                failures ??= [];
                failures.Add(new IOException(GetConflictMessage(operation)));
                continue;
            }

            if (operation.Disposition == LinkDisposition.Skip)
            {
                _ = LinkFile(operation, dryRun: false);
                skipped++;
                continue;
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(operation.Operation.Target)!;
                logger.Log(LogLevel.Verbose, $"Ensuring directory exists: {targetDirectory}");
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

                logger.Log(
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

        var cleanupFailed = CleanupBackups(appliedOperations, ref failures);
        LogFailures(failures);

        return new(
            new LinkSummary(created, replaced, skipped, failed),
            cleanupFailed);
    }

    private int CleanupBackups(
        IReadOnlyList<AppliedLinkOperation> appliedOperations,
        ref List<Exception>? failures)
    {
        var cleanupFailed = 0;
        foreach (var appliedOperation in appliedOperations)
        {
            if (appliedOperation.BackupPath is null)
            {
                continue;
            }

            try
            {
                fileSystem.DeleteBackup(appliedOperation.BackupPath, appliedOperation.Operation.Target);
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

        return cleanupFailed;
    }

    private void LogFailures(List<Exception>? failures)
    {
        if (failures is null)
        {
            return;
        }

        foreach (var failure in failures)
        {
            logger.Log(LogLevel.Error, $"{failure.Message}");
        }
    }

    private static LinkSummary CreateSummary(IReadOnlyList<ValidatedLinkOperation> operations)
    {
        var created = 0;
        var replaced = 0;
        var skipped = 0;
        var failed = 0;

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
                case LinkDisposition.Conflict:
                    failed++;
                    break;
            }
        }

        return new(created, replaced, skipped, failed);
    }

    private void LogLinkOperation(ValidatedLinkOperation operation) =>
        logger.Log(LogLevel.Verbose, $"Linking {operation.Operation.Source} to {operation.Operation.Target}");

    private AppliedLinkOperation? LinkFile(ValidatedLinkOperation validatedOperation, bool dryRun)
    {
        var (operation, disposition) = validatedOperation;
        var (source, target, sourceIsDirectory) = operation;
        var normalizedSource = PathUtilities.NormalizePath(source);
        var normalizedTarget = PathUtilities.NormalizePath(target);

        if (disposition == LinkDisposition.Conflict)
        {
            logger.Log(LogLevel.Error, $"{GetConflictMessage(validatedOperation)}");
            return null;
        }

        if (disposition == LinkDisposition.Skip)
        {
            if (dryRun)
            {
                logger.Log(LogLevel.Success, $"[DRY-RUN] Would skip already linked: {normalizedTarget} -> {normalizedSource}");
            }
            else
            {
                logger.Log(LogLevel.Success, $"Skipping already linked: {normalizedTarget} -> {normalizedSource}");
            }

            return null;
        }

        if (dryRun)
        {
            if (disposition == LinkDisposition.Replace)
            {
                logger.Log(LogLevel.Verbose, $"[DRY-RUN] Would replace existing target: {normalizedTarget}");
            }

            if (sourceIsDirectory)
            {
                logger.Log(LogLevel.Success, $"[DRY-RUN] Would create directory symlink: {normalizedTarget} -> {normalizedSource}");
            }
            else
            {
                logger.Log(LogLevel.Success, $"[DRY-RUN] Would create file symlink: {normalizedTarget} -> {normalizedSource}");
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
                    throw new AggregateException(
                        $"Failed to create symlink and restore the original target '{normalizedTarget}'. " +
                        $"The original entry may remain at '{PathUtilities.NormalizePath(backupPath)}'.",
                        ex,
                        rollbackException);
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

        logger.Log(LogLevel.Verbose, $"Temporarily moving existing target: {target} -> {backupPath}");
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

    private static string GetConflictMessage(ValidatedLinkOperation operation) =>
        $"Failed to link '{PathUtilities.NormalizePath(operation.Operation.Target)}' from " +
        $"'{PathUtilities.NormalizePath(operation.Operation.Source)}': target already exists; use --force to overwrite.";

    private readonly record struct AppliedLinkOperation(
        LinkOperation Operation,
        string? BackupPath);
}
