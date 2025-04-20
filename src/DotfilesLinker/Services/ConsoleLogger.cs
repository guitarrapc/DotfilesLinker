namespace DotfilesLinker.Services;

public interface ILogger
{
    void Success(string message);
    void Error(string message);
    void Info(string message);
    void Verbose(string message);
}

internal class NullLogger : ILogger
{
    public void Success(string message) { }
    public void Error(string message) { }
    public void Info(string message) { }
    public void Verbose(string message) { }
}

internal class ConsoleLogger(bool verbose) : ILogger
{
    public void Success(string message)
    {
        WriteSuccess(message);
    }

    public void Error(string message)
    {
        WriteError(message);
    }

    public void Info(string message)
    {
        if (verbose)
        {
            WriteInfo(message);
        }
    }

    public void Verbose(string message)
    {
        if (verbose)
        {
            WriteVerbose(message);
        }
    }

    static void WriteSuccess(string msg) => WriteColored("[o] ", msg, ConsoleColor.Green);
    static void WriteError(string msg) => WriteColored("[x] ", msg, ConsoleColor.Red);
    static void WriteInfo(string msg) => WriteColored("[i] ", msg, ConsoleColor.Cyan);
    static void WriteVerbose(string msg) => WriteColored("[v] ", msg, ConsoleColor.Yellow);

    static void WriteColored(string prefix, string msg, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"{prefix}{msg}");
        Console.ForegroundColor = prev;
    }
}
