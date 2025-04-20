namespace DotfilesLinker.Infrastructure;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string? GetLinkTarget(string path);
    void Delete(string path);
    void CreateFileSymlink(string linkPath, string target);
    void CreateDirectorySymlink(string linkPath, string target);
    IEnumerable<string> EnumerateFiles(string root, string pattern, bool recursive);
    void EnsureDirectory(string path);
}