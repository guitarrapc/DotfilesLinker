using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

// parse args
bool forceOverwrite =
    args.Any(a => a.Equals("--force=y", StringComparison.OrdinalIgnoreCase));

// build up
var fs = new DefaultFileSystem();
var svc = new FileLinkerService(fs);

string executionRoot = Environment.CurrentDirectory;
string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

// execute
try
{
    svc.LinkDotfiles(executionRoot, userHome, ".dotfiles_ignore", forceOverwrite);

    WriteSuccess("All operations completed.");
}
catch (UnauthorizedAccessException ex)
{
    WriteError("Permission denied: " + ex.Message);
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    WriteError("File not found: " + ex.Message);
    Environment.Exit(1);
}
catch (DirectoryNotFoundException ex)
{
    WriteError("Directory not found: " + ex.Message);
    Environment.Exit(1);
}
catch (InvalidOperationException ex)
{
    WriteError("Operation failed: " + ex.Message);
    Environment.Exit(1);
}
catch (Exception ex)
{
    WriteError("An unexpected error occurred: " + ex.Message);
    Environment.Exit(1);
}

static void WriteSuccess(string msg) => WriteColored("[o] ", msg, ConsoleColor.Green);
static void WriteError(string msg) => WriteColored("[x] ", msg, ConsoleColor.Red);

static void WriteColored(string prefix, string msg, ConsoleColor color)
{
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine($"{prefix}{msg}");
    Console.ForegroundColor = prev;
}
