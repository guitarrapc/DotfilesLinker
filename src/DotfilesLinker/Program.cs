using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using System.Reflection;

// parse args
if (!CliOptionsParser.TryParse(args, out var options, out var optionError))
{
    var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
    Console.Error.WriteLine($"Error: {optionError}");
    Console.Error.WriteLine($"Try '{appName} --help' for more information.");
    Environment.ExitCode = 2;
    return;
}

// display help or version information and exit if requested
if (options.ShowHelp)
{
    DisplayHelp();
    return;
}
if (options.ShowVersion)
{
    DisplayVersion();
    return;
}

// build up
var fs = new DefaultFileSystem();
using var logger = new ConsoleLogger(options.Verbose);
var svc = new FileLinkerService(fs, logger);

// execute
try
{
    // Get configuration from command-line options, environment variables, or defaults
    string executionRoot = PathOptionResolver.Resolve(
        options.RepositoryRoot,
        Environment.GetEnvironmentVariable("DOTFILES_ROOT"),
        Environment.CurrentDirectory);
    string userHome = PathOptionResolver.Resolve(
        optionValue: null,
        Environment.GetEnvironmentVariable("DOTFILES_HOME"),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    string ignoreFileName = Environment.GetEnvironmentVariable("DOTFILES_IGNORE_FILE") ?? "dotfiles_ignore";

    logger.Log(LogLevel.Info, $"Execution root: {executionRoot}");
    logger.Log(LogLevel.Info, $"User home: {userHome}");
    logger.Log(LogLevel.Info, $"Ignore file: {ignoreFileName}");
    logger.Log(LogLevel.Info, $"Force overwrite: {options.ForceOverwrite}");
    logger.Log(LogLevel.Info, $"Dry run: {options.DryRun}");

    var result = svc.LinkDotfiles(
        executionRoot,
        userHome,
        ignoreFileName,
        options.ForceOverwrite,
        options.DryRun);
    var summary = result.Summary;
    logger.Log(
        LogLevel.Summary,
        $"Created: {summary.Created}, replaced: {summary.Replaced}, skipped: {summary.Skipped}, failed: {summary.Failed}");

    if (summary.Total == 0)
    {
        Environment.ExitCode = 1;
        return;
    }

    if (result.HasErrors)
    {
        Environment.ExitCode = 1;
        return;
    }

    if (options.DryRun)
    {
        logger.Log(LogLevel.Success, "Dry run completed successfully. No changes were made."u8);
    }
    else
    {
        logger.Log(LogLevel.Success, "All operations completed."u8);
    }
}
catch (UnauthorizedAccessException ex)
{
    logger.Log(LogLevel.Error, $"Permission denied: {ex.Message}");
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    logger.Log(LogLevel.Error, $"File not found: {ex.Message}");
    Environment.Exit(1);
}
catch (DirectoryNotFoundException ex)
{
    logger.Log(LogLevel.Error, $"Directory not found: {ex.Message}");
    Environment.Exit(1);
}
catch (InvalidOperationException ex)
{
    logger.Log(LogLevel.Error, $"Operation failed: {ex.Message}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    logger.Log(LogLevel.Error, $"An unexpected error occurred: {ex.Message}");
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
          --root PATH        Directory containing dotfiles (default: DOTFILES_ROOT or current directory)
          --force            Overwrite existing files or directories
          --verbose, -v      Display detailed information during execution
          --version          Display version information
          --dry-run, -d      Simulate the operations without making any changes

        Description:
          This utility creates symbolic links from files in the selected repository
          to the appropriate locations in your home directory.

        Directory Structure:
          - Files with a '.' prefix in the repository root will be linked directly to $HOME
          - Files in the HOME/ directory will be linked to the same relative path in $HOME
          - Files in the ROOT/ directory will be linked to the same relative path in /
            (Only available on Linux/macOS)

        Ignore File:
          Files listed in 'dotfiles_ignore' will be excluded from linking

        Environment Variables:
          DOTFILES_ROOT            Directory containing dotfiles when --root is omitted
          DOTFILES_HOME            Target home directory (default: user's home directory)
          DOTFILES_IGNORE_FILE     Name of ignore file (default: dotfiles_ignore)

        Examples:
          {{appName}}              # Link dotfiles using default settings
          {{appName}} --root PATH  # Link dotfiles from another directory
          {{appName}} --force      # Overwrite any existing files
          {{appName}} --verbose    # Show detailed information
          {{appName}} --dry-run    # Simulate the operations
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
