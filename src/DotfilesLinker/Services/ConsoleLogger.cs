namespace DotfilesLinker.Services;

internal interface ILogger
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

internal class ConsoleLogger(
    bool verbose,
    TextWriter? output = null,
    TextWriter? error = null) : ILogger
{
    private readonly TextWriter _output = output ?? Console.Out;
    private readonly TextWriter _error = error ?? Console.Error;

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

    void WriteSuccess(string msg) => WriteColored(_output, "[o] ", msg, ConsoleColor.Green);
    void WriteError(string msg) => WriteColored(_error, "[x] ", msg, ConsoleColor.Red);
    void WriteInfo(string msg) => WriteColored(_output, "[i] ", msg, ConsoleColor.Cyan);
    void WriteVerbose(string msg) => WriteColored(_output, "[v] ", msg, ConsoleColor.Yellow);

    static void WriteColored(TextWriter writer, string prefix, string msg, ConsoleColor color)
    {
        if (!ReferenceEquals(writer, Console.Out) && !ReferenceEquals(writer, Console.Error))
        {
            writer.WriteLine($"{prefix}{msg}");
            return;
        }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            writer.WriteLine($"{prefix}{msg}");
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }
}
