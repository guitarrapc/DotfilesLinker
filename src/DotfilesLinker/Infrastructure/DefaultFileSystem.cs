namespace DotfilesLinker.Infrastructure;

public sealed class DefaultFileSystem : IFileSystem
{
    public bool FileExists(string p) => File.Exists(p);
    public bool DirectoryExists(string p) => Directory.Exists(p);
    public string? GetLinkTarget(string p) =>
        new FileInfo(p).LinkTarget ?? new DirectoryInfo(p).LinkTarget;

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

    public void CreateFileSymlink(string link, string target) =>
        File.CreateSymbolicLink(link, target);

    public void CreateDirectorySymlink(string link, string target) =>
        Directory.CreateSymbolicLink(link, target);

    public IEnumerable<string> EnumerateFiles(string root, string pattern, bool recursive) =>
        Directory.EnumerateFiles(root, pattern,
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public void EnsureDirectory(string p) => Directory.CreateDirectory(p);
}
