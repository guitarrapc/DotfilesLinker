namespace DotfilesLinker.Services;

/// <summary>
/// Matches a single path segment using gitignore-style wildcards.
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Matches <paramref name="text"/> against a pattern containing <c>*</c>, <c>?</c>,
    /// character ranges such as <c>[a-z]</c>, and backslash escapes.
    /// </summary>
    public static bool IsMatch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        return IsMatch(text.AsSpan(), pattern.AsSpan());
    }

    /// <summary>
    /// Matches spans without allocating temporary strings or match tables.
    /// </summary>
    public static bool IsMatch(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
    {

        var textIndex = 0;
        var patternIndex = 0;
        var starPatternIndex = -1;
        var starTextIndex = -1;

        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                do
                {
                    patternIndex++;
                }
                while (patternIndex < pattern.Length && pattern[patternIndex] == '*');

                starPatternIndex = patternIndex;
                starTextIndex = textIndex;
                continue;
            }

            if (patternIndex < pattern.Length &&
                TryMatchToken(pattern, patternIndex, text[textIndex], out var nextPatternIndex))
            {
                patternIndex = nextPatternIndex;
                textIndex++;
                continue;
            }

            if (starPatternIndex < 0 || ++starTextIndex > text.Length)
            {
                return false;
            }

            textIndex = starTextIndex;
            patternIndex = starPatternIndex;
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static bool TryMatchToken(ReadOnlySpan<char> pattern, int index, char value, out int nextIndex)
    {
        var token = pattern[index];
        if (token == '?')
        {
            nextIndex = index + 1;
            return true;
        }

        if (token == '\\' && index + 1 < pattern.Length)
        {
            nextIndex = index + 2;
            return CharsEqual(pattern[index + 1], value);
        }

        if (token == '[' && TryMatchCharacterClass(pattern, index, value, out nextIndex, out var matches))
        {
            return matches;
        }

        nextIndex = index + 1;
        return CharsEqual(token, value);
    }

    private static bool TryMatchCharacterClass(
        ReadOnlySpan<char> pattern,
        int startIndex,
        char value,
        out int nextIndex,
        out bool matches)
    {
        var index = startIndex + 1;
        var negated = index < pattern.Length && (pattern[index] == '!' || pattern[index] == '^');
        if (negated)
        {
            index++;
        }

        var classStart = index;
        var found = false;
        while (index < pattern.Length && pattern[index] != ']')
        {
            var lower = pattern[index];
            if (lower == '\\' && index + 1 < pattern.Length)
            {
                lower = pattern[++index];
            }

            if (index + 2 < pattern.Length && pattern[index + 1] == '-' && pattern[index + 2] != ']')
            {
                var upper = pattern[index + 2];
                found |= IsInRange(value, lower, upper);
                index += 3;
            }
            else
            {
                found |= CharsEqual(lower, value);
                index++;
            }
        }

        if (index >= pattern.Length || index == classStart)
        {
            nextIndex = startIndex + 1;
            matches = false;
            return false;
        }

        nextIndex = index + 1;
        matches = negated ? !found : found;
        return true;
    }

    private static bool IsInRange(char value, char lower, char upper)
    {
        value = char.ToUpperInvariant(value);
        lower = char.ToUpperInvariant(lower);
        upper = char.ToUpperInvariant(upper);
        return value >= lower && value <= upper;
    }

    private static bool CharsEqual(char left, char right) =>
        char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
}
