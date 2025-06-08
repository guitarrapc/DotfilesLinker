using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using System.Reflection;

namespace DotfilesLinker.Tests;

public class FileLinkerServicePatternTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly FileLinkerService _service;

    public FileLinkerServicePatternTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _service = new FileLinkerService(_fileSystemMock);
    }

    [Theory]
    [InlineData(".DS_Store", true)]           // macOS hidden file
    [InlineData("._.DS_Store", true)]         // macOS metadata file
    [InlineData("Thumbs.db", true)]           // Windows thumbnail cache
    [InlineData("Desktop.ini", true)]         // Windows folder settings
    [InlineData("desktop.ini", true)]         // Case insensitive variant
    [InlineData(".git", true)]                // VCS directory
    [InlineData(".svn", true)]                // VCS directory
    [InlineData("normal.txt", false)]         // Regular file
    [InlineData(".dotfile", false)]           // Regular dotfile
    [InlineData(".gitconfig", false)]         // Regular git config file
    public void ShouldIgnoreFile_DefaultPatterns_ShouldMatchCorrectly(string fileName, bool expectedResult)
    {
        // Arrange
        var emptyUserPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Act
        bool result = InvokeShouldIgnoreFile(fileName, emptyUserPatterns);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("backup.bak", true)]          // Backup file
    [InlineData("temp.tmp", true)]            // Temporary file
    [InlineData(".vimrc.swp", true)]          // Vim swap file
    [InlineData(".config.swo", true)]         // Vim swap file
    [InlineData("script~", true)]             // Backup file with tilde
    [InlineData("normal.txt", false)]         // Regular file
    [InlineData(".vimrc", false)]             // Regular dotfile
    [InlineData("backup.txt", false)]         // Non-matching extension
    public void ShouldIgnoreFile_WildcardPatterns_ShouldMatchCorrectly(string fileName, bool expectedResult)
    {
        // Arrange
        var emptyUserPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Act
        bool result = InvokeShouldIgnoreFile(fileName, emptyUserPatterns);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("custom.txt", true)]         // User-defined pattern
    [InlineData("ignored.file", true)]       // User-defined pattern
    [InlineData(".vimrc.local", true)]       // User-defined pattern
    [InlineData("normal.txt", false)]        // Non-matching file
    public void ShouldIgnoreFile_UserDefinedPatterns_ShouldMatchCorrectly(string fileName, bool expectedResult)
    {
        // Arrange
        var userPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "custom.txt",
            "ignored.file",
            ".vimrc.local"
        };

        // Act
        bool result = InvokeShouldIgnoreFile(fileName, userPatterns);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("*.log", "app.log", true)]
    [InlineData("*.log", "system.log", true)]
    [InlineData("*.log", "log.txt", false)]
    [InlineData("log*", "log.txt", true)]
    [InlineData("log*", "logging.log", true)]
    [InlineData("log*", "app.log", false)]
    [InlineData("app*.log", "app.log", true)]
    [InlineData("app*.log", "app-debug.log", true)]
    [InlineData("app*.log", "application.txt", false)]
    public void IsWildcardMatch_ShouldMatchCorrectly(string pattern, string fileName, bool expectedResult)
    {
        // Arrange & Act
        bool result = InvokeIsWildcardMatch(fileName, pattern);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(".DS_Store", true)]
    [InlineData("Thumbs.db", true)]
    [InlineData("app.log", true)]        // User-defined pattern
    [InlineData("backup.bak", true)]     // Default wildcard pattern
    [InlineData("normal.txt", false)]
    public void ShouldIgnoreFile_CombinedPatterns_ShouldMatchCorrectly(string fileName, bool expectedResult)
    {
        // Arrange
        var userPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "*.log",
            "custom.txt"
        };

        // Act
        bool result = InvokeShouldIgnoreFile(fileName, userPatterns);

        // Assert
        Assert.Equal(expectedResult, result);
    }    // Helper method to invoke the private ShouldIgnoreFileEnhanced method via reflection
    private bool InvokeShouldIgnoreFile(string fileName, HashSet<string> userIgnorePatterns)
    {
        var methodInfo = typeof(FileLinkerService).GetMethod("ShouldIgnoreFileEnhanced",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // The new method requires more parameters: filePath, fileName, isDir, userIgnorePatterns
        // For tests, we'll use fileName as filePath and set isDir to false
        return (bool)methodInfo!.Invoke(_service, new object[] { fileName, fileName, false, userIgnorePatterns })!;
    }    // Helper method to invoke the WildcardMatcher.IsMatch method
    private bool InvokeIsWildcardMatch(string fileName, string pattern)
    {
        return WildcardMatcher.IsMatch(fileName, pattern);
    }
}
