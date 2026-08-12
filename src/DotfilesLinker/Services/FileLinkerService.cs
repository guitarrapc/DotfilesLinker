using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Coordinates planning and applying links from a dotfiles repository.
/// </summary>
internal sealed class FileLinkerService
{
    private readonly ILogger _logger;
    private readonly LinkPlanBuilder _planBuilder;
    private readonly LinkPlanExecutor _planExecutor;

    public FileLinkerService(IFileSystem fileSystem, ILogger? logger = null)
    {
        _logger = logger ?? new NullLogger();
        _planBuilder = new(fileSystem, _logger);
        _planExecutor = new(fileSystem, _logger);
    }

    /// <summary>
    /// Links dotfiles from the specified repository to the user's home directory or system root.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreFileName">The name of the ignore file containing patterns to exclude.</param>
    /// <param name="overwrite">Whether to overwrite existing files or directories.</param>
    /// <param name="dryRun">If true, only shows what would be done without actually creating links.</param>
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

        var operations = _planBuilder.Build(repoRoot, userHome, ignoreFileName, overwrite);
        if (operations.Count == 0)
        {
            _logger.Log(
                LogLevel.Error,
                $"No linkable dotfiles were found in '{repoRoot}'. Verify the repository path with --root or run the command from the dotfiles repository.");
            return default;
        }

        var result = _planExecutor.Execute(operations, dryRun);

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
}
