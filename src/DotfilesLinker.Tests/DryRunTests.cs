using DotfilesLinker.Infrastructure;
using DotfilesLinker.Services;
using NSubstitute;

namespace DotfilesLinker.Tests;

public class DryRunTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly ILogger _loggerMock;
    private readonly FileLinkerService _service;

    public DryRunTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _loggerMock = Substitute.For<ILogger>();
        _service = new FileLinkerService(_fileSystemMock, _loggerMock);
    }

    [Fact]
    public void LinkDotfiles_WithDryRun_ShouldNotCreateLinks()
    {
        // Arrange
        string repoRoot = "/repo";
        string userHome = "/home/user";
        string ignoreFileName = "dotfiles_ignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup mock to return some test files
        var filesInRepo = new[] { "/repo/.file1", "/repo/.file2" };
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);
        _fileSystemMock.DirectoryExists(Arg.Is<string>(s => s.Contains("/repo"))).Returns(true);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert
        // Verify that symlinks were not actually created
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>());
        _fileSystemMock.DidNotReceive().CreateDirectorySymlink(Arg.Any<string>(), Arg.Any<string>());

        // Verify that logger received messages with [DRY-RUN] prefix
        _loggerMock.Received().Info("DRY RUN MODE: No files will be actually linked");
        _loggerMock.Received().Info("DRY RUN COMPLETED: No files were actually linked");
        _loggerMock.Received().Success(Arg.Is<string>(s => s.StartsWith("[DRY-RUN]")));
    }

    [Fact]
    public void LinkDotfiles_WithoutDryRun_ShouldCreateLinks()
    {
        // Arrange
        string repoRoot = "/repo";
        string userHome = "/home/user";
        string ignoreFileName = "dotfiles_ignore";
        bool overwrite = true; // overwriteをtrueに変更して既存ファイルを上書きできるようにする
        bool dryRun = false;

        // Setup mock to return some test files
        var filesInRepo = new[] { "/repo/.file1", "/repo/.file2" };
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);

        // すべてのFileExistsチェックにfalseを返すように設定
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // 対象ディレクトリは存在するように設定
        _fileSystemMock.DirectoryExists(Arg.Any<string>()).Returns(true);

        // IsDirectoryExistsはfalseを返すように設定（ファイルとして扱われるように）
        _fileSystemMock.DirectoryExists(Arg.Is<string>(s => s.Contains(".file"))).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert
        // Verify that symlinks were actually created
        _fileSystemMock.Received().CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>());

        // Verify that logger did not receive messages with [DRY-RUN] prefix
        _loggerMock.DidNotReceive().Info("DRY RUN MODE: No files will be actually linked");
        _loggerMock.DidNotReceive().Info("DRY RUN COMPLETED: No files were actually linked");
        _loggerMock.DidNotReceive().Success(Arg.Is<string>(s => s.StartsWith("[DRY-RUN]")));
    }

    [Fact]
    public void LinkDotfiles_WithDryRun_ShouldNotDeleteExistingFiles()
    {
        // Arrange
        string repoRoot = "/repo";
        string userHome = "/home/user";
        string ignoreFileName = "dotfiles_ignore";
        bool overwrite = true; // Enable overwrite to test deletion behavior
        bool dryRun = true;

        // Setup mock to return some test files
        var filesInRepo = new[] { "/repo/.file1" };
        _fileSystemMock.EnumerateFiles(repoRoot, ".*", false).Returns(filesInRepo);

        // Simulate existing file
        _fileSystemMock.FileExists(Arg.Is<string>(s => s.Contains(".file1"))).Returns(true);
        _fileSystemMock.DirectoryExists(Arg.Is<string>(s => s.Contains("/repo"))).Returns(true);

        // GetLinkTargetのモックを設定
        _fileSystemMock.GetLinkTarget(Arg.Any<string>()).Returns("/some/other/path");

        // GetLinkTargetのモックを設定
        _fileSystemMock.GetLinkTarget(Arg.Any<string>()).Returns("/some/other/path");

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert
        // Verify that existing files were not deleted
        _fileSystemMock.DidNotReceive().Delete(Arg.Any<string>());

        // Verify that dry run logs were produced
        _loggerMock.Received().Verbose(Arg.Is<string>(s => s.StartsWith("[DRY-RUN] Would delete")));
    }

    [Fact]
    public void LinkDotfiles_WithDryRun_ShouldNotCreateDirectories()
    {
        // Arrange
        string repoRoot = "/repo";
        string userHome = "/home/user";
        string ignoreFileName = "dotfiles_ignore";
        bool overwrite = false;
        bool dryRun = true;

        // Setup HOME directory structure
        var homeRoot = Path.Combine(repoRoot, "HOME");
        var filesInHome = new[] { "/repo/HOME/.config/file1" };

        _fileSystemMock.DirectoryExists(Arg.Is<string>(s => s.Contains("/repo"))).Returns(true);
        _fileSystemMock.DirectoryExists(homeRoot).Returns(true);
        _fileSystemMock.EnumerateFiles(homeRoot, "*", true).Returns(filesInHome);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);

        // Act
        _service.LinkDotfiles(repoRoot, userHome, ignoreFileName, overwrite, dryRun);

        // Assert
        // Verify that directories were not created
        _fileSystemMock.DidNotReceive().EnsureDirectory(Arg.Any<string>());

        // Verify that symlinks were not created
        _fileSystemMock.DidNotReceive().CreateFileSymlink(Arg.Any<string>(), Arg.Any<string>());
    }
}
