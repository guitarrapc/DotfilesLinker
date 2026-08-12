using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class ConsoleLoggerTests
{
    [Fact]
    public void Error_ShouldWriteToStandardErrorOnly()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var logger = new ConsoleLogger(verbose: false, standardOutput, standardError);

        logger.Error("operation failed");

        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.Equal($"[x] operation failed{Environment.NewLine}", standardError.ToString());
    }

    [Fact]
    public void Success_ShouldWriteToStandardOutputOnly()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var logger = new ConsoleLogger(verbose: false, standardOutput, standardError);

        logger.Success("completed");

        Assert.Equal($"[o] completed{Environment.NewLine}", standardOutput.ToString());
        Assert.Equal(string.Empty, standardError.ToString());
    }

    [Fact]
    public void Summary_ShouldWriteCountsToStandardOutput()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var logger = new ConsoleLogger(verbose: false, standardOutput, standardError);

        logger.Summary(new LinkSummary(Created: 2, Replaced: 3, Skipped: 4));

        Assert.Equal(
            $"Created: 2, replaced: 3, skipped: 4{Environment.NewLine}",
            standardOutput.ToString());
        Assert.Equal(string.Empty, standardError.ToString());
    }
}
