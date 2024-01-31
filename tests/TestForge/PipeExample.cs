using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RemoteForge;

namespace TestForge;

public sealed class PipeInfo : IRemoteForge
{
    private readonly string _factoryUri;

    public static string ForgeName => "PipeTest";
    public static string ForgeDescription => "Test pipe transport";

    public bool FailOnClose { get; }
    public bool FailOnCreate { get; }
    public bool FailOnRead { get; }
    public bool EndOnRead { get; }
    public bool FailOnWrite { get; }
    public bool LogMessages { get; }
    public bool Hang { get; }

    private PipeInfo(
        string factoryUri,
        bool failOnClose,
        bool failOnCreate,
        bool failOnRead,
        bool endOnRead,
        bool failOnWrite,
        bool logMessages,
        bool hang
    )
    {
        FailOnClose = failOnClose;
        FailOnCreate = failOnCreate;
        FailOnRead = failOnRead;
        EndOnRead = endOnRead;
        FailOnWrite = failOnWrite;
        LogMessages = logMessages;
        Hang = hang;
        _factoryUri = factoryUri;
    }

    public IRemoteForgeTransport CreateTransport()
        => new PipeTransport(
            FailOnClose,
            FailOnCreate,
            FailOnRead,
            EndOnRead,
            FailOnWrite,
            LogMessages,
            Hang);

    public string GetTransportString() => _factoryUri;

    public static IRemoteForge Create(string info)
    {
        bool failOnClose, failOnCreate, failOnRead, endOnRead, failOnWrite, logMessages, hang;
        failOnClose = failOnCreate = failOnRead = endOnRead = failOnWrite = logMessages = hang = false;

        Uri pipeUri = new($"PipeTest://{info}");
        NameValueCollection infoQueries = HttpUtility.ParseQueryString(pipeUri.Query);
        for (int i = 0; i < infoQueries.Count; i++)
        {
            string? key = infoQueries.GetKey(i)?.ToLowerInvariant();
            string? value = infoQueries.Get(i);

            bool result;
            if (key == "logmessages" && bool.TryParse(value, out result))
            {
                logMessages = result;
            }
            else if (key == "failonclose" && bool.TryParse(value, out result))
            {
                failOnClose = result;
            }
            else if (key == "failoncreate" && bool.TryParse(value, out result))
            {
                failOnCreate = result;
            }
            else if (key == "failonread" && bool.TryParse(value, out result))
            {
                failOnRead = result;
            }
            else if (key == "endonread" && bool.TryParse(value, out result))
            {
                endOnRead = result;
            }
            else if (key == "failonwrite" && bool.TryParse(value, out result))
            {
                failOnWrite = result;
            }
            else if (key == "hang" && bool.TryParse(value, out result))
            {
                hang = result;
            }
        }

        return new PipeInfo(
            $"PipeTest:{info}",
            failOnClose,
            failOnCreate,
            failOnRead,
            endOnRead,
            failOnWrite,
            logMessages,
            hang);
    }
}

public sealed class PipeTransport : IRemoteForgeTransport, IDisposable
{
    private Process? _proc;
    private readonly bool _failOnClose;
    private readonly bool _failOnCreate;
    private readonly bool _failOnRead;
    private readonly bool _endOnRead;
    private readonly bool _failOnWrite;
    private readonly bool _logMessages;
    private readonly bool _hang;


    internal PipeTransport(
        bool failOnClose,
        bool failOnCreate,
        bool failOnRead,
        bool endOfRead,
        bool failOnWrite,
        bool logMessages,
        bool hang)
    {
        _failOnClose = failOnClose;
        _failOnCreate = failOnCreate;
        _failOnRead = failOnRead;
        _endOnRead = endOfRead;
        _failOnWrite = failOnWrite;
        _logMessages = logMessages;
        _hang = hang;
    }

    public Task CreateConnection(CancellationToken cancellationToken)
    {
        if (_failOnCreate)
        {
            throw new Exception("Failed to create connection");
        }

        _proc = new()
        {
            StartInfo = new()
            {
                FileName = "pwsh",
                Arguments = "-NoLogo -ServerMode",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            }
        };
        _proc.Start();

        return Task.CompletedTask;
    }

    public async Task CloseConnection(CancellationToken cancellationToken)
    {
        if (_proc != null)
        {
            _proc.Kill();
            await _proc.WaitForExitAsync(cancellationToken);
        }

        if (_failOnClose)
        {
            throw new Exception("Failed to close connection");
        }
    }

    public async Task WriteMessage(string message, CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);
        if (_logMessages)
        {
            Console.WriteLine($"Writing: {message}");
        }

        if (_failOnWrite)
        {
            throw new Exception("Failed to write message");
        }

        await _proc.StandardInput.WriteLineAsync(message.AsMemory(), cancellationToken);
    }

    public async Task<string?> WaitMessage(CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);

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

        string? msg = await _proc.StandardOutput.ReadLineAsync(cancellationToken);
        if (_logMessages)
        {
            Console.WriteLine($"Receiving: {msg}");
        }

        return msg;
    }

    public void Dispose()
    {
        _proc?.Dispose();
        _proc = null;
    }
}
