namespace DotfilesLinker.Services;

/// <summary>
/// Provides advanced wildcard pattern matching functionality.
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Performs wildcard matching for file patterns supporting multiple asterisks (*) and question marks (?) in patterns.
    /// </summary>
    /// <param name="text">The text to match against the pattern.</param>
    /// <param name="pattern">The pattern to match.</param>
    /// <returns>True if the text matches the pattern; otherwise, false.</returns>
    public static bool IsMatch(string text, string pattern)
    {
        // Edge cases
        if (string.IsNullOrEmpty(pattern))
        {
            return string.IsNullOrEmpty(text);
        }

        if (pattern == "*")
        {
            return true;
        }

        // Case insensitive comparison
        return MatchPattern(text.ToLowerInvariant(), pattern.ToLowerInvariant(), 0, 0);
    }

    /// <summary>
    /// Recursive helper function for pattern matching.
    /// </summary>
    /// <param name="text">The text to match.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <param name="textIndex">Current position in the text.</param>
    /// <param name="patternIndex">Current position in the pattern.</param>
    /// <returns>True if the remaining text matches the remaining pattern; otherwise, false.</returns>
    private static bool MatchPattern(string text, string pattern, int textIndex, int patternIndex)
    {
        int textLength = text.Length;
        int patternLength = pattern.Length;

        // Base case: if we've reached the end of both strings, we have a match
        if (textIndex == textLength && patternIndex == patternLength)
        {
            return true;
        }

        // If we've reached the end of the pattern but not the text, no match
        if (patternIndex == patternLength)
        {
            return false;
        }

        // Handle current pattern character
        switch (pattern[patternIndex])
        {
            case '*':
                // Try to match zero or more characters
                // 1) Skip the asterisk and try to match the rest of the pattern with the current text position
                // 2) Match one character and try again with the same pattern
                return MatchPattern(text, pattern, textIndex, patternIndex + 1) ||
                       (textIndex < textLength && MatchPattern(text, pattern, textIndex + 1, patternIndex));

            case '?':
                // Match exactly one character
                return textIndex < textLength && MatchPattern(text, pattern, textIndex + 1, patternIndex + 1);

            default:
                // Match exact character
                return textIndex < textLength &&
                       pattern[patternIndex] == text[textIndex] &&
                       MatchPattern(text, pattern, textIndex + 1, patternIndex + 1);
        }
    }
}
