using System.Buffers;
using System.Text;
using DotfilesLinker.Services;

namespace DotfilesLinker.Tests;

internal sealed class TestLogger
{
    private readonly ArrayBufferWriter<byte> _output = new();
    private readonly ArrayBufferWriter<byte> _error = new();

    public TestLogger(bool verbose = true)
    {
        Logger = new ConsoleLogger(verbose, _output, _error);
    }

    public ILogger Logger { get; }
    public string Output => Encoding.UTF8.GetString(_output.WrittenSpan);
    public string Error => Encoding.UTF8.GetString(_error.WrittenSpan);
}
