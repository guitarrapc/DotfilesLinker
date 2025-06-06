using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using NSubstitute;

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
        _fileSystemMock.FileExists(ignoreFilePath).Returns(true);

        // Mock ReadAllLines
        _fileSystemMock.ReadAllLines(ignoreFilePath).Returns(ignoredFiles);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite);

        // Assert
        _fileSystemMock.Received(1).CreateFileSymlink(Path.Combine(userHome, ".file1"), "/repo/.file1");
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Path.Combine(userHome, ".file2"), "/repo/.file2");
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
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(true);
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
        _fileSystemMock.EnumerateFiles(homeRoot, "*", true).Returns(filesInHome);
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
    }    [Fact]
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
        _fileSystemMock.EnumerateFiles(homeRoot, "*", true).Returns(filesInHome);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Mock GetRelativePath behavior
        _fileSystemMock.When(fs =>
            fs.CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>()))
            .Do(callInfo => {
                // Just capture the call but don't do anything
            });

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ".dotfiles_ignore", overwrite);

        // Assert
        // Verify that CreateFileSymlink was called only once and only for the non-OS specific file
        _fileSystemMock.Received(1).CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/.config/file1");        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), "/repo/HOME/.config/.DS_Store");
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
        _fileSystemMock.FileExists(ignoreFilePath).Returns(true);

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
}
