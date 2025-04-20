using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using System.Reflection;

// parse args
bool showHelp = args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase));
bool showVersion = args.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase));
bool forceOverwrite = args.Any(a => a.Equals("--force=y", StringComparison.OrdinalIgnoreCase));
bool verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase));

// display help or version information and exit if requested
if (showHelp)
{
    DisplayHelp();
    return;
}
if (showVersion)
{
    DisplayVersion();
    return;
}

// build up
var fs = new DefaultFileSystem();
var logger = new ConsoleLogger(verbose);
var svc = new FileLinkerService(fs, logger);

// Get configuration from environment variables or use defaults
string executionRoot = Environment.GetEnvironmentVariable("DOTFILES_ROOT") ?? Environment.CurrentDirectory;
string userHome = Environment.GetEnvironmentVariable("DOTFILES_HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string ignoreFileName = Environment.GetEnvironmentVariable("DOTFILES_IGNORE_FILE") ?? "dotfiles_ignore";

logger.Info($"Execution root: {executionRoot}");
logger.Info($"User home: {userHome}");
logger.Info($"Ignore file: {ignoreFileName}");
logger.Info($"Force overwrite: {forceOverwrite}");

// execute
try
{
    svc.LinkDotfiles(executionRoot, userHome, ignoreFileName, forceOverwrite);

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

// Displays help information for the application.
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
          --version          Display version information

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

        Environment Variables:
          DOTFILES_ROOT            Directory containing dotfiles (default: current directory)
          DOTFILES_HOME            Target home directory (default: user's home directory)
          DOTFILES_IGNORE_FILE     Name of ignore file (default: dotfiles_ignore)

        Examples:
          {{appName}}              # Link dotfiles using default settings
          {{appName}} --force=y    # Overwrite any existing files
          {{appName}} --verbose    # Show detailed information
        """);
}

// Displays version information for the application.
static void DisplayVersion()
{
    var asm = Assembly.GetEntryAssembly();
    var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);

    // Get version information
    var version = "1.0.0";
    var infoVersion = asm!.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
    if (infoVersion != null)
    {
        version = infoVersion.InformationalVersion;
        var i = version.IndexOf('+');
        if (i != -1)
        {
            version = version.Substring(0, i);
        }
    }
    else
    {
        var asmVersion = asm!.GetCustomAttribute<AssemblyVersionAttribute>();
        if (asmVersion != null)
        {
            version = asmVersion.Version;
        }
    }

    Console.WriteLine($"{appName} version {version}");
}
