namespace DotfilesLinker.Tests;

public class GitignoreMatcherTests
{
    [Fact]
    public void IsIgnored_LastMatchingRuleWins()
    {
        var matcher = new Services.GitignoreMatcher(["*.log", "!important.log", "*.log"]);

        Assert.True(matcher.IsIgnored("important.log"));
    }

    [Fact]
    public void IsIgnored_NegationReincludesFile()
    {
        var matcher = new Services.GitignoreMatcher(["*.log", "!important.log"]);

        Assert.False(matcher.IsIgnored("logs/important.log"));
        Assert.True(matcher.IsIgnored("logs/application.log"));
    }

    [Fact]
    public void IsIgnored_CannotReincludeFileBelowIgnoredParent()
    {
        var matcher = new Services.GitignoreMatcher(["docs/", "!docs/README.md"]);

        Assert.True(matcher.IsIgnored("docs/README.md"));
    }

    [Fact]
    public void IsIgnored_CanReincludeFileAfterReincludingParent()
    {
        var matcher = new Services.GitignoreMatcher(["docs/", "!docs/", "docs/*", "!docs/README.md"]);

        Assert.False(matcher.IsIgnored("docs/README.md"));
        Assert.True(matcher.IsIgnored("docs/other.md"));
    }

    [Fact]
    public void IsIgnored_DirectoryRuleIgnoresDescendants()
    {
        var matcher = new Services.GitignoreMatcher(["cache/"]);

        Assert.True(matcher.IsIgnored("HOME/cache/data.json"));
        Assert.False(matcher.IsIgnored("HOME/cache.json"));
    }

    [Fact]
    public void IsIgnored_LeadingSlashAnchorsRuleToRepositoryRoot()
    {
        var matcher = new Services.GitignoreMatcher(["/config.json"]);

        Assert.True(matcher.IsIgnored("config.json"));
        Assert.False(matcher.IsIgnored("HOME/config.json"));
    }

    [Fact]
    public void IsIgnored_SlashPatternIsRelativeToRepositoryRoot()
    {
        var matcher = new Services.GitignoreMatcher(["HOME/config/*.json"]);

        Assert.True(matcher.IsIgnored("HOME/config/app.json"));
        Assert.False(matcher.IsIgnored("config/app.json"));
    }

    [Fact]
    public void IsIgnored_CommentsAndEmptyLinesAreIgnored()
    {
        var matcher = new Services.GitignoreMatcher(["", "  ", "# comment", "*.tmp"]);

        Assert.Equal(1, matcher.Count);
        Assert.True(matcher.IsIgnored("work.tmp"));
        Assert.False(matcher.IsIgnored("# comment"));
    }

    [Fact]
    public void IsIgnored_EscapedCommentAndNegationPrefixesAreLiteral()
    {
        var matcher = new Services.GitignoreMatcher([@"\#notes", @"\!important"]);

        Assert.True(matcher.IsIgnored("#notes"));
        Assert.True(matcher.IsIgnored("!important"));
    }

    [Fact]
    public void IsIgnored_TrailingDoubleAsteriskMatchesContentsButNotDirectory()
    {
        var matcher = new Services.GitignoreMatcher(["logs/**"]);

        Assert.False(matcher.IsIgnored("logs", isDirectory: true));
        Assert.True(matcher.IsIgnored("logs/archive/app.log"));
    }

    [Fact]
    public void IsIgnored_UnescapedTrailingSpacesAreIgnored()
    {
        var matcher = new Services.GitignoreMatcher(["report.tmp   "]);

        Assert.True(matcher.IsIgnored("report.tmp"));
    }

    [Fact]
    public void IsIgnored_EscapedTrailingSpaceIsSignificant()
    {
        var matcher = new Services.GitignoreMatcher([@"report.tmp\ "]);

        Assert.True(matcher.IsIgnored("report.tmp "));
        Assert.False(matcher.IsIgnored("report.tmp"));
    }

    [Fact]
    public void IsMatch_ExactPathMatch_ReturnsTrue()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "test/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_SimpleWildcardInPath_ReturnsTrue()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "test/*.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectoryOnlyPattern_ReturnsTrueForDirectory()
    {
        // Arrange
        string path = "test/dir";
        string pattern = "test/dir/";
        bool isDir = true;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DirectoryOnlyPattern_ReturnsFalseForFile()
    {
        // Arrange
        string path = "test/dir";
        string pattern = "test/dir/";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_DirectoryOnlyPattern_ReturnsTrueForDescendant()
    {
        bool result = Services.GitignoreMatcher.IsMatch(
            "test/dir/file.txt",
            "test/dir/",
            isDirectory: false);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskWildcard_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "test/**/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskAtEnd_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "test/**";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskAtBeginning_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/file.txt";
        string pattern = "**/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMatch_NonMatchingPath_ReturnsFalse()
    {
        // Arrange
        string path = "test/file.txt";
        string pattern = "other/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_DoubleAsteriskWithOtherSegments_ReturnsTrue()
    {
        // Arrange
        string path = "test/subdir/subsub/file.txt";
        string pattern = "test/**/subsub/file.txt";
        bool isDir = false;

        // Act
        bool result = Services.GitignoreMatcher.IsMatch(path, pattern, isDir);

        // Assert
        Assert.True(result);
    }
}
