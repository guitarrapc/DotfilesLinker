using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotfilesLinker.Services;

internal enum LogLevel
{
    Success,
    Error,
    Info,
    Verbose,
    Summary
}

internal interface ILogger
{
    bool IsEnabled(LogLevel level);
    IBufferWriter<byte> BeginWrite(LogLevel level);
    void EndWrite(LogLevel level);
}

internal static class LoggerExtensions
{
    public static void Log(this ILogger logger, LogLevel level, ReadOnlySpan<byte> message)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        var writer = logger.BeginWrite(level);
        writer.Write(message);
        logger.EndWrite(level);
    }

    public static void Log(
        this ILogger logger,
        LogLevel level,
        [InterpolatedStringHandlerArgument("logger", "level")] ref Utf8LogInterpolatedStringHandler handler) =>
        handler.Complete();
}

[InterpolatedStringHandler]
internal ref struct Utf8LogInterpolatedStringHandler
{
    private readonly ILogger? _logger;
    private readonly LogLevel _level;
    private readonly IBufferWriter<byte>? _writer;

    public Utf8LogInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        ILogger logger,
        LogLevel level,
        out bool shouldAppend)
    {
        _ = literalLength;
        _ = formattedCount;
        shouldAppend = logger.IsEnabled(level);
        _logger = shouldAppend ? logger : null;
        _level = level;
        _writer = shouldAppend ? logger.BeginWrite(level) : null;
    }

    public void AppendLiteral(string value) => WriteUtf8(value);

    public void AppendFormatted(string? value)
    {
        if (value is not null)
        {
            WriteUtf8(value);
        }
    }

    public void AppendFormatted(int value)
    {
        if (_writer is null)
        {
            return;
        }

        var destination = _writer.GetSpan(11);
        _ = Utf8Formatter.TryFormat(value, destination, out var bytesWritten);
        _writer.Advance(bytesWritten);
    }

    public void AppendFormatted(bool value) =>
        _writer?.Write(value ? "True"u8 : "False"u8);

    public void Complete() => _logger?.EndWrite(_level);

    private void WriteUtf8(ReadOnlySpan<char> value)
    {
        if (_writer is null || value.IsEmpty)
        {
            return;
        }

        var destination = _writer.GetSpan(Encoding.UTF8.GetMaxByteCount(value.Length));
        var bytesWritten = Encoding.UTF8.GetBytes(value, destination);
        _writer.Advance(bytesWritten);
    }
}

internal sealed class NullLogger : ILogger
{
    public bool IsEnabled(LogLevel level) => false;
    public IBufferWriter<byte> BeginWrite(LogLevel level) => NullBufferWriter.Instance;
    public void EndWrite(LogLevel level) { }

    private sealed class NullBufferWriter : IBufferWriter<byte>
    {
        public static readonly NullBufferWriter Instance = new();
        public void Advance(int count) { }
        public Memory<byte> GetMemory(int sizeHint = 0) => Memory<byte>.Empty;
        public Span<byte> GetSpan(int sizeHint = 0) => Span<byte>.Empty;
    }
}

internal sealed class ConsoleLogger : ILogger, IDisposable
{
    private readonly bool _verbose;
    private readonly ArrayBufferWriter<byte> _buffer = new(256);
    private readonly Stream? _outputStream;
    private readonly Stream? _errorStream;
    private readonly IBufferWriter<byte>? _outputBuffer;
    private readonly IBufferWriter<byte>? _errorBuffer;
    private readonly bool _useColors;
    private ConsoleColor _previousColor;

    public ConsoleLogger(bool verbose)
    {
        _verbose = verbose;
        _outputStream = Console.OpenStandardOutput();
        _errorStream = Console.OpenStandardError();
        _useColors = !Console.IsOutputRedirected;
    }

    internal ConsoleLogger(
        bool verbose,
        IBufferWriter<byte> output,
        IBufferWriter<byte> error)
    {
        _verbose = verbose;
        _outputBuffer = output;
        _errorBuffer = error;
    }

    public bool IsEnabled(LogLevel level) =>
        level is not LogLevel.Info and not LogLevel.Verbose || _verbose;

    public IBufferWriter<byte> BeginWrite(LogLevel level)
    {
        _buffer.Clear();
        if (_useColors && level != LogLevel.Summary)
        {
            _previousColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(level);
        }

        _buffer.Write(GetPrefix(level));
        return _buffer;
    }

    public void EndWrite(LogLevel level)
    {
        _buffer.Write(OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8);

        if (level == LogLevel.Error)
        {
            WriteCompletedMessage(_errorStream, _errorBuffer);
        }
        else
        {
            WriteCompletedMessage(_outputStream, _outputBuffer);
        }

        if (_useColors && level != LogLevel.Summary)
        {
            Console.ForegroundColor = _previousColor;
        }
    }

    public void Dispose()
    {
        _outputStream?.Dispose();
        _errorStream?.Dispose();
    }

    private void WriteCompletedMessage(Stream? stream, IBufferWriter<byte>? buffer)
    {
        if (stream is not null)
        {
            stream.Write(_buffer.WrittenSpan);
            return;
        }

        buffer!.Write(_buffer.WrittenSpan);
    }

    private static ReadOnlySpan<byte> GetPrefix(LogLevel level) => level switch
    {
        LogLevel.Success => "[o] "u8,
        LogLevel.Error => "[x] "u8,
        LogLevel.Info => "[i] "u8,
        LogLevel.Verbose => "[v] "u8,
        _ => ReadOnlySpan<byte>.Empty
    };

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Success => ConsoleColor.Green,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Info => ConsoleColor.Cyan,
        LogLevel.Verbose => ConsoleColor.Yellow,
        _ => Console.ForegroundColor
    };
}

internal static class BufferWriterExtensions
{
    public static void Write(this IBufferWriter<byte> writer, ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        var destination = writer.GetSpan(value.Length);
        value.CopyTo(destination);
        writer.Advance(value.Length);
    }
}
