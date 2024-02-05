using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RemoteForge;

namespace TestForge;

public sealed class PwshTransport : ProcessTransport
{
    private readonly bool _failOnClose;
    private readonly bool _failOnCreate;
    private readonly bool _failOnRead;
    private readonly bool _endOnRead;
    private readonly bool _failOnWrite;
    private readonly bool _logMessages;
    private readonly bool _hang;

    public PwshTransport() : base("pwsh", new string[] { "-NoLogo", "-ServerMode" })
    { }

    public PwshTransport(Dictionary<string, string> environment)
        : base("pwsh", new string[] { "-NoLogo", "-ServerMode" }, environment)
    { }

    public PwshTransport(string exe, IEnumerable<string> arguments)
        : base(exe, arguments)
    { }

    public PwshTransport(
        bool failOnClose,
        bool failOnCreate,
        bool failOnRead,
        bool endOnRead,
        bool failOnWrite,
        bool logMessages,
        bool hang) : this()
    {
        _failOnClose = failOnClose;
        _failOnCreate = failOnCreate;
        _failOnRead = failOnRead;
        _endOnRead = endOnRead;
        _failOnWrite = failOnWrite;
        _logMessages = logMessages;
        _hang = hang;
    }

    protected override async Task Open(CancellationToken cancellationToken)
    {
        if (_failOnCreate)
        {
            throw new Exception("Failed to create connection");
        }

        await base.Open(cancellationToken);
    }

    protected override async Task Close(CancellationToken cancellationToken)
    {
        await base.Close(cancellationToken);

        if (_failOnClose)
        {
            throw new Exception("Failed to close connection");
        }
    }

    protected override async Task WriteInput(string line, CancellationToken cancellationToken)
    {
        if (_logMessages)
        {
            Console.WriteLine($"Writing: {line}");
        }

        if (_failOnWrite)
        {
            throw new Exception("Failed to write message");
        }

        await base.WriteInput(line, cancellationToken);
    }

    protected override async Task<string?> ReadError(CancellationToken cancellationToken)
    {
        return await base.ReadError(cancellationToken);
    }

    protected override async Task<string?> ReadOutput(CancellationToken cancellationToken)
    {
        if (_failOnRead)
        {
            throw new Exception("Failed to read message");
        }
        else if (_endOnRead)
        {
            return null;
        }
        else if (_hang)
        {
            await Task.Delay(-1, cancellationToken);
        }

        string? line = await base.ReadOutput(cancellationToken);
        if (_logMessages && !string.IsNullOrWhiteSpace(line))
        {
            Console.WriteLine($"Reading: {line}");
        }

        return line;
    }
}
