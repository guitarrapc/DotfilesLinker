using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using Moq;
using Xunit;

namespace DotfilesLinker.Tests;

public class FileIgnoreEnhancedTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger> _mockLogger;
    private readonly FileLinkerService _service;

    public FileIgnoreEnhancedTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger>();
        _service = new FileLinkerService(_mockFileSystem.Object, _mockLogger.Object);
    }

    // We need to test the private method ShouldIgnoreFileEnhanced
    // Since it's private, we'll use the public LinkDotfiles method and verify the behavior

    [Fact]
    public void LinkDotfiles_IgnoresDefaultPatterns()
    {
        // Arrange
        string repoRoot = @"C:\repo";
        string userHome = @"C:\home";
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock to return .DS_Store file in repository root
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(repoRoot, ".*", false))
            .Returns(new[] { @"C:\repo\.DS_Store" });

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(repoRoot, ignoreFileName)))
            .Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify no attempt was made to create a symlink for the .DS_Store file
        _mockFileSystem.Verify(fs => fs.CreateFileSymlink(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LinkDotfiles_IgnoresNegationPatterns()
    {
        // Arrange
        string repoRoot = @"C:\repo";
        string userHome = @"C:\home";
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock to return .bashrc and .vimrc files in repository root
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(repoRoot, ".*", false))
            .Returns(new[] { @"C:\repo\.bashrc", @"C:\repo\.vimrc" });

        // Setup ignore file with pattern that ignores all dot files but includes .vimrc
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(repoRoot, ignoreFileName)))
            .Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllLines(Path.Combine(repoRoot, ignoreFileName)))
            .Returns(new[] { ".*", "!.vimrc" });

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify attempt was made to create symlink only for .vimrc
        _mockLogger.Verify(l => l.Success(It.Is<string>(s => s.Contains(".vimrc"))), Times.Once);
        _mockLogger.Verify(l => l.Success(It.Is<string>(s => s.Contains(".bashrc"))), Times.Never);
    }

    [Fact]
    public void LinkDotfiles_IgnoresGitignoreStylePatterns()
    {
        // Arrange
        string repoRoot = @"C:\repo";
        string homeDir = Path.Combine(repoRoot, "HOME");
        string userHome = @"C:\home";
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock for HOME directory structure
        _mockFileSystem.Setup(fs => fs.DirectoryExists(homeDir)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(homeDir, "*", true))
            .Returns(new[] {
                Path.Combine(homeDir, "config", "app.json"),
                Path.Combine(homeDir, "config", "settings.json"),
                Path.Combine(homeDir, "log", "app.log")
            });

        // Setup ignore file with gitignore style pattern
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(repoRoot, ignoreFileName)))
            .Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllLines(Path.Combine(repoRoot, ignoreFileName)))
            .Returns(new[] { "config/settings.json", "log/**" });

        // Mark each file as not a directory
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(homeDir)).Returns(true);

        // Setup for repoRoot files
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(repoRoot, ".*", false))
            .Returns(Array.Empty<string>());

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify only app.json was not ignored
        _mockLogger.Verify(l => l.Verbose(It.Is<string>(s => s.Contains("app.json") && s.Contains("Linking"))), Times.Once);
        _mockLogger.Verify(l => l.Verbose(It.Is<string>(s => s.Contains("settings.json") && s.Contains("Linking"))), Times.Never);
        _mockLogger.Verify(l => l.Verbose(It.Is<string>(s => s.Contains("app.log") && s.Contains("Linking"))), Times.Never);
    }
}
