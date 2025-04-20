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
}
