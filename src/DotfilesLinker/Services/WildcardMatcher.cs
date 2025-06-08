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
    /// Dynamic programming based pattern matching that handles wildcards.
    /// </summary>
    /// <param name="text">The text to match.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <param name="textIndex">Current position in the text.</param>
    /// <param name="patternIndex">Current position in the pattern.</param>
    /// <returns>True if the remaining text matches the remaining pattern; otherwise, false.</returns>
    private static bool MatchPattern(string text, string pattern, int textIndex, int patternIndex)
    {
        // For '*': since '*' can match zero or more characters, `dp[i][j] = dp[i-1][j] || dp[i][j-1]`
        // For '?': since '?' matches any single character, `dp[i][j] = dp[i-1][j-1]`
        // For a regular character: the characters must match and the preceding pattern must also match, so `dp[i][j] = dp[i-1][j-1] && text[i-1] == pattern[j-1]`

        int textLength = text.Length;
        int patternLength = pattern.Length;

        // Create a DP table
        bool[,] dp = new bool[textLength + 1, patternLength + 1];
        dp[0, 0] = true; // Empty text matches empty pattern

        // Handle patterns with leading '*'
        for (int j = 1; j <= patternLength; j++)
        {
            if (pattern[j - 1] == '*')
            {
                dp[0, j] = dp[0, j - 1];
            }
        }

        // Fill the DP table
        for (int i = 1; i <= textLength; i++)
        {
            for (int j = 1; j <= patternLength; j++)
            {
                if (pattern[j - 1] == '*')
                {
                    dp[i, j] = dp[i - 1, j] || dp[i, j - 1];
                }
                else if (pattern[j - 1] == '?')
                {
                    dp[i, j] = dp[i - 1, j - 1];
                }
                else
                {
                    dp[i, j] = dp[i - 1, j - 1] && text[i - 1] == pattern[j - 1];
                }
            }
        }

        return dp[textLength, patternLength];
    }
}
