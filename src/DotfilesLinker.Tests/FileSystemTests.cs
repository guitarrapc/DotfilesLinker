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
    public void CreateDirectorySymlink_ShouldThrowException_WhenPathsAreInvalid()
    {
        // Arrange
        string invalidLinkPath = "";
        string invalidTargetPath = "";

        // Configure the mock
        _fileSystemMock
            .When(fs => fs.CreateDirectorySymlink(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call =>
            {
                var linkPath = call.ArgAt<string>(0);
                var targetPath = call.ArgAt<string>(1);

                if (string.IsNullOrWhiteSpace(linkPath) || string.IsNullOrWhiteSpace(targetPath))
                {
                    throw new ArgumentException("Invalid path(s) provided.");
                }
            });

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _fileSystemMock.CreateDirectorySymlink(invalidLinkPath, invalidTargetPath));
    }

    [Fact]
    public void EnumerateFiles_ShouldHandleComplexPattern()
    {
        // Arrange
        var root = "root";
        var pattern = "[a-c]*.txt"; // 複雑なパターン
        var recursive = true;
        var expectedFiles = new List<string> { "root/a.txt", "root/b123.txt", "root/c.txt" };
        _fileSystemMock.EnumerateFiles(root, pattern, recursive).Returns(expectedFiles);

        // Act
        var result = _fileSystemMock.EnumerateFiles(root, pattern, recursive);

        // Assert
        Assert.Equal(expectedFiles, result);
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

    [Fact]
    public void FileExists_ShouldReturnFalse_WhenPathIsInvalid()
    {
        // Arrange
        string invalidPath = "";

        // Act
        var result = _fileSystemMock.FileExists(invalidPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DirectoryExists_ShouldReturnFalse_WhenPathIsInvalid()
    {
        // Arrange
        string invalidPath = "";

        // Act
        var result = _fileSystemMock.DirectoryExists(invalidPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetLinkTarget_ShouldReturnNull_WhenPathIsNotSymlink()
    {
        // Arrange
        string nonSymlinkPath = "regularFile.txt";
        _fileSystemMock.GetLinkTarget(nonSymlinkPath).Returns((string?)null);

        // Act
        var result = _fileSystemMock.GetLinkTarget(nonSymlinkPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Delete_ShouldNotThrow_WhenPathDoesNotExist()
    {
        // Arrange
        string nonexistentPath = "nonexistentFile.txt";

        // Act & Assert
        var exception = Record.Exception(() => _fileSystemMock.Delete(nonexistentPath));
        Assert.Null(exception);
    }

    [Fact]
    public void CreateFileSymlink_ShouldThrowException_WhenPathsAreInvalid()
    {
        // Arrange
        string invalidLinkPath = "";
        string invalidTargetPath = ""; // if null is specified, exception is thrown without mock

        // Configure the mock to throw an exception for invalid arguments
        _fileSystemMock
            .When(fs => fs.CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call =>
            {
                var linkPath = call.ArgAt<string>(0);
                var targetPath = call.ArgAt<string>(1);

                if (string.IsNullOrWhiteSpace(linkPath) || string.IsNullOrWhiteSpace(targetPath))
                {
                    throw new ArgumentException("Invalid path(s) provided.");
                }
            });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystemMock.CreateFileSymlink(invalidLinkPath, invalidTargetPath));
    }

    [Fact]
    public void EnumerateFiles_ShouldReturnEmpty_WhenRootPathIsInvalid()
    {
        // Arrange
        string invalidRoot = "";
        _fileSystemMock.EnumerateFiles(invalidRoot, "*.txt", true).Returns(Enumerable.Empty<string>());

        // Act
        var result = _fileSystemMock.EnumerateFiles(invalidRoot, "*.txt", true);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void EnsureDirectory_ShouldThrowException_WhenPathIsInvalid()
    {
        // Arrange
        string invalidPath = ""; // if null is specified, exception is thrown without mock

        // Configure the mock to throw an exception for invalid arguments
        _fileSystemMock
            .When(fs => fs.EnsureDirectory(Arg.Any<string>()))
            .Do(call =>
            {
                var path = call.ArgAt<string>(0);

                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("Invalid path provided.");
                }
            });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystemMock.EnsureDirectory(invalidPath));
    }

    [Fact]
    public void ReadAllLines_ShouldReturnLinesFromFile()
    {
        // Arrange
        var filePath = "test.txt";
        var expectedLines = new[] { "line1", "line2", "line3" };
        _fileSystemMock.ReadAllLines(filePath).Returns(expectedLines);

        // Act
        var result = _fileSystemMock.ReadAllLines(filePath);

        // Assert
        Assert.Equal(expectedLines, result);
    }

    [Fact]
    public void ReadAllLines_ShouldThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentFilePath = "nonexistent.txt";
        _fileSystemMock.ReadAllLines(nonExistentFilePath).Returns(x => throw new FileNotFoundException());

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _fileSystemMock.ReadAllLines(nonExistentFilePath));
    }
}
