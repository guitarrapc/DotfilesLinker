using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using NSubstitute;
using Xunit;

namespace DotfilesLinker.Tests;

public class FileIgnoreEnhancedTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly ILogger _loggerMock;
    private readonly FileLinkerService _service;

    public FileIgnoreEnhancedTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _loggerMock = Substitute.For<ILogger>();
        _service = new FileLinkerService(_fileSystemMock, _loggerMock);
    }

    // We need to test the private method ShouldIgnoreFileEnhanced
    // Since it's private, we'll use the public LinkDotfiles method and verify the behavior    [Fact]
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
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false)
            .Returns(new[] { @"C:\repo\.DS_Store" });

        _fileSystemMock.FileExists(Path.Combine(repoRoot, ignoreFileName))
            .Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify no attempt was made to create a symlink for the .DS_Store file
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>());
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
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false)
            .Returns(new[] { @"C:\repo\.bashrc", @"C:\repo\.vimrc" });

        // Setup ignore file with pattern that ignores all dot files but includes .vimrc
        _fileSystemMock.FileExists(Path.Combine(repoRoot, ignoreFileName))
            .Returns(true);
        _fileSystemMock.ReadAllLines(Path.Combine(repoRoot, ignoreFileName))
            .Returns(new[] { ".*", "!.vimrc" });

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify attempt was made to create symlink only for .vimrc
        _loggerMock.Received().Success(Arg.Is<string>(s => s.Contains(".vimrc")));
        _loggerMock.DidNotReceive().Success(Arg.Is<string>(s => s.Contains(".bashrc")));
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
        _fileSystemMock.DirectoryExists(homeDir).Returns(true);
        _fileSystemMock.EnumerateFiles(homeDir, "*", true)
            .Returns(new[] {
                Path.Combine(homeDir, "config", "app.json"),
                Path.Combine(homeDir, "config", "settings.json"),
                Path.Combine(homeDir, "log", "app.log")
            });

        // Setup ignore file with gitignore style pattern
        _fileSystemMock.FileExists(Path.Combine(repoRoot, ignoreFileName))
            .Returns(true);
        _fileSystemMock.ReadAllLines(Path.Combine(repoRoot, ignoreFileName))
            .Returns(new[] { "config/settings.json", "log/**" });

        // Mark each file as not a directory
        _fileSystemMock.DirectoryExists(Arg.Any<string>()).Returns(false);
        _fileSystemMock.DirectoryExists(homeDir).Returns(true);

        // Setup for repoRoot files
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false)
            .Returns(Array.Empty<string>());

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert - Verify only app.json was not ignored
        _loggerMock.Received().Verbose(Arg.Is<string>(s => s.Contains("app.json") && s.Contains("Linking")));
        _loggerMock.DidNotReceive().Verbose(Arg.Is<string>(s => s.Contains("settings.json") && s.Contains("Linking")));
        _loggerMock.DidNotReceive().Verbose(Arg.Is<string>(s => s.Contains("app.log") && s.Contains("Linking")));
    }
}
