using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

// parse args
bool showHelp = args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase));
bool forceOverwrite = args.Any(a => a.Equals("--force=y", StringComparison.OrdinalIgnoreCase));
bool verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase));

// display help and exit if requested
if (showHelp)
{
    DisplayHelp();
    return;
}

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

/// <summary>
/// Displays help information for the application.
/// </summary>
static void DisplayHelp()
{
    var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
    Console.WriteLine($$"""
        Dotfiles Linker - A utility to link dotfiles from a repository to your home directory

        Usage: {{appName}} [options]

        Options:
          --help, -h         Display this help message
          --force=y          Overwrite existing files or directories
          --verbose, -v      Display detailed information during execution

        Description:
          This utility creates symbolic links from files in the current directory
          to the appropriate locations in your home directory.

        Directory Structure:
          - Files with a '.' prefix in the repository root will be linked directly to $HOME
          - Files in the HOME/ directory will be linked to the same relative path in $HOME
          - Files in the ROOT/ directory will be linked to the same relative path in /
            (Only available on Linux/macOS)

        Ignore File:
          Files listed in 'dotfiles_ignore' will be excluded from linking

        Examples:
          {{appName}}              # Link dotfiles using default settings
          {{appName}} --force=y    # Overwrite any existing files
          {{appName}} --verbose    # Show detailed information
        """);
}
