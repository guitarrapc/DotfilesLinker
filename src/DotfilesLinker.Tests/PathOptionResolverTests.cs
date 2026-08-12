using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

public class PathOptionResolverTests
{
    [Fact]
    public void Resolve_CommandLineOptionTakesPrecedence()
    {
        var result = PathOptionResolver.Resolve(
            Path.Combine("option", "dotfiles"),
            Path.Combine("environment", "dotfiles"),
            Path.Combine("default", "dotfiles"));

        Assert.Equal(Path.GetFullPath(Path.Combine("option", "dotfiles")), result);
    }

    [Fact]
    public void Resolve_EnvironmentVariableTakesPrecedenceOverDefault()
    {
        var result = PathOptionResolver.Resolve(
            optionValue: null,
            Path.Combine("environment", "dotfiles"),
            Path.Combine("default", "dotfiles"));

        Assert.Equal(Path.GetFullPath(Path.Combine("environment", "dotfiles")), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_UsesDefaultWhenOtherValuesAreEmpty(string? environmentValue)
    {
        var defaultValue = Path.Combine("default", "dotfiles");

        var result = PathOptionResolver.Resolve(optionValue: null, environmentValue, defaultValue);

        Assert.Equal(Path.GetFullPath(defaultValue), result);
    }
}
