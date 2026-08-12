namespace DotfilesLinker.Tests;

public class WildcardMatcherTests
{
    [Fact]
    public void IsMatch_SimpleMatchWithoutWildcards_ReturnsTrue()
    {
        // Arrange
        string text = "file.txt";
        string pattern = "file.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_SimpleMismatchWithoutWildcards_ReturnsFalse()
    {
        // Arrange
        string text = "file.txt";
        string pattern = "other.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_PrefixWildcard_ReturnsTrue()
    {
        // Arrange
        string text = "test.backup";
        string pattern = "*.backup";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_SuffixWildcard_ReturnsTrue()
    {
        // Arrange
        string text = "tempfile.txt";
        string pattern = "temp*";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_MiddleWildcard_ReturnsTrue()
    {
        // Arrange
        string text = "before_after.txt";
        string pattern = "before*after.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_TwoWildcards_ReturnsTrue()
    {
        // Arrange
        string text = "abcdefg";
        string pattern = "a*c*g";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ThreeWildcards_ReturnsTrue()
    {
        // Arrange
        string text = "start_middle_end.txt";
        string pattern = "start*mid*le*end.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_MultipleWildcards_ReturnsFalseWhenMismatch()
    {
        // Arrange
        string text = "start_wrong_end.txt";
        string pattern = "start*middle*end.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_OnlyWildcard_ReturnsTrue()
    {
        // Arrange
        string text = "anything.txt";
        string pattern = "*";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_EmptyPattern_ReturnsFalseForNonEmptyText()
    {
        // Arrange
        string text = "file.txt";
        string pattern = "";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_EmptyText_ReturnsFalseForNonEmptyPattern()
    {
        // Arrange
        string text = "";
        string pattern = "file*";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_CaseInsensitivity_ReturnsTrue()
    {
        // Arrange
        string text = "AbCdEf";
        string pattern = "abc*ef";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_QuestionMarkWildcard_ReturnsTrue()
    {
        // Arrange
        string text = "file1.txt";
        string pattern = "file?.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_QuestionMarkWildcard_ReturnsFalseForTwoCharacters()
    {
        // Arrange
        string text = "file12.txt";
        string pattern = "file?.txt";

        // Act
        bool result = Services.WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("file[0-9].txt", "file7.txt", true)]
    [InlineData("file[0-9].txt", "filex.txt", false)]
    [InlineData("file[!0-9].txt", "filex.txt", true)]
    [InlineData("file[!0-9].txt", "file7.txt", false)]
    [InlineData(@"file\?.txt", "file?.txt", true)]
    [InlineData(@"file\*.txt", "file*.txt", true)]
    public void IsMatch_GitignoreCharacterClassesAndEscapes(
        string pattern,
        string text,
        bool expected)
    {
        Assert.Equal(expected, Services.WildcardMatcher.IsMatch(text, pattern));
    }

    [Fact]
    public void IsMatch_AvoidsPerCallHeapAllocation()
    {
        const string text = "application-2026-08-12.log";
        const string pattern = "app*[0-9]-??-??.log";

        for (var i = 0; i < 100; i++)
        {
            Services.WildcardMatcher.IsMatch(text, pattern);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var matchCount = 0;
        for (var i = 0; i < 10_000; i++)
        {
            if (Services.WildcardMatcher.IsMatch(text, pattern))
            {
                matchCount++;
            }
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(10_000, matchCount);
        Assert.InRange(allocated, 0, 64 * 1024);
    }
}
