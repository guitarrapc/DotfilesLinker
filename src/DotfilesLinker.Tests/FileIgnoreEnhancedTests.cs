using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;

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

    [Fact]
    public void LinkDotfiles_IgnoresDefaultPatterns()
    {
        // Arrange
        string repoRoot = Path.Combine(Path.DirectorySeparatorChar.ToString(), "repo");
        string userHome = Path.Combine(Path.DirectorySeparatorChar.ToString(), "home");
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock to return .DS_Store file in repository root
        string dsStorePath = Path.Combine(repoRoot, ".DS_Store");
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false)
            .Returns(new[] { dsStorePath });

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
        string repoRoot = Path.Combine(Path.DirectorySeparatorChar.ToString(), "repo");
        string userHome = Path.Combine(Path.DirectorySeparatorChar.ToString(), "home");
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock to return .bashrc and .vimrc files in repository root
        string bashrcPath = Path.Combine(repoRoot, ".bashrc");
        string vimrcPath = Path.Combine(repoRoot, ".vimrc");
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false)
            .Returns(new[] { bashrcPath, vimrcPath });

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
        string repoRoot = Path.Combine(Path.DirectorySeparatorChar.ToString(), "repo");
        string homeDir = Path.Combine(repoRoot, "HOME");
        string userHome = Path.Combine(Path.DirectorySeparatorChar.ToString(), "home");
        string ignoreFileName = ".linkignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock for HOME directory structure
        _fileSystemMock.DirectoryExists(homeDir).Returns(true);
        _fileSystemMock.EnumerateFiles(homeDir, "*", false)
            .Returns(new[] {
                Path.Combine(homeDir, "config", "app.json"),
                Path.Combine(homeDir, "config", "settings.json"),
                Path.Combine(homeDir, "log", "app.log")
            });

        // Setup ignore file with gitignore style pattern
        _fileSystemMock.FileExists(Path.Combine(repoRoot, ignoreFileName))
            .Returns(true);
        _fileSystemMock.ReadAllLines(Path.Combine(repoRoot, ignoreFileName))
            .Returns(new[] { "HOME/config/settings.json", "HOME/log/**" });

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

    [Fact]
    public void LinkDotfiles_DoesNotDescendIntoIgnoredDirectory()
    {
        var repoRoot = Path.Combine(Path.DirectorySeparatorChar.ToString(), "repo");
        var homeDir = Path.Combine(repoRoot, "HOME");
        var cacheDir = Path.Combine(homeDir, "cache");
        var ignoreFileName = ".linkignore";
        var ignoreFilePath = Path.Combine(repoRoot, ignoreFileName);

        _fileSystemMock.DirectoryExists(homeDir).Returns(true);
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(Array.Empty<string>());
        _fileSystemMock.EnumerateDirectories(homeDir).Returns([cacheDir]);
        _fileSystemMock.FileExists(ignoreFilePath).Returns(true);
        _fileSystemMock.ReadAllLines(ignoreFilePath).Returns(["HOME/cache/"]);

        _service.LinkDotfiles(
            repoRoot,
            Path.Combine(Path.DirectorySeparatorChar.ToString(), "home"),
            ignoreFileName,
            overwrite: false,
            dryRun: true);

        _fileSystemMock.Received(1).EnumerateDirectories(homeDir);
        _fileSystemMock.DidNotReceive().EnumerateDirectories(cacheDir);
        _fileSystemMock.DidNotReceive().EnumerateFiles(cacheDir, Arg.Any<string>(), Arg.Any<bool>());
    }
}
