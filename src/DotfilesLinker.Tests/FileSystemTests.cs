using DotfilesLinker.Infrastructure;

namespace DotfilesLinker.Tests;

public sealed class FileSystemTests : IDisposable
{
    private readonly DefaultFileSystem _fileSystem = new();
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"DotfilesLinker-{Guid.NewGuid():N}");

    public FileSystemTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void FileExists_ReflectsActualFileSystemState()
    {
        var path = Path.Combine(_root, "file.txt");

        Assert.False(_fileSystem.FileExists(path));

        File.WriteAllText(path, "content");

        Assert.True(_fileSystem.FileExists(path));
    }

    [Fact]
    public void DirectoryExists_ReflectsActualFileSystemState()
    {
        var path = Path.Combine(_root, "directory");

        Assert.False(_fileSystem.DirectoryExists(path));

        Directory.CreateDirectory(path);

        Assert.True(_fileSystem.DirectoryExists(path));
    }

    [Fact]
    public void PathExists_ReflectsFilesDirectoriesAndDanglingLinks()
    {
        var file = CreateFile("existing.txt");
        var directory = Path.Combine(_root, "existing-directory");
        var danglingLink = Path.Combine(_root, "dangling-link.txt");
        Directory.CreateDirectory(directory);
        _fileSystem.CreateFileSymlink(danglingLink, Path.Combine(_root, "missing.txt"));

        Assert.True(_fileSystem.PathExists(file));
        Assert.True(_fileSystem.PathExists(directory));
        Assert.True(_fileSystem.PathExists(danglingLink));
        Assert.False(_fileSystem.PathExists(Path.Combine(_root, "missing")));
    }

    [Fact]
    public void EnsureDirectory_CreatesDirectoryAndIsIdempotent()
    {
        var path = Path.Combine(_root, "parent", "child");

        _fileSystem.EnsureDirectory(path);
        _fileSystem.EnsureDirectory(path);

        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void ReadAllLines_ReadsActualFileAndThrowsForMissingFile()
    {
        var path = Path.Combine(_root, "lines.txt");
        string[] expected = ["line1", "line2", "line3"];
        File.WriteAllLines(path, expected);

        Assert.Equal(expected, _fileSystem.ReadAllLines(path));
        Assert.Throws<FileNotFoundException>(() =>
            _fileSystem.ReadAllLines(Path.Combine(_root, "missing.txt")));
    }

    [Fact]
    public void EnumerateFiles_HonorsPatternAndRecursion()
    {
        var nested = Path.Combine(_root, "nested");
        Directory.CreateDirectory(nested);
        var rootText = CreateFile("root.txt");
        CreateFile("root.json");
        var nestedText = CreateFile(Path.Combine("nested", "nested.txt"));

        var topLevel = _fileSystem.EnumerateFiles(_root, "*.txt", recursive: false).ToHashSet();
        var recursive = _fileSystem.EnumerateFiles(_root, "*.txt", recursive: true).ToHashSet();

        Assert.Equal([rootText], topLevel);
        Assert.Equal(2, recursive.Count);
        Assert.Contains(rootText, recursive);
        Assert.Contains(nestedText, recursive);
    }

    [Fact]
    public void EnumerateDirectories_ReturnsImmediateChildrenOnly()
    {
        var first = Path.Combine(_root, "first");
        var second = Path.Combine(_root, "second");
        var nested = Path.Combine(first, "nested");
        Directory.CreateDirectory(nested);
        Directory.CreateDirectory(second);

        var directories = _fileSystem.EnumerateDirectories(_root).ToHashSet();

        Assert.Equal(2, directories.Count);
        Assert.Contains(first, directories);
        Assert.Contains(second, directories);
        Assert.DoesNotContain(nested, directories);
    }

    [Fact]
    public void Delete_RemovesFileAndEmptyDirectoryAndIgnoresMissingPath()
    {
        var file = CreateFile("delete.txt");
        var directory = Path.Combine(_root, "empty");
        Directory.CreateDirectory(directory);

        _fileSystem.Delete(file);
        _fileSystem.Delete(directory);
        var exception = Record.Exception(() =>
            _fileSystem.Delete(Path.Combine(_root, "missing")));

        Assert.False(File.Exists(file));
        Assert.False(Directory.Exists(directory));
        Assert.Null(exception);
    }

    [Fact]
    public void DeleteBackup_RemovesNonEmptyDirectory()
    {
        var original = Path.Combine(_root, "settings");
        var backup = original + ".dotfileslinker-backup";
        Directory.CreateDirectory(backup);
        File.WriteAllText(Path.Combine(backup, "existing.txt"), "content");

        _fileSystem.DeleteBackup(backup, original);

        Assert.False(Directory.Exists(backup));
    }

    [Fact]
    public void DeleteBackup_RejectsPathNotGeneratedFromOriginalTarget()
    {
        var original = Path.Combine(_root, "settings");
        var unrelated = Path.Combine(_root, "unrelated");
        Directory.CreateDirectory(unrelated);

        Assert.Throws<InvalidOperationException>(() =>
            _fileSystem.DeleteBackup(unrelated, original));
        Assert.True(Directory.Exists(unrelated));
    }

    [Fact]
    public void DeleteBackup_RemovesDirectoryLinkWithoutFollowingTarget()
    {
        var original = Path.Combine(_root, "settings");
        var backup = original + ".dotfileslinker-backup";
        var externalDirectory = Path.Combine(_root, "external");
        Directory.CreateDirectory(externalDirectory);
        var externalFile = Path.Combine(externalDirectory, "keep.txt");
        File.WriteAllText(externalFile, "content");
        Directory.CreateSymbolicLink(backup, externalDirectory);

        _fileSystem.DeleteBackup(backup, original);

        Assert.False(Directory.Exists(backup));
        Assert.True(File.Exists(externalFile));
    }

    [Fact]
    public void Move_RenamesFileDirectoryAndSymbolicLink()
    {
        var file = CreateFile("move-file.txt");
        var movedFile = Path.Combine(_root, "moved-file.txt");
        var directory = Path.Combine(_root, "move-directory");
        var movedDirectory = Path.Combine(_root, "moved-directory");
        var link = Path.Combine(_root, "move-link.txt");
        var movedLink = Path.Combine(_root, "moved-link.txt");
        var directoryLinkTarget = Path.Combine(_root, "directory-link-target");
        var directoryLink = Path.Combine(_root, "move-directory-link");
        var movedDirectoryLink = Path.Combine(_root, "moved-directory-link");
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(directoryLinkTarget);
        _fileSystem.CreateFileSymlink(link, file);
        _fileSystem.CreateDirectorySymlink(directoryLink, directoryLinkTarget);

        _fileSystem.Move(file, movedFile);
        _fileSystem.Move(directory, movedDirectory);
        _fileSystem.Move(link, movedLink);
        _fileSystem.Move(directoryLink, movedDirectoryLink);

        Assert.True(File.Exists(movedFile));
        Assert.True(Directory.Exists(movedDirectory));
        Assert.Equal(file, _fileSystem.GetLinkTarget(movedLink));
        Assert.Equal(directoryLinkTarget, _fileSystem.GetLinkTarget(movedDirectoryLink));
    }

    [Fact]
    public void CreateFileSymlink_CreatesLinkAndReportsTarget()
    {
        var target = CreateFile(Path.Combine("targets", "file.txt"));
        var link = Path.Combine(_root, "file-link.txt");

        _fileSystem.CreateFileSymlink(link, target);

        Assert.True(_fileSystem.FileExists(link));
        Assert.Equal(target, _fileSystem.GetLinkTarget(link));
        Assert.Equal("content", File.ReadAllText(link));
    }

    [Fact]
    public void GetLinkTarget_PreservesRelativeFileLinkTarget()
    {
        var target = CreateFile(Path.Combine("targets", "relative.txt"));
        var linksDirectory = Path.Combine(_root, "links");
        Directory.CreateDirectory(linksDirectory);
        var link = Path.Combine(linksDirectory, "relative-link.txt");
        var relativeTarget = Path.GetRelativePath(linksDirectory, target);

        _fileSystem.CreateFileSymlink(link, relativeTarget);

        Assert.Equal(relativeTarget, _fileSystem.GetLinkTarget(link));
        Assert.Equal("content", File.ReadAllText(link));
    }

    [Fact]
    public void CreateDirectorySymlink_CreatesLinkAndReportsTarget()
    {
        var target = Path.Combine(_root, "target-directory");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "file.txt"), "content");
        var link = Path.Combine(_root, "directory-link");

        _fileSystem.CreateDirectorySymlink(link, target);

        Assert.True(_fileSystem.DirectoryExists(link));
        Assert.Equal(target, _fileSystem.GetLinkTarget(link));
        Assert.Equal("content", File.ReadAllText(Path.Combine(link, "file.txt")));
    }

    [Fact]
    public void IsSymbolicLink_DetectsFileAndDirectoryLinks()
    {
        var fileTarget = CreateFile(Path.Combine("targets", "file-target.txt"));
        var directoryTarget = Path.Combine(_root, "directory-target");
        Directory.CreateDirectory(directoryTarget);
        var fileLink = Path.Combine(_root, "file-link.txt");
        var directoryLink = Path.Combine(_root, "directory-link");
        _fileSystem.CreateFileSymlink(fileLink, fileTarget);
        _fileSystem.CreateDirectorySymlink(directoryLink, directoryTarget);

        Assert.False(_fileSystem.IsSymbolicLink(fileTarget));
        Assert.False(_fileSystem.IsSymbolicLink(directoryTarget));
        Assert.True(_fileSystem.IsSymbolicLink(fileLink));
        Assert.True(_fileSystem.IsSymbolicLink(directoryLink));
    }

    [Fact]
    public void Delete_RemovesSymbolicLinkWithoutDeletingTarget()
    {
        var target = CreateFile(Path.Combine("targets", "preserved.txt"));
        var link = Path.Combine(_root, "preserved-link.txt");
        _fileSystem.CreateFileSymlink(link, target);

        _fileSystem.Delete(link);

        Assert.False(File.Exists(link));
        Assert.True(File.Exists(target));
    }

    [Fact]
    public void GetLinkTarget_ReturnsNullForRegularFileAndDirectory()
    {
        var file = CreateFile("regular.txt");
        var directory = Path.Combine(_root, "regular-directory");
        Directory.CreateDirectory(directory);

        Assert.Null(_fileSystem.GetLinkTarget(file));
        Assert.Null(_fileSystem.GetLinkTarget(directory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateFile(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "content");
        return path;
    }
}
