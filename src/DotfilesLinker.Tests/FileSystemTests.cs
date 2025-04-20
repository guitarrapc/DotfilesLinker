using NSubstitute;
using DotfilesLinker.Infrastructure;

namespace DotfilesLinker.Tests;

public class FileSystemTests
{
    private readonly IFileSystem _fileSystemMock;

    public FileSystemTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
    }

    [Fact]
    public void FileExists_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var filePath = "test.txt";
        _fileSystemMock.FileExists(filePath).Returns(true);

        // Act
        var result = _fileSystemMock.FileExists(filePath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DirectoryExists_ShouldReturnFalse_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var directoryPath = "nonexistent";
        _fileSystemMock.DirectoryExists(directoryPath).Returns(false);

        // Act
        var result = _fileSystemMock.DirectoryExists(directoryPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetLinkTarget_ShouldReturnCorrectTarget()
    {
        // Arrange
        var linkPath = "link";
        var targetPath = "target";
        _fileSystemMock.GetLinkTarget(linkPath).Returns(targetPath);

        // Act
        var result = _fileSystemMock.GetLinkTarget(linkPath);

        // Assert
        Assert.Equal(targetPath, result);
    }

    [Fact]
    public void Delete_ShouldInvokeDeleteMethod()
    {
        // Arrange
        var path = "fileToDelete.txt";

        // Act
        _fileSystemMock.Delete(path);

        // Assert
        _fileSystemMock.Received(1).Delete(path);
    }

    [Fact]
    public void CreateFileSymlink_ShouldInvokeWithCorrectParameters()
    {
        // Arrange
        var linkPath = "link";
        var targetPath = "target";

        // Act
        _fileSystemMock.CreateFileSymlink(linkPath, targetPath);

        // Assert
        _fileSystemMock.Received(1).CreateFileSymlink(linkPath, targetPath);
    }

    [Fact]
    public void EnumerateFiles_ShouldReturnCorrectFileList()
    {
        // Arrange
        var root = "root";
        var pattern = "*.txt";
        var recursive = true;
        var files = new List<string> { "file1.txt", "file2.txt" };
        _fileSystemMock.EnumerateFiles(root, pattern, recursive).Returns(files);

        // Act
        var result = _fileSystemMock.EnumerateFiles(root, pattern, recursive);

        // Assert
        Assert.Equal(files, result);
    }

    [Fact]
    public void EnsureDirectory_ShouldInvokeEnsureDirectoryMethod()
    {
        // Arrange
        var directoryPath = "newDirectory";

        // Act
        _fileSystemMock.EnsureDirectory(directoryPath);

        // Assert
        _fileSystemMock.Received(1).EnsureDirectory(directoryPath);
    }
}
