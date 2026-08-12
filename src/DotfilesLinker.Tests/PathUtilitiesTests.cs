using DotfilesLinker.Utilities;

namespace DotfilesLinker.Tests;

public class PathUtilitiesTests
{
    [Fact]
    public void LinkTargetEquals_ResolvesRelativeTargetFromLinkDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "relative-link");
        var linkPath = Path.Combine(root, "home", ".config", "settings.json");
        var linkTarget = Path.Combine("..", "..", "repo", "settings.json");
        var expectedTarget = Path.Combine(root, "repo", "settings.json");

        Assert.True(PathUtilities.LinkTargetEquals(linkPath, linkTarget, expectedTarget));
    }

    [Fact]
    public void LinkTargetEquals_ReturnsFalseForDifferentResolvedTarget()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "different-link");
        var linkPath = Path.Combine(root, "home", "settings.json");

        Assert.False(PathUtilities.LinkTargetEquals(
            linkPath,
            Path.Combine("..", "repo", "actual.json"),
            Path.Combine(root, "repo", "expected.json")));
    }

    [Fact]
    public void LinkTargetEquals_ComparesAbsoluteTarget()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "absolute-link");
        var linkPath = Path.Combine(root, "home", "settings.json");
        var expectedTarget = Path.Combine(root, "repo", "settings.json");

        Assert.True(PathUtilities.LinkTargetEquals(linkPath, expectedTarget, expectedTarget));
    }

    [Fact]
    public void LinkTargetEquals_UsesPlatformCaseSensitivity()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "case-link");
        var linkPath = Path.Combine(root, "home", "settings.json");
        var linkTarget = Path.Combine(root, "repo", "SETTINGS.json");
        var expectedTarget = Path.Combine(root, "repo", "settings.json");

        Assert.Equal(
            OperatingSystem.IsWindows(),
            PathUtilities.LinkTargetEquals(linkPath, linkTarget, expectedTarget));
    }
}
