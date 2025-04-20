using DotfilesLinker.Infrastructure;
using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

public sealed class FileLinkerService(IFileSystem fileSystem)
{
    /*-----------------------------------------------------------
     * public APIs
     *----------------------------------------------------------*/

    /// <summary>
    /// リポジトリ直下の「隠しファイル」（.gitignore 等）を
    /// ユーザーホームにシンボリックリンクします。
    /// </summary>
    public void LinkDotfiles(
        string repoRoot,
        string userHome,
        string ignoreFileName,
        bool overwrite)
    {
        var ignore = LoadIgnoreList(Path.Combine(repoRoot, ignoreFileName));

        foreach (var src in fileSystem.EnumerateFiles(repoRoot, ".*", recursive: false)
                               .Where(p => !ignore.Contains(Path.GetFileName(p))))
        {
            var dst = Path.Combine(userHome, Path.GetFileName(src));
            LinkFile(src, dst, overwrite);
        }
    }

    /// <summary>
    /// リポジトリの home/ 以下をユーザーホーム配下に
    /// そのままディレクトリツリーごとリンクします。
    /// </summary>
    public void LinkHomeFiles(
        string repoRoot,
        string userHome,
        bool overwrite)
    {
        var homeRoot = Path.Combine(repoRoot, "home");
        if (!fileSystem.DirectoryExists(homeRoot)) return;

        foreach (var src in fileSystem.EnumerateFiles(homeRoot, "*", recursive: true))
        {
            var rel = Path.GetRelativePath(homeRoot, src);
            var dst = Path.Combine(userHome, rel);

            fileSystem.EnsureDirectory(Path.GetDirectoryName(dst)!);
            LinkFile(src, dst, overwrite);
        }
    }

    /*-----------------------------------------------------------
     * private helpers
     *----------------------------------------------------------*/

    private void LinkFile(string source, string target, bool overwrite)
    {
        bool exists = fileSystem.FileExists(target) || fileSystem.DirectoryExists(target);

        if (exists)
        {
            var currentLinkTarget = fileSystem.GetLinkTarget(target);

            // 既に正しいリンクが張られているなら何もしない
            if (currentLinkTarget is not null && PathUtilities.PathEquals(currentLinkTarget, source))
                return;

            if (!overwrite)
                throw new InvalidOperationException($"'{target}' already exists; use --force=y to overwrite.");

            fileSystem.Delete(target);
        }

        // ディレクトリ or ファイルで API を分ける
        if (fileSystem.DirectoryExists(source))
        {
            fileSystem.CreateDirectorySymlink(target, source);
        }
        else
        {
            fileSystem.CreateFileSymlink(target, source);
        }
    }

    private static HashSet<string> LoadIgnoreList(string ignoreFilePath)
    {
        if (!File.Exists(ignoreFilePath))
            return new(StringComparer.OrdinalIgnoreCase);

        return File.ReadAllLines(ignoreFilePath)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(line => line.Trim())
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
