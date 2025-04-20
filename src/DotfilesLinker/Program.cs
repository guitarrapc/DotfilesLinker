using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

// parse args
bool forceOverwrite = args.Any(a => a.Equals("--force=y", StringComparison.OrdinalIgnoreCase));
bool verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase));

// build up
var fs = new DefaultFileSystem();
var logger = new ConsoleLogger(verbose);
var svc = new FileLinkerService(fs, logger);

string executionRoot = Environment.CurrentDirectory;
string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

logger.Info($"Execution root: {executionRoot}");
logger.Info($"User home: {userHome}");
logger.Info($"Force overwrite: {forceOverwrite}");

// execute
try
{
    svc.LinkDotfiles(executionRoot, userHome, "dotfiles_ignore", forceOverwrite);

    logger.Success("All operations completed.");
}
catch (UnauthorizedAccessException ex)
{
    logger.Error("Permission denied: " + ex.Message);
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    logger.Error("File not found: " + ex.Message);
    Environment.Exit(1);
}
catch (DirectoryNotFoundException ex)
{
    logger.Error("Directory not found: " + ex.Message);
    Environment.Exit(1);
}
catch (InvalidOperationException ex)
{
    logger.Error("Operation failed: " + ex.Message);
    Environment.Exit(1);
}
catch (Exception ex)
{
    logger.Error("An unexpected error occurred: " + ex.Message);
    Environment.Exit(1);
}
