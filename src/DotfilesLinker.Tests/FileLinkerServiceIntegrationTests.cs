using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public sealed class FileLinkerServiceIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"DotfilesLinker-Service-{Guid.NewGuid():N}");

    [Fact]
    public void LinkDotfiles_PreservesDirectorySymlinkWithoutTraversingItsTarget()
    {
        var repoRoot = Path.Combine(_root, "repo");
        var sourceHome = Path.Combine(repoRoot, "HOME");
        var externalDirectory = Path.Combine(_root, "external");
        var userHome = Path.Combine(_root, "user-home");
        var sourceLink = Path.Combine(sourceHome, ".config", "shared");
        var destinationLink = Path.Combine(userHome, ".config", "shared");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceLink)!);
        Directory.CreateDirectory(externalDirectory);
        File.WriteAllText(Path.Combine(externalDirectory, "external.txt"), "content");
        Directory.CreateSymbolicLink(sourceLink, externalDirectory);

        var service = new FileLinkerService(new DefaultFileSystem());
        service.LinkDotfiles(repoRoot, userHome, "dotfiles_ignore", overwrite: false);

        Assert.Equal(sourceLink, new DirectoryInfo(destinationLink).LinkTarget);
        Assert.Equal("content", File.ReadAllText(Path.Combine(destinationLink, "external.txt")));
    }

    [Fact]
    public void LinkDotfiles_WithForceReplacesNonEmptyDirectoryAndRemovesBackup()
    {
        var repoRoot = Path.Combine(_root, "repo");
        var userHome = Path.Combine(_root, "user-home");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var backup = target + ".dotfileslinker-backup";

        Directory.CreateDirectory(repoRoot);
        File.WriteAllText(source, "new settings");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "existing.txt"), "existing settings");

        var service = new FileLinkerService(new DefaultFileSystem());
        service.LinkDotfiles(repoRoot, userHome, "dotfiles_ignore", overwrite: true);

        Assert.Equal(source, new FileInfo(target).LinkTarget);
        Assert.False(Directory.Exists(backup));
        Assert.False(File.Exists(backup));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
