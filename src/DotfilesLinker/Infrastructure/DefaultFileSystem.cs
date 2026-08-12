namespace DotfilesLinker.Infrastructure;

internal sealed class DefaultFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public bool FileExists(string p) => File.Exists(p);
    /// <inheritdoc/>
    public bool DirectoryExists(string p) => Directory.Exists(p);
    /// <inheritdoc/>
    public bool IsSymbolicLink(string p) =>
        (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0;
    /// <inheritdoc/>
    public string? GetLinkTarget(string p) =>
        new FileInfo(p).LinkTarget ?? new DirectoryInfo(p).LinkTarget;

    /// <inheritdoc/>
    public void Delete(string p)
    {
        if (File.Exists(p))
        {
            File.Delete(p);
        }
        else if (Directory.Exists(p))
        {
            Directory.Delete(p, recursive: false);
        }
    }

    /// <inheritdoc/>
    public void CreateFileSymlink(string link, string target) =>
        File.CreateSymbolicLink(link, target);

    /// <inheritdoc/>
    public void CreateDirectorySymlink(string link, string target) =>
        Directory.CreateSymbolicLink(link, target);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string root, string pattern, bool recursive) =>
        Directory.EnumerateFiles(root, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string root) =>
        Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly);

    /// <inheritdoc/>
    public void EnsureDirectory(string p) => Directory.CreateDirectory(p);

    /// <inheritdoc/>
    public string[] ReadAllLines(string path) => File.ReadAllLines(path);
}
