using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class FileLinkerServicePatternTests
{
    [Theory]
    [InlineData(".DS_Store", true)]
    [InlineData("._.DS_Store", true)]
    [InlineData("Thumbs.db", true)]
    [InlineData("desktop.ini", true)]
    [InlineData("backup.bak", true)]
    [InlineData("temp.tmp", true)]
    [InlineData(".vimrc.swp", true)]
    [InlineData("script~", true)]
    [InlineData(".dotfile", false)]
    [InlineData(".gitconfig", false)]
    [InlineData("HOME/.git/config", true)]
    [InlineData("HOME/.config/app.json", false)]
    public void LinkDotfiles_AppliesBuiltInPatterns(string repositoryPath, bool expectedIgnored)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var logger = new TestLogger();
        var service = new FileLinkerService(fileSystem, logger.Logger);
        var repoRoot = Path.Combine(Path.DirectorySeparatorChar.ToString(), "repo");
        var userHome = Path.Combine(Path.DirectorySeparatorChar.ToString(), "home");
        var source = Path.Combine(repoRoot, repositoryPath.Replace('/', Path.DirectorySeparatorChar));
        var homeRoot = Path.Combine(repoRoot, "HOME");

        fileSystem.EnumerateFiles(repoRoot, ".*", false).Returns(
            repositoryPath.StartsWith("HOME/", StringComparison.Ordinal)
                ? Array.Empty<string>()
                : [source]);
        fileSystem.DirectoryExists(homeRoot).Returns(
            repositoryPath.StartsWith("HOME/", StringComparison.Ordinal));
        fileSystem.EnumerateFiles(homeRoot, "*", false).Returns(
            repositoryPath.StartsWith("HOME/", StringComparison.Ordinal)
                ? [source]
                : Array.Empty<string>());

        service.LinkDotfiles(repoRoot, userHome, "dotfiles_ignore", overwrite: false, dryRun: true);

        if (expectedIgnored)
        {
            Assert.DoesNotContain($" -> {source}", logger.Output);
        }
        else
        {
            Assert.Contains($" -> {source}", logger.Output);
        }
    }
}
