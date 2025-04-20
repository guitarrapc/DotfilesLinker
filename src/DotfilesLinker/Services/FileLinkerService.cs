using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Provides functionality to link dotfiles from a repository to the user's home directory or system root.
/// </summary>
public sealed class FileLinkerService(IFileSystem fileSystem)
{
    /*-----------------------------------------------------------
     * public APIs
     *----------------------------------------------------------*/

    /// <summary>
    /// Links dotfiles from the specified repository to the user's home directory or system root.
    /// </summary>
    /// <param name="repoRoot">The root directory of the dotfiles repository.</param>
    /// <param name="userHome">The user's home directory path.</param>
    /// <param name="ignoreFileName">The name of the ignore file containing patterns to exclude.</param>
    /// <param name="overwrite">Whether to overwrite existing files or directories.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a target file or directory already exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    public void LinkDotfiles(string repoRoot, string userHome, string ignoreFileName, bool overwrite)
    {
        // Filter files in the root of the repository
        var ignore = LoadIgnoreList(Path.Combine(repoRoot, ignoreFileName));
        var files = fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false)
            .Where(p => !ignore.Contains(Path.GetFileName(p)));

        // Link files in the root of the repository to $HOME
        foreach (var src in files)
        {
            var dst = Path.Combine(userHome, Path.GetFileName(src));
            LinkFile(src, dst, overwrite);
        }

        // Link files in the HOME directory which starts with '.'
        var homeRoot = Path.Combine(repoRoot, "HOME");
        if (fileSystem.DirectoryExists(homeRoot))
        {
            foreach (var src in fileSystem.EnumerateFiles(homeRoot, "*", recursive: true))
            {
                var rel = Path.GetRelativePath(homeRoot, src);
                var dst = Path.Combine(userHome, rel);

                fileSystem.EnsureDirectory(Path.GetDirectoryName(dst)!);
                LinkFile(src, dst, overwrite);
            }
        }

        // Link files in the ROOT directory (Linux/macOS only)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var rootRoot = Path.Combine(repoRoot, "ROOT");
            if (fileSystem.DirectoryExists(rootRoot))
            {
                foreach (var src in fileSystem.EnumerateFiles(rootRoot, "*", recursive: true))
                {
                    var rel = Path.GetRelativePath(rootRoot, src);
                    var dst = Path.Combine("/", rel);

                    fileSystem.EnsureDirectory(Path.GetDirectoryName(dst)!);
                    LinkFile(src, dst, overwrite);
                }
            }
        }
    }

    /*-----------------------------------------------------------
     * private helpers
     *----------------------------------------------------------*/

    /// <summary>
    /// Creates a symbolic link from the source to the target path.
    /// </summary>
    /// <param name="source">The source file or directory path.</param>
    /// <param name="target">The target file or directory path.</param>
    /// <param name="overwrite">Whether to overwrite the target if it already exists.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the target exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    private void LinkFile(string source, string target, bool overwrite)
    {
        bool exists = fileSystem.FileExists(target) || fileSystem.DirectoryExists(target);

        if (exists)
        {
            var currentLinkTarget = fileSystem.GetLinkTarget(target);

            // If the target is a symlink and points to the same file, do nothing
            if (currentLinkTarget is not null && PathUtilities.PathEquals(currentLinkTarget, source))
                return;

            if (!overwrite)
                throw new InvalidOperationException($"'{target}' already exists; use --force=y to overwrite.");

            fileSystem.Delete(target);
        }

        // Create the link
        if (fileSystem.DirectoryExists(source))
        {
            fileSystem.CreateDirectorySymlink(target, source);
        }
        else
        {
            fileSystem.CreateFileSymlink(target, source);
        }
    }

    /// <summary>
    /// Loads the ignore list from the specified file.
    /// </summary>
    /// <param name="ignoreFilePath">The path to the ignore file.</param>
    /// <returns>A set of file or directory names to ignore.</returns>
    private static HashSet<string> LoadIgnoreList(string ignoreFilePath)
    {
        if (!File.Exists(ignoreFilePath))
            return new(StringComparer.Ordinal);

        return File.ReadAllLines(ignoreFilePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(line => line.Trim())
                   .ToHashSet(StringComparer.Ordinal);
    }
}
