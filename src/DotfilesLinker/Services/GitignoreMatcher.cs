using DotfilesLinker.Utilities;

namespace DotfilesLinker.Services;

/// <summary>
/// Provides functionality to match paths using gitignore-style patterns.
/// </summary>
public static class GitignoreMatcher
{
    /// <summary>
    /// Represents a parsed .gitignore pattern
    /// </summary>
    private readonly struct GitIgnorePattern
    {
        /// <summary>
        /// Whether the pattern is a negation pattern (starts with !).
        /// </summary>
        public readonly bool Negation;

        /// <summary>
        /// Whether the pattern is directory-only (ends with /).
        /// </summary>
        public readonly bool DirOnly;

        /// <summary>
        /// Path segments split by '/' (may include "**").
        /// </summary>
        public readonly string[] Segments;

        public GitIgnorePattern(string pattern)
        {
            // Check for negation
            if (pattern.StartsWith("!"))
            {
                Negation = true;
                pattern = pattern.Substring(1).Trim();
            }
            else
            {
                Negation = false;
            }

            // Check for directory-only pattern
            if (pattern.EndsWith("/"))
            {
                DirOnly = true;
                pattern = pattern.Substring(0, pattern.Length - 1);
            }
            else
            {
                DirOnly = false;
            }

            // Trim leading slash if present
            if (pattern.StartsWith("/"))
            {
                pattern = pattern.Substring(1);
            }

            Segments = pattern.Split('/');
        }
    }

    /// <summary>
    /// Checks if a path matches a .gitignore style pattern.
    /// </summary>
    /// <param name="path">The path to check (using forward slashes).</param>
    /// <param name="pattern">The .gitignore style pattern.</param>
    /// <param name="isDir">Whether the path represents a directory.</param>
    /// <returns>True if the path matches the pattern; otherwise, false.</returns>
    public static bool IsMatch(string path, string pattern, bool isDir)
    {
        // Parse the pattern
        var pat = new GitIgnorePattern(pattern);

        // Skip directory-only patterns if the path is not a directory
        if (pat.DirOnly && !isDir)
        {
            return false;
        }

        // Ensure path uses forward slashes
        path = PathUtilities.NormalizePathForPatternMatching(path);

        // Remove any leading / for consistency
        if (path.StartsWith("/"))
        {
            path = path.Substring(1);
        }

        // Split path into segments
        var pathSegs = path.Split('/');

        // Match the segments
        return MatchSegments(pat.Segments, pathSegs);
    }

    /// <summary>
    /// Checks if path segments match pattern segments.
    /// </summary>
    /// <param name="segments">The pattern segments.</param>
    /// <param name="pathSegs">The path segments.</param>
    /// <returns>True if the path segments match the pattern segments; otherwise, false.</returns>
    private static bool MatchSegments(string[] segments, string[] pathSegs)
    {
        return MatchHelper(segments, pathSegs, 0, 0);
    }

    /// <summary>
    /// Recursive helper for matchSegments.
    /// </summary>
    /// <param name="segments">The pattern segments.</param>
    /// <param name="pathSegs">The path segments.</param>
    /// <param name="i">Current index in segments.</param>
    /// <param name="j">Current index in pathSegs.</param>
    /// <returns>True if remaining segments match remaining path segments; otherwise, false.</returns>
    private static bool MatchHelper(string[] segments, string[] pathSegs, int i, int j)
    {
        int nSeg = segments.Length;
        int nPath = pathSegs.Length;

        while (i < nSeg && j < nPath)
        {
            string seg = segments[i];

            if (seg == "**")
            {
                // "**" can match zero or more segments
                // If "**" is the last segment, match everything
                if (i + 1 == nSeg)
                {
                    return true;
                }

                // Try to match the rest of the pattern at different positions
                for (int k = j; k <= nPath; k++)
                {
                    if (MatchHelper(segments, pathSegs, i + 1, k))
                    {
                        return true;
                    }
                }
                return false;
            }

            // For non-"**" segments, match just one segment
            if (!MatchSingleSegment(seg, pathSegs[j]))
            {
                return false;
            }

            i++;
            j++;
        }

        // After the loop: check if we've used all segments
        if (i == nSeg && j == nPath)
        {
            return true;
        }

        // If we've consumed all path segments but still have pattern segments,
        // those remaining segments must all be "**"
        if (i < nSeg && j == nPath)
        {
            for (int k = i; k < nSeg; k++)
            {
                if (segments[k] != "**")
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a single path segment matches a pattern segment.
    /// </summary>
    /// <param name="segment">The pattern segment.</param>
    /// <param name="name">The path segment.</param>
    /// <returns>True if the path segment matches the pattern segment; otherwise, false.</returns>
    private static bool MatchSingleSegment(string segment, string name)
    {
        // Edge cases
        if (string.IsNullOrEmpty(segment))
        {
            return string.IsNullOrEmpty(name);
        }

        if (segment == "*")
        {
            return true;
        }

        // For more complex patterns with * and ? wildcards
        return WildcardMatcher.IsMatch(name, segment);
    }
}
