namespace DotfilesLinker.Utilities;

/// <summary>
/// Provides utility methods for working with file and directory paths.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Compares two file or directory paths for equality, considering their absolute paths.
    /// </summary>
    /// <param name="a">The first path to compare.</param>
    /// <param name="b">The second path to compare.</param>
    /// <returns>
    /// <c>true</c> if the absolute paths of <paramref name="a"/> and <paramref name="b"/> are equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method resolves the absolute paths of the input paths using <see cref="Path.GetFullPath(string)"/>
    /// and performs a case-sensitive comparison using <see cref="StringComparison.Ordinal"/>.
    /// </remarks>
    public static bool PathEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.Ordinal);
    }
}
