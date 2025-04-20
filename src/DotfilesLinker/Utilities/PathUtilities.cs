namespace DotfilesLinker.Utilities;

public static class PathUtilities
{
    public static bool PathEquals(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
