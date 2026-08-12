using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class CliOptionsParserTests
{
    [Theory]
    [MemberData(nameof(ValidOptions))]
    public void TryParse_ParsesDocumentedOptions(
        string[] args,
        bool showHelp,
        bool showVersion,
        bool forceOverwrite,
        bool verbose,
        bool dryRun)
    {
        var result = CliOptionsParser.TryParse(args, out var options, out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Equal(
            new CliOptions(showHelp, showVersion, forceOverwrite, verbose, dryRun),
            options);
    }

    [Theory]
    [InlineData("--FORCE", "unknown option")]
    [InlineData("--Help", "unknown option")]
    [InlineData("-V", "unknown option")]
    [InlineData("--froce", "unknown option")]
    [InlineData("--force=y", "unknown option")]
    [InlineData("repository", "unexpected argument")]
    [InlineData("--", "unexpected argument")]
    public void TryParse_RejectsInvalidOrIncorrectlyCasedArguments(
        string argument,
        string expectedError)
    {
        var result = CliOptionsParser.TryParse([argument], out var options, out var error);

        Assert.False(result);
        Assert.Equal(default, options);
        Assert.Contains(expectedError, error);
    }

    public static TheoryData<string[], bool, bool, bool, bool, bool> ValidOptions() => new()
    {
        { [], false, false, false, false, false },
        { ["--force"], false, false, true, false, false },
        { ["--force=true"], false, false, true, false, false },
        { ["--force", "--force=false"], false, false, false, false, false },
        { ["--verbose", "--dry-run"], false, false, false, true, true },
        { ["-v", "-d", "-h"], true, false, false, true, true },
        { ["--version"], false, true, false, false, false }
    };
}
