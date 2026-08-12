namespace DotfilesLinker.Services;

internal readonly record struct CliOptions(
    bool ShowHelp = false,
    bool ShowVersion = false,
    bool ForceOverwrite = false,
    bool Verbose = false,
    bool DryRun = false,
    string? RepositoryRoot = null);

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
        string? repositoryRoot = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
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
                case "--root":
                    if (++index >= args.Length || string.IsNullOrEmpty(args[index]))
                    {
                        options = default;
                        error = "option '--root' requires a non-empty path";
                        return false;
                    }

                    repositoryRoot = args[index];
                    break;
                case "--":
                    options = default;
                    error = "unexpected argument '--'";
                    return false;
                default:
                    if (argument.StartsWith("--root=", StringComparison.Ordinal))
                    {
                        repositoryRoot = argument["--root=".Length..];
                        if (repositoryRoot.Length == 0)
                        {
                            options = default;
                            error = "option '--root' requires a non-empty path";
                            return false;
                        }

                        break;
                    }

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
            dryRun,
            repositoryRoot);
        error = null;
        return true;
    }
}
