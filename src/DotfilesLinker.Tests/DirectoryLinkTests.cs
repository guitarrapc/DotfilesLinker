using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public sealed class DirectoryLinkTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"DotfilesLinker-Directory-{Guid.NewGuid():N}");
    private string Repo => Path.Combine(root, "repo");
    private string Home => Path.Combine(root, "home");
    private string Source => Path.Combine(Repo, "HOME", ".agents", "skills", "demo");
    private string Target => Path.Combine(Home, ".agents", "skills", "demo");

    private FileLinkerService Setup(string? patterns, string ignore = "")
    {
        Directory.CreateDirectory(Path.Combine(Source, "references"));
        File.WriteAllText(Path.Combine(Source, "SKILL.md"), "skill");
        File.WriteAllText(Path.Combine(Source, "references", "guide.md"), "guide");
        File.WriteAllText(Path.Combine(Repo, "HOME", ".agents", "settings.json"), "{}");
        File.WriteAllText(Path.Combine(Repo, "dotfiles_ignore"), ignore);
        if (patterns is not null)
            File.WriteAllText(Path.Combine(Repo, "dotfiles_link_dirs"), patterns);
        return new(new DefaultFileSystem());
    }

    [Theory]
    [InlineData(null, "", false, false)]
    [InlineData("", "", false, false)]
    [InlineData("HOME/.agents/skills/demo", "", true, false)]
    [InlineData("# Skills\r\n\r\nHOME/.agents/skills/*/\r\n", "", true, false)]
    [InlineData("HOME/.agents/skills/*\n!HOME/.agents/skills/demo", "", false, false)]
    [InlineData("HOME/.agents/skills/other", "", false, false)]
    [InlineData("HOME/.agents/skills/demo/SKILL.md", "", false, false)]
    [InlineData("HOME/.agents/skills/*", "HOME/.agents/skills/demo/", false, true)]
    [InlineData("HOME/.agents/skills/*", "HOME/.agents/skills/", false, true)]
    [InlineData("HOME/.agents/skills/*", "*.md", true, false)]
    public void SelectedDirectoriesBecomeLinks(string? patterns, string ignore, bool directoryLink, bool skipped)
    {
        var service = Setup(patterns, ignore);
        var result = service.LinkDotfiles(Repo, Home, "dotfiles_ignore", overwrite: false);
        Assert.False(result.HasErrors);
        Assert.Equal(!skipped, Directory.Exists(Target));
        if (!skipped)
        {
            Assert.Equal(directoryLink ? Source : null, new DirectoryInfo(Target).LinkTarget);
            Assert.Equal(directoryLink ? null : Path.Combine(Source, "SKILL.md"), new FileInfo(Path.Combine(Target, "SKILL.md")).LinkTarget);
            Assert.Equal("guide", File.ReadAllText(Path.Combine(Target, "references", "guide.md")));
        }
        Assert.NotNull(new FileInfo(Path.Combine(Home, ".agents", "settings.json")).LinkTarget);
        Assert.False(File.Exists(Path.Combine(Home, "dotfiles_link_dirs")));
    }

    [Fact]
    public void SelectedParentStopsTraversalEvenWhenDescendantsAreExcluded()
    {
        var service = Setup("HOME/.agents/skills/demo\n!HOME/.agents/skills/demo/references");
        var result = service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false);
        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Summary.Created);
        Assert.Equal(Source, new DirectoryInfo(Target).LinkTarget);
        Assert.Null(new FileInfo(Path.Combine(Target, "references", "guide.md")).LinkTarget);
    }

    [Fact]
    public void DefaultExcludedDirectoryIsNotLinked()
    {
        var service = Setup("HOME/.agents/skills/*");
        Directory.CreateDirectory(Path.Combine(Repo, "HOME", ".agents", "skills", ".git"));
        Assert.False(service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false).HasErrors);
        Assert.False(Directory.Exists(Path.Combine(Home, ".agents", "skills", ".git")));
    }

    [Fact]
    public void RerunSkipsDirectoryLinkAndNewSourceFilesAreVisible()
    {
        var service = Setup("HOME/.agents/skills/*");
        Assert.False(service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false).HasErrors);
        File.WriteAllText(Path.Combine(Source, "new.md"), "new");
        Assert.Equal("new", File.ReadAllText(Path.Combine(Target, "new.md")));
        var result = service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false);
        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Summary.Skipped);
        Assert.Null(new FileInfo(Path.Combine(Target, "SKILL.md")).LinkTarget);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ExistingFileLinksRequireForceAndDryRunDoesNotMutate(bool force, bool dryRun)
    {
        var service = Setup(null);
        service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false);
        File.WriteAllText(Path.Combine(Repo, "dotfiles_link_dirs"), "HOME/.agents/skills/*");
        var result = service.LinkDotfiles(Repo, Home, "dotfiles_ignore", force, dryRun);
        Assert.Equal(!force, result.HasErrors);
        Assert.Equal(force && !dryRun ? Source : null, new DirectoryInfo(Target).LinkTarget);
        Assert.Equal("skill", File.ReadAllText(Path.Combine(Source, "SKILL.md")));
        Assert.Equal("skill", File.ReadAllText(Path.Combine(Target, "SKILL.md")));
        Assert.False(Directory.Exists(Target + ".dotfileslinker-backup"));
    }

    [Fact]
    public void DryRunReportsDirectoryWithoutCreatingHome()
    {
        Setup("HOME/.agents/skills/*");
        var log = new TestLogger();
        var service = new FileLinkerService(new DefaultFileSystem(), log.Logger);
        var result = service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false, dryRun: true);
        Assert.False(result.HasErrors);
        Assert.Contains("Would create directory symlink", log.Output);
        Assert.False(Directory.Exists(Home));
    }

    [Fact]
    public void SelectedEmptyDirectoryIsLinked()
    {
        var service = Setup("HOME/.agents/skills/*");
        var empty = Path.Combine(Repo, "HOME", ".agents", "skills", "empty");
        Directory.CreateDirectory(empty);
        Assert.False(service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false).HasErrors);
        Assert.Equal(empty, new DirectoryInfo(Path.Combine(Home, ".agents", "skills", "empty")).LinkTarget);
    }

    [Fact]
    public void UnreadableDirectoryLinkConfigurationFailsBeforeMutation()
    {
        var service = Setup(null);
        Directory.CreateDirectory(Path.Combine(Repo, "dotfiles_link_dirs"));
        Assert.ThrowsAny<Exception>(() => service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false));
        Assert.False(Directory.Exists(Home));
    }

    [Fact]
    public void RootDirectorySelectionAppearsInDryRun()
    {
        if (OperatingSystem.IsWindows()) return;
        Setup("ROOT/opt/dotfileslinker-directory-test");
        Directory.CreateDirectory(Path.Combine(Repo, "ROOT", "opt", "dotfileslinker-directory-test"));
        var log = new TestLogger();
        var service = new FileLinkerService(new DefaultFileSystem(), log.Logger);
        Assert.False(service.LinkDotfiles(Repo, Home, "dotfiles_ignore", false, true).HasErrors);
        Assert.Contains("Would create directory symlink: /opt/dotfileslinker-directory-test", log.Output);
        Assert.False(Directory.Exists(Home));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
