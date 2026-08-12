using System.Buffers;

namespace DotfilesLinker.Services;

/// <summary>
/// Evaluates an ordered set of gitignore-style rules against repository-relative paths.
/// </summary>
internal sealed class GitignoreMatcher
{
    private const int MaxStackAllocatedSegments = 64;
    private readonly Rule[] _rules;

    /// <summary>
    /// Creates a matcher from ignore-file lines. Empty lines and comments are discarded.
    /// </summary>
    public GitignoreMatcher(IEnumerable<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        _rules = patterns
            .Select(Rule.TryParse)
            .Where(static rule => rule is not null)
            .Cast<Rule>()
            .ToArray();
    }

    /// <summary>
    /// Gets the number of active rules.
    /// </summary>
    public int Count => _rules.Length;

    /// <summary>
    /// Determines whether a repository-relative path is ignored. When several rules match,
    /// the last matching rule wins, as it does in a .gitignore file.
    /// </summary>
    public bool IsIgnored(string path, bool isDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(path);

        return IsIgnoredCore(path, isDirectory);
    }

    private bool IsIgnoredCore(string path, bool isDirectory)
    {
        var segmentCount = CountPathSegments(path);
        PathSegment[]? rentedSegments = null;
        Span<PathSegment> pathSegments = segmentCount <= MaxStackAllocatedSegments
            ? stackalloc PathSegment[segmentCount]
            : (rentedSegments = ArrayPool<PathSegment>.Shared.Rent(segmentCount));
        pathSegments = pathSegments[..segmentCount];
        FillPathSegments(path, pathSegments);

        try
        {
            for (var pathLength = 1; pathLength <= pathSegments.Length; pathLength++)
            {
                var candidateIsDirectory = pathLength < pathSegments.Length || isDirectory;
                var ignored = false;

                foreach (var rule in _rules)
                {
                    if (rule.IsMatch(path, pathSegments, pathLength, candidateIsDirectory))
                    {
                        ignored = !rule.Negated;
                    }
                }

                // Git cannot re-include a file while one of its parent directories remains excluded.
                if (ignored)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (rentedSegments is not null)
            {
                ArrayPool<PathSegment>.Shared.Return(rentedSegments);
            }
        }
    }

    /// <summary>
    /// Checks a path against one gitignore-style pattern.
    /// </summary>
    public static bool IsMatch(string path, string pattern, bool isDirectory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(pattern);

        var rule = Rule.TryParse(pattern);
        if (rule is null)
        {
            return false;
        }

        return IsRuleMatch(path, isDirectory, rule);
    }

    private static bool IsRuleMatch(string path, bool isDirectory, Rule rule)
    {
        var segmentCount = CountPathSegments(path);
        PathSegment[]? rentedSegments = null;
        Span<PathSegment> pathSegments = segmentCount <= MaxStackAllocatedSegments
            ? stackalloc PathSegment[segmentCount]
            : (rentedSegments = ArrayPool<PathSegment>.Shared.Rent(segmentCount));
        pathSegments = pathSegments[..segmentCount];
        FillPathSegments(path, pathSegments);

        try
        {
            for (var pathLength = 1; pathLength <= pathSegments.Length; pathLength++)
            {
                var candidateIsDirectory = pathLength < pathSegments.Length || isDirectory;
                if (rule.IsMatch(path, pathSegments, pathLength, candidateIsDirectory))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (rentedSegments is not null)
            {
                ArrayPool<PathSegment>.Shared.Return(rentedSegments);
            }
        }
    }

    private static int CountPathSegments(ReadOnlySpan<char> path)
    {
        var count = 0;
        var insideSegment = false;
        foreach (var character in path)
        {
            if (IsDirectorySeparator(character))
            {
                insideSegment = false;
            }
            else if (!insideSegment)
            {
                insideSegment = true;
                count++;
            }
        }

        return count;
    }

    private static void FillPathSegments(ReadOnlySpan<char> path, Span<PathSegment> segments)
    {
        var segmentIndex = 0;
        var segmentStart = -1;

        for (var index = 0; index <= path.Length; index++)
        {
            var atEnd = index == path.Length;
            if (!atEnd && !IsDirectorySeparator(path[index]))
            {
                if (segmentStart < 0)
                {
                    segmentStart = index;
                }

                continue;
            }

            if (segmentStart >= 0)
            {
                segments[segmentIndex++] = new PathSegment(segmentStart, index - segmentStart);
                segmentStart = -1;
            }
        }
    }

    private static bool IsDirectorySeparator(char character) => character is '/' or '\\';

    private readonly record struct PathSegment(int Start, int Length);

    private sealed class Rule
    {
        private readonly bool _anchored;
        private readonly bool _directoryOnly;
        private readonly bool _hasSlash;
        private readonly bool _descendantsOnly;
        private readonly string[] _segments;

        private Rule(string pattern, bool negated, bool anchored, bool directoryOnly)
        {
            Negated = negated;
            _anchored = anchored;
            _directoryOnly = directoryOnly;
            _hasSlash = pattern.Contains('/');
            _descendantsOnly = pattern.EndsWith("/**", StringComparison.Ordinal);
            _segments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Negated { get; }

        public static Rule? TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var pattern = TrimUnescapedTrailingSpaces(line);
            if (pattern.Length == 0 || pattern[0] == '#')
            {
                return null;
            }

            var escapedPrefix = pattern.StartsWith("\\#", StringComparison.Ordinal) ||
                pattern.StartsWith("\\!", StringComparison.Ordinal);
            var negated = !escapedPrefix && pattern[0] == '!';
            if (negated)
            {
                pattern = pattern[1..];
            }
            else if (escapedPrefix)
            {
                pattern = pattern[1..];
            }

            if (pattern.Length == 0)
            {
                return null;
            }

            var anchored = pattern.StartsWith('/');
            var directoryOnly = pattern.EndsWith('/');
            pattern = pattern.Trim('/');

            return pattern.Length == 0
                ? null
                : new Rule(pattern, negated, anchored, directoryOnly);
        }

        public bool IsMatch(
            string path,
            ReadOnlySpan<PathSegment> pathSegments,
            int pathLength,
            bool isDirectory)
        {
            if (pathLength == 0)
            {
                return false;
            }

            if (_descendantsOnly && pathLength < _segments.Length)
            {
                return false;
            }

            if (!_hasSlash && !_anchored)
            {
                return (!_directoryOnly || isDirectory) &&
                    MatchPathSegment(path, pathSegments[pathLength - 1], _segments[0]);
            }

            return (!_directoryOnly || isDirectory) &&
                MatchSegments(_segments, path, pathSegments, 0, 0, pathLength);
        }

        private static bool MatchSegments(
            string[] patternSegments,
            string path,
            ReadOnlySpan<PathSegment> pathSegments,
            int patternIndex,
            int pathIndex,
            int pathLength)
        {
            while (patternIndex < patternSegments.Length && pathIndex < pathLength)
            {
                var segment = patternSegments[patternIndex];
                if (segment == "**")
                {
                    if (patternIndex + 1 == patternSegments.Length)
                    {
                        return true;
                    }

                    for (var nextPathIndex = pathIndex; nextPathIndex <= pathLength; nextPathIndex++)
                    {
                        if (MatchSegments(
                            patternSegments,
                            path,
                            pathSegments,
                            patternIndex + 1,
                            nextPathIndex,
                            pathLength))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (!MatchPathSegment(path, pathSegments[pathIndex], segment))
                {
                    return false;
                }

                patternIndex++;
                pathIndex++;
            }

            while (patternIndex < patternSegments.Length && patternSegments[patternIndex] == "**")
            {
                patternIndex++;
            }

            return patternIndex == patternSegments.Length && pathIndex == pathLength;
        }

        private static bool MatchPathSegment(string path, PathSegment pathSegment, string pattern) =>
            WildcardMatcher.IsMatch(
                path.AsSpan(pathSegment.Start, pathSegment.Length),
                pattern.AsSpan());

        private static string TrimUnescapedTrailingSpaces(string pattern)
        {
            var end = pattern.Length;
            while (end > 0 && pattern[end - 1] == ' ')
            {
                var slashCount = 0;
                for (var i = end - 2; i >= 0 && pattern[i] == '\\'; i--)
                {
                    slashCount++;
                }

                if ((slashCount & 1) != 0)
                {
                    return pattern.Remove(end - 2, 1);
                }

                end--;
            }

            return end == pattern.Length ? pattern : pattern[..end];
        }
    }
}
