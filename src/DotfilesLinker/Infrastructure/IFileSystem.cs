namespace DotfilesLinker.Infrastructure;

/// <summary>
/// Provides an abstraction for file system operations to support testing and platform-specific behavior.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the directory exists; otherwise, <c>false</c>.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Gets the target of a symbolic link at the specified path.
    /// </summary>
    /// <param name="path">The path to the symbolic link.</param>
    /// <returns>The target path of the symbolic link, or <c>null</c> if the path is not a symbolic link.</returns>
    string? GetLinkTarget(string path);

    /// <summary>
    /// Deletes the specified file or empty directory.
    /// </summary>
    /// <param name="path">The path of the file or directory to delete.</param>
    void Delete(string path);

    /// <summary>
    /// Creates a symbolic link to a file at the specified path.
    /// </summary>
    /// <param name="linkPath">The path where the symbolic link should be created.</param>
    /// <param name="target">The path to the target file.</param>
    void CreateFileSymlink(string linkPath, string target);

    /// <summary>
    /// Creates a symbolic link to a directory at the specified path.
    /// </summary>
    /// <param name="linkPath">The path where the symbolic link should be created.</param>
    /// <param name="target">The path to the target directory.</param>
    void CreateDirectorySymlink(string linkPath, string target);

    /// <summary>
    /// Enumerates files that match a specific pattern in a specified directory.
    /// </summary>
    /// <param name="root">The directory to search.</param>
    /// <param name="pattern">The search pattern to match against file names.</param>
    /// <param name="recursive">Whether to search subdirectories recursively.</param>
    /// <returns>An enumerable collection of file paths that match the search pattern.</returns>
    IEnumerable<string> EnumerateFiles(string root, string pattern, bool recursive);

    /// <summary>
    /// Creates a directory at the specified path if it does not already exist.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    void EnsureDirectory(string path);
}
