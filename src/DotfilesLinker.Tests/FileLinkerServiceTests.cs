using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class FileLinkerServiceTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly FileLinkerService _service;

    public FileLinkerServiceTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _service = new FileLinkerService(_fileSystemMock);
    }

    [Fact]
    public void LinkDotfiles_ShouldLinkFilesInRepoRoot()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[] { "/repo/.file1", "/repo/.file2" };
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        foreach (var file in filesInRepo)
        {
            var target = Path.Combine(userHome, Path.GetFileName(file));
            _fileSystemMock.Received(1).CreateFileSymlink(target, file);
        }
    }

    [Fact]
    public void LinkDotfiles_ShouldResolveRelativeRootsBeforeCreatingLinks()
    {
        var repoRoot = Path.Combine("relative", "repo");
        var userHome = Path.Combine("relative", "home");
        var fullRepoRoot = Path.GetFullPath(repoRoot);
        var fullUserHome = Path.GetFullPath(userHome);
        var source = Path.Combine(fullRepoRoot, ".settings");
        var target = Path.Combine(fullUserHome, ".settings");

        _fileSystemMock.EnumerateFiles(fullRepoRoot, ".*", false).Returns([source]);

        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: false);

        _fileSystemMock.Received(1).CreateFileSymlink(target, source);
        _fileSystemMock.Received(1).PathExists(Path.Combine(fullRepoRoot, ".dotfiles_ignore"));
        _fileSystemMock.DidNotReceive().CreateFileSymlink(
            Arg.Is<string>(path => !Path.IsPathFullyQualified(path)),
            Arg.Any<string>());
    }

    [Fact]
    public void LinkDotfiles_ShouldSkipIgnoredFiles()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[] { "/repo/.file1", "/repo/.file2" };
        var ignoredFiles = new[] { ".file2" };

        // Mock file system behavior
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        string ignoreFilePath = Path.Combine(repoRoot, ignoreFileName);

        // Mock ignore file existance check
        _fileSystemMock.PathExists(ignoreFilePath).Returns(true);

        // Mock ReadAllLines
        _fileSystemMock.ReadAllLines(ignoreFilePath).Returns(ignoredFiles);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        _fileSystemMock.Received(1).CreateFileSymlink(Path.Combine(userHome, ".file1"), "/repo/.file1");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".file2"), "/repo/.file2");
    }

    [Fact]
    public void LinkDotfiles_ShouldStopWhenIgnoreFileCannotBeRead()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var ignoreFileName = "dotfiles_ignore";
        var ignoreFilePath = Path.Combine(repoRoot, ignoreFileName);
        var readException = new IOException("access denied");

        _fileSystemMock.PathExists(ignoreFilePath).Returns(true);
        _fileSystemMock.ReadAllLines(ignoreFilePath).Returns(_ => throw readException);

        var exception = Assert.Throws<IOException>(() =>
            _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite: false));

        Assert.Same(readException, exception.InnerException);
        Assert.Contains(ignoreFilePath, exception.Message);
        _fileSystemMock.DidNotReceive().EnumerateFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void LinkDotfiles_ShouldStopWhenIgnoreFileCannotBeInspected()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var ignoreFileName = "dotfiles_ignore";
        var ignoreFilePath = Path.Combine(repoRoot, ignoreFileName);
        var inspectionException = new UnauthorizedAccessException("access denied");

        _fileSystemMock.PathExists(ignoreFilePath).Returns(_ => throw inspectionException);

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite: false));

        Assert.Same(inspectionException, exception.InnerException);
        Assert.Contains(ignoreFilePath, exception.Message);
        _fileSystemMock.DidNotReceive().ReadAllLines(Arg.Any<string>());
        _fileSystemMock.DidNotReceive().EnumerateFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public void LinkDotfiles_ShouldThrowException_WhenTargetExistsAndOverwriteIsFalse()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[] { "/repo/.file1" };
        var target = Path.Combine(userHome, ".file1");
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.PathExists(target).Returns(true);
        _fileSystemMock.GetLinkTarget(Arg.Any<string>()).Returns((string?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite));
    }


    [Fact]
    public void LinkDotfiles_ShouldLinkFilesInHomeDirectory()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string homeRoot = Path.Combine(repoRoot, "HOME");
        bool overwrite = false;

        var filesInHome = new[] { "/repo/HOME/.config/file1", "/repo/HOME/.config/file2" };
        _fileSystemMock.DirectoryExists(homeRoot).Returns(true);
        _fileSystemMock.EnumerateFiles(homeRoot, "*", false).Returns(filesInHome);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite);

        // Assert
        foreach (var file in filesInHome)
        {
            var relativePath = Path.GetRelativePath(homeRoot, file);
            var target = Path.Combine(userHome, relativePath);
            _fileSystemMock.Received(1).CreateFileSymlink(target, file);
        }
    }

    [Fact]
    public void LinkDotfiles_ShouldLinkDirectorySymlinkWithoutTraversingIt()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var homeRoot = Path.Combine(repoRoot, "HOME");
        var directoryLink = Path.Combine(homeRoot, ".config", "shared");
        var target = Path.Combine(userHome, ".config", "shared");

        _fileSystemMock.DirectoryExists(homeRoot).Returns(true);
        _fileSystemMock.DirectoryExists(directoryLink).Returns(true);
        _fileSystemMock.EnumerateDirectories(homeRoot).Returns([directoryLink]);
        _fileSystemMock.IsSymbolicLink(directoryLink).Returns(true);

        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: false);

        _fileSystemMock.Received(1).CreateDirectorySymlink(target, directoryLink);
        _fileSystemMock.DidNotReceive().EnumerateDirectories(directoryLink);
        _fileSystemMock.DidNotReceive().EnumerateFiles(directoryLink, Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public void LinkDotfiles_ShouldRejectSymbolicHomeDirectory()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var homeRoot = Path.Combine(repoRoot, "HOME");

        _fileSystemMock.DirectoryExists(homeRoot).Returns(true);
        _fileSystemMock.IsSymbolicLink(homeRoot).Returns(true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _service.LinkDotfiles(repoRoot, "/home/user", ".dotfiles_ignore", overwrite: false));

        Assert.Contains("must not be a symbolic link", exception.Message);
        _fileSystemMock.DidNotReceive().EnumerateDirectories(homeRoot);
        _fileSystemMock.DidNotReceive().EnumerateFiles(homeRoot, Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public void LinkDotfiles_ShouldIgnoreDefaultOSSpecificFiles()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[]
        {
            "/repo/.file1",
            "/repo/.DS_Store",       // macOS specific
            "/repo/._.DS_Store",     // macOS specific
            "/repo/Thumbs.db",       // Windows specific
            "/repo/Desktop.ini"      // Windows specific
        };

        // Mock file system behavior
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        // Only the actual dotfile should be linked, not the OS specific files
        _fileSystemMock.Received(1).CreateFileSymlink(Path.Combine(userHome, ".file1"), "/repo/.file1");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".DS_Store"), "/repo/.DS_Store");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "._.DS_Store"), "/repo/._.DS_Store");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "Thumbs.db"), "/repo/Thumbs.db");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "Desktop.ini"), "/repo/Desktop.ini");
    }

    [Fact]
    public void LinkDotfiles_ShouldIgnoreDefaultWildcardPatterns()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[]
        {
            "/repo/.file1",
            "/repo/backup.bak",    // *.bak pattern
            "/repo/temp.tmp",      // *.tmp pattern
            "/repo/.vimrc.swp",    // .*.swp pattern
            "/repo/script~"        // *~ pattern
        };

        // Mock file system behavior
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        // Only the actual dotfile should be linked, not the files matching wildcard patterns
        _fileSystemMock.Received(1).CreateFileSymlink(Path.Combine(userHome, ".file1"), "/repo/.file1");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "backup.bak"), "/repo/backup.bak");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "temp.tmp"), "/repo/temp.tmp");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".vimrc.swp"), "/repo/.vimrc.swp");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "script~"), "/repo/script~");
    }

    [Fact]
    public void LinkDotfiles_ShouldIgnoreDefaultFilesInSubdirectories()
    {
        // Arrange
        string repoRoot = "/repo";
        string userHome = "/home/user";
        string homeRoot = Path.Combine(repoRoot, "HOME");
        bool overwrite = false;

        var filesInHome = new[]
        {
            "/repo/HOME/.config/file1",
            "/repo/HOME/.config/.DS_Store",
            "/repo/HOME/Documents/Thumbs.db",
            "/repo/HOME/Scripts/script~"
        };

        _fileSystemMock.DirectoryExists(homeRoot).Returns(true);
        _fileSystemMock.EnumerateFiles(homeRoot, "*", false).Returns(filesInHome);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Mock GetRelativePath behavior
        _fileSystemMock.When(fs =>
            fs.CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                // Just capture the call but don't do anything
            });

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite);

        // Assert
        // Verify that CreateFileSymlink was called only once and only for the non-OS specific file
        _fileSystemMock.Received(1).CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/.config/file1"); _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/.config/.DS_Store");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/Documents/Thumbs.db");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/Scripts/script~");
    }

    [Fact]
    public void LinkDotfiles_ShouldCombineUserDefinedAndDefaultIgnorePatterns()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        string userHome = "/home/user";
        string ignoreFileName = ".dotfiles_ignore";
        bool overwrite = false;

        var filesInRepo = new[]
        {
            "/repo/.file1",
            "/repo/.file2",         // User-defined ignore
            "/repo/.DS_Store",      // Default ignore
            "/repo/custom.ignore"   // User-defined ignore
        };

        var ignoredFiles = new[] { ".file2", "custom.ignore" };

        // Mock file system behavior
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        string ignoreFilePath = Path.Combine(repoRoot, ignoreFileName);

        // Mock ignore file existance check
        _fileSystemMock.PathExists(ignoreFilePath).Returns(true);

        // Mock ReadAllLines
        _fileSystemMock.ReadAllLines(ignoreFilePath).Returns(ignoredFiles);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        // Only .file1 should be linked
        _fileSystemMock.Received(1).CreateFileSymlink(Path.Combine(userHome, ".file1"), "/repo/.file1");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".file2"), "/repo/.file2");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".DS_Store"), "/repo/.DS_Store");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, "custom.ignore"), "/repo/custom.ignore");
    }

    [Fact]
    public void LinkDotfiles_ShouldSkipEquivalentRelativeSymbolicLink()
    {
        var root = Path.Combine(Path.GetTempPath(), "DotfilesLinker", "relative-link-service");
        var repoRoot = Path.Combine(root, "repo");
        var userHome = Path.Combine(root, "home");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var relativeLinkTarget = Path.GetRelativePath(userHome, source);

        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns([source]);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);
        _fileSystemMock.FileExists(target).Returns(true);
        _fileSystemMock.PathExists(target).Returns(true);
        _fileSystemMock.GetLinkTarget(target).Returns(relativeLinkTarget);

        _service.LinkDotfiles(
            repoRoot,
            userHome,
            ".dotfiles_ignore",
            overwrite: false);

        _fileSystemMock.Received(1).GetLinkTarget(target);
        _fileSystemMock.DidNotReceive().Delete(target);
        _fileSystemMock.DidNotReceive().CreateFileSymlink(target, source);
    }

    [Fact]
    public void LinkDotfiles_WithForceRestoresOriginalWhenLinkCreationFails()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var backup = target + ".dotfileslinker-backup";
        var creationException = new IOException("symlink creation failed");
        var targetInspectionCount = 0;

        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns([source]);
        _fileSystemMock.PathExists(target).Returns(_ => targetInspectionCount++ == 0);
        _fileSystemMock.PathExists(backup).Returns(false);
        _fileSystemMock
            .When(fs => fs.CreateFileSymlink(target, source))
            .Do(_ => throw creationException);

        var exception = Assert.Throws<IOException>(() =>
            _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: true));

        Assert.Same(creationException, exception);
        Received.InOrder(() =>
        {
            _fileSystemMock.Move(target, backup);
            _fileSystemMock.CreateFileSymlink(target, source);
            _fileSystemMock.Move(backup, target);
        });
        _fileSystemMock.DidNotReceive().Delete(backup);
    }

    [Fact]
    public void LinkDotfiles_WithForceRemovesBackupAfterSuccessfulReplacement()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var backup = target + ".dotfileslinker-backup";

        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns([source]);
        _fileSystemMock.PathExists(target).Returns(true);
        _fileSystemMock.PathExists(backup).Returns(false);

        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: true);

        Received.InOrder(() =>
        {
            _fileSystemMock.Move(target, backup);
            _fileSystemMock.CreateFileSymlink(target, source);
            _fileSystemMock.Delete(backup);
        });
    }

    [Fact]
    public void LinkDotfiles_WithForceUsesNumberedBackupWhenDefaultNameExists()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var backup = target + ".dotfileslinker-backup";
        var numberedBackup = backup + ".1";

        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns([source]);
        _fileSystemMock.PathExists(target).Returns(true);
        _fileSystemMock.PathExists(backup).Returns(true);
        _fileSystemMock.PathExists(numberedBackup).Returns(false);

        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: true);

        _fileSystemMock.Received(1).Move(target, numberedBackup);
        _fileSystemMock.Received(1).Delete(numberedBackup);
        _fileSystemMock.DidNotReceive().Move(target, backup);
    }

    [Fact]
    public void LinkDotfiles_WithForceRestoresOriginalWhenBackupCleanupFails()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "repo");
        var userHome = Path.Combine(Path.GetTempPath(), "home", "user");
        var source = Path.Combine(repoRoot, ".settings");
        var target = Path.Combine(userHome, ".settings");
        var backup = target + ".dotfileslinker-backup";
        var targetInspectionCount = 0;

        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns([source]);
        _fileSystemMock.PathExists(target).Returns(_ => ++targetInspectionCount <= 2);
        _fileSystemMock.PathExists(backup).Returns(false);
        _fileSystemMock
            .When(fs => fs.Delete(backup))
            .Do(_ => throw new IOException("backup cleanup failed"));

        var exception = Assert.Throws<IOException>(() =>
            _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite: true));

        Assert.Contains("original target was restored", exception.Message);
        Received.InOrder(() =>
        {
            _fileSystemMock.Move(target, backup);
            _fileSystemMock.CreateFileSymlink(target, source);
            _fileSystemMock.Delete(backup);
            _fileSystemMock.Delete(target);
            _fileSystemMock.Move(backup, target);
        });
    }
}
