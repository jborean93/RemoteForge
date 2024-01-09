using System;
using System.Diagnostics;
using System.Threading;
using RemoteForge.Shared;

namespace RemoteForge.Client;

public sealed class PipeInfo : IRemoteForge
{
    public IRemoteForgeTransport CreateTransport()
        => new PipeTransport();
}

public sealed class PipeTransport : IRemoteForgeTransport, IDisposable
{
    private Process? _proc;

    public void CreateConnection(CancellationToken cancellationToken)
    {
        _proc = new()
        {
            StartInfo = new()
            {
                FileName = "pwsh",
                Arguments = "-s",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            }
        };
        _proc.Start();
    }

    public void CloseConnection(CancellationToken cancellationToken)
    {
        Console.WriteLine("Kill");
        //_proc?.Kill();
        Console.WriteLine("WaitForExit");
        //_proc?.WaitForExit();
        Console.WriteLine("CloseConnection done");
    }

    public void WriteMessage(string message, CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);
        Console.WriteLine($"Writing: {message}");
        _proc.StandardInput.WriteLine(message);
    }

    public string WaitMessage(CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);
        Console.WriteLine("Starting recv");
        string? msg = _proc.StandardOutput.ReadLineAsync(cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        Console.WriteLine($"Receiving: {msg}");
        return msg ?? "";
    }

    public void Dispose()
    {
        _proc?.Kill();
        _proc?.WaitForExit();
        _proc?.Dispose();
        _proc = null;
    }
}
