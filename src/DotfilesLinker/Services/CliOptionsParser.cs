namespace DotfilesLinker.Services;

internal readonly record struct CliOptions(
    bool ShowHelp = false,
    bool ShowVersion = false,
    bool ForceOverwrite = false,
    bool Verbose = false,
    bool DryRun = false);

internal static class CliOptionsParser
{
    public static bool TryParse(
        ReadOnlySpan<string> args,
        out CliOptions options,
        out string? error)
    {
        var showHelp = false;
        var showVersion = false;
        var forceOverwrite = false;
        var verbose = false;
        var dryRun = false;

        foreach (var argument in args)
        {
            switch (argument)
            {
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--version":
                    showVersion = true;
                    break;
                case "--force":
                case "--force=true":
                    forceOverwrite = true;
                    break;
                case "--force=false":
                    forceOverwrite = false;
                    break;
                case "--verbose":
                case "-v":
                    verbose = true;
                    break;
                case "--dry-run":
                case "-d":
                    dryRun = true;
                    break;
                case "--":
                    options = default;
                    error = "unexpected argument '--'";
                    return false;
                default:
                    options = default;
                    error = argument.StartsWith('-')
                        ? $"unknown option '{argument}'"
                        : $"unexpected argument '{argument}'";
                    return false;
            }
        }

        options = new CliOptions(
            showHelp,
            showVersion,
            forceOverwrite,
            verbose,
            dryRun);
        error = null;
        return true;
    }
}
