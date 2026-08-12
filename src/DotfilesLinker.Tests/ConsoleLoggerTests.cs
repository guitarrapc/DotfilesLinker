using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class ConsoleLoggerTests
{
    [Fact]
    public void Error_ShouldWriteToStandardErrorOnly()
    {
        var logger = new TestLogger(verbose: false);

        logger.Logger.Log(LogLevel.Error, "operation failed"u8);

        Assert.Equal(string.Empty, logger.Output);
        Assert.Equal($"[x] operation failed{Environment.NewLine}", logger.Error);
    }

    [Fact]
    public void Success_ShouldWriteToStandardOutputOnly()
    {
        var logger = new TestLogger(verbose: false);

        logger.Logger.Log(LogLevel.Success, "completed"u8);

        Assert.Equal($"[o] completed{Environment.NewLine}", logger.Output);
        Assert.Equal(string.Empty, logger.Error);
    }

    [Fact]
    public void Summary_ShouldWriteInterpolatedCountsToStandardOutput()
    {
        var logger = new TestLogger(verbose: false);
        var summary = new LinkSummary(Created: 2, Replaced: 3, Skipped: 4, Failed: 5);

        logger.Logger.Log(
            LogLevel.Summary,
            $"Created: {summary.Created}, replaced: {summary.Replaced}, skipped: {summary.Skipped}, failed: {summary.Failed}");

        Assert.Equal(
            $"Created: 2, replaced: 3, skipped: 4, failed: 5{Environment.NewLine}",
            logger.Output);
        Assert.Equal(string.Empty, logger.Error);
    }

    [Fact]
    public void Verbose_ShouldNotFormatOrWriteWhenDisabled()
    {
        var logger = new TestLogger(verbose: false);
        var formatted = false;

        logger.Logger.Log(LogLevel.Verbose, $"value: {FormatValue()}");

        Assert.False(formatted);
        Assert.Equal(string.Empty, logger.Output);

        string FormatValue()
        {
            formatted = true;
            return "formatted";
        }
    }

    [Fact]
    public void InterpolatedMessage_ShouldWriteUtf8WithoutLosingUnicode()
    {
        var logger = new TestLogger(verbose: true);
        const string path = "ドットファイル/設定.json";

        logger.Logger.Log(LogLevel.Info, $"対象: {path}");

        Assert.Equal($"[i] 対象: {path}{Environment.NewLine}", logger.Output);
    }
}
