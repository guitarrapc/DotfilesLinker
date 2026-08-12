using DotfilesLinker.Utilities;

namespace DotfilesLinker.Tests;

public class PathUtilitiesTests
{
    [Theory]
    [InlineData("same", "same", true)]
    [InlineData("parent/child", "parent", true)]
    [InlineData("parent/child/grandchild", "parent", true)]
    [InlineData("parent", "parent/child", false)]
    [InlineData("parent-sibling", "parent", false)]
    public void IsSameOrDescendant_DetectsPathContainment(
        string path,
        string directory,
        bool expected)
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "containment");

        Assert.Equal(
            expected,
            PathUtilities.IsSameOrDescendant(
                Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)),
                Path.Combine(root, directory.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public void PathsOverlap_IsSymmetric()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "overlap");
        var parent = Path.Combine(root, "parent");
        var child = Path.Combine(parent, "child");
        var sibling = Path.Combine(root, "sibling");

        Assert.True(PathUtilities.PathsOverlap(parent, child));
        Assert.True(PathUtilities.PathsOverlap(child, parent));
        Assert.False(PathUtilities.PathsOverlap(parent, sibling));
    }

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
