using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

// ── 引数解析 ───────────────────────────────────────────────
bool forceOverwrite =
    args.Any(a => a.Equals("--force=y", StringComparison.OrdinalIgnoreCase));

// ── 依存組み立て ───────────────────────────────────────────
var fs = new DefaultFileSystem();
var svc = new FileLinkerService(fs);

string executionRoot = Environment.CurrentDirectory;
string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

// ── 実行 ───────────────────────────────────────────────────
try
{
    svc.LinkDotfiles(executionRoot, userHome, ".dotfiles_ignore", forceOverwrite);
    svc.LinkHomeFiles(executionRoot, userHome, forceOverwrite);

    WriteSuccess("All operations completed.");
}
catch (Exception ex)
{
    WriteError(ex.Message);
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
