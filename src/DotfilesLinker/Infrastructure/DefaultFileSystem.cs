namespace DotfilesLinker.Infrastructure;

internal sealed class DefaultFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public bool FileExists(string p) => File.Exists(p);
    /// <inheritdoc/>
    public bool DirectoryExists(string p) => Directory.Exists(p);
    /// <inheritdoc/>
    public bool PathExists(string p)
    {
        try
        {
            _ = File.GetAttributes(p);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }
    /// <inheritdoc/>
    public bool IsSymbolicLink(string p) =>
        (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0;
    /// <inheritdoc/>
    public string? GetLinkTarget(string p) =>
        new FileInfo(p).LinkTarget ?? new DirectoryInfo(p).LinkTarget;

    /// <inheritdoc/>
    public void Delete(string p)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(p);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            Directory.Delete(p, recursive: false);
            return;
        }

        File.Delete(p);
    }

    /// <inheritdoc/>
    public void DeleteBackup(string backupPath, string originalPath)
    {
        var fullBackupPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(backupPath));
        var fullOriginalPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(originalPath));
        var expectedBackupPath = fullOriginalPath + ".dotfileslinker-backup";

        if (!IsGeneratedBackupPath(fullBackupPath, expectedBackupPath))
        {
            throw new InvalidOperationException(
                $"Refusing to recursively delete '{backupPath}' because it is not a generated backup for '{originalPath}'.");
        }

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(fullBackupPath);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            Directory.Delete(
                fullBackupPath,
                recursive: (attributes & FileAttributes.ReparsePoint) == 0);
            return;
        }

        File.Delete(fullBackupPath);
    }

    /// <inheritdoc/>
    public void Move(string sourcePath, string destinationPath)
    {
        var attributes = File.GetAttributes(sourcePath);
        if ((attributes & FileAttributes.Directory) != 0)
        {
            Directory.Move(sourcePath, destinationPath);
            return;
        }

        File.Move(sourcePath, destinationPath);
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
    public void EnsureDirectory(string p, IList<string> createdDirectories)
    {
        var fullPath = Path.GetFullPath(p);
        var pathRoot = Path.GetPathRoot(fullPath);
        var missingDirectories = new Stack<string>();

        for (var current = fullPath; !Directory.Exists(current);)
        {
            if (pathRoot is not null && PathsEqual(current, pathRoot))
            {
                break;
            }

            missingDirectories.Push(current);
            var parent = Path.GetDirectoryName(current);
            if (parent is null || PathsEqual(parent, current))
            {
                break;
            }

            current = parent;
        }

        while (missingDirectories.TryPop(out var directory))
        {
            if (Directory.Exists(directory))
            {
                continue;
            }

            Directory.CreateDirectory(directory);
            createdDirectories.Add(directory);
        }
    }

    /// <inheritdoc/>
    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    private static bool IsGeneratedBackupPath(string backupPath, string expectedBackupPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(backupPath, expectedBackupPath, comparison))
        {
            return true;
        }

        var numberedPrefix = expectedBackupPath + ".";
        if (!backupPath.StartsWith(numberedPrefix, comparison))
        {
            return false;
        }

        return uint.TryParse(
                backupPath.AsSpan(numberedPrefix.Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var suffix) &&
            suffix > 0;
    }

    private static bool PathsEqual(string first, string second) =>
        string.Equals(
            first,
            second,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
}
