namespace DotfilesLinker.Tests;

public class GitignoreMatcherTests
{
    [Fact]
    public void IsMatch_ExactPathMatch_ReturnsTrue()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "test/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_SimpleWildcardInPath_ReturnsTrue()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "test/*.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectoryOnlyPattern_ReturnsTrueForDirectory()
    {
        // Arrange
        string path = "test/dir";
        string pattern = "test/dir/";
        bool isDir = true;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectoryOnlyPattern_ReturnsFalseForFile()
    {
        // Arrange
        string path = "test/dir";
        string pattern = "test/dir/";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskWildcard_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "test/**/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskAtEnd_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "test/**";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskAtBeginning_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "**/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NonMatchingPath_ReturnsFalse()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "other/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskWithOtherSegments_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/subsub/file.txt";
        string pattern = "test/**/subsub/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }
}
