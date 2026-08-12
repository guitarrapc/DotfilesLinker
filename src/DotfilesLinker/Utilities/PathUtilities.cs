using System.Runtime.CompilerServices;

namespace DotfilesLinker.Utilities;

/// <summary>
/// Provides utility methods for working with file and directory paths.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Compares two paths after converting them to absolute paths.
    /// </summary>
    public static bool PathEquals(string first, string second)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
        {
            return false;
        }

        return PathsEqual(
            Path.GetFullPath(first),
            Path.GetFullPath(second));
    }

    /// <summary>
    /// Determines whether a symbolic link target resolves to an expected path.
    /// </summary>
    /// <param name="linkPath">The path of the symbolic link.</param>
    /// <param name="linkTarget">The target stored in the symbolic link.</param>
    /// <param name="expectedTarget">The expected target path.</param>
    /// <returns>
    /// <c>true</c> if the resolved link target and expected target are equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A relative <paramref name="linkTarget"/> is resolved from the parent directory of
    /// <paramref name="linkPath"/>. Path comparison is case-insensitive on Windows and
    /// case-sensitive on other platforms.
    /// </remarks>
    public static bool LinkTargetEquals(string linkPath, string linkTarget, string expectedTarget)
    {
        if (string.IsNullOrEmpty(linkPath) ||
            string.IsNullOrEmpty(linkTarget) ||
            string.IsNullOrEmpty(expectedTarget))
        {
            return false;
        }

        var fullLinkPath = Path.GetFullPath(linkPath);
        var linkDirectory = Path.GetDirectoryName(fullLinkPath)!;
        var resolvedLinkTarget = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(linkTarget, linkDirectory));
        var fullExpectedTarget = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(expectedTarget));
        return PathsEqual(resolvedLinkTarget, fullExpectedTarget);
    }

    private static bool PathsEqual(string first, string second) =>
        string.Equals(
            first,
            second,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);

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
