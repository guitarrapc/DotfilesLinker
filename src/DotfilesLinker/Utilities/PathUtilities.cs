using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Normalizes a path for consistent display across platforms.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized path string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string NormalizePath(string path)
    {
        // Use platform-specific path separator for display
        return OperatingSystem.IsWindows()
            ? path.Replace('/', '\\')
            : path.Replace('\\', '/');
    }

    /// <summary>
    /// Normalizes a path string for pattern matching.
    /// Always converts to forward slashes for consistent pattern matching across platforms.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized path with forward slashes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string NormalizePathForPatternMatching(string path)
    {
        // Always use forward slashes for pattern matching
        return path.Replace('\\', '/');
    }
}
