namespace DotfilesLinker.Services;

internal static class PathOptionResolver
{
    public static string Resolve(string? optionValue, string? environmentValue, string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);

        var value = !string.IsNullOrEmpty(optionValue)
            ? optionValue
            : !string.IsNullOrEmpty(environmentValue)
                ? environmentValue
                : defaultValue;

        return Path.GetFullPath(value);
    }
}
