using System;
using System.Diagnostics;
using System.Threading;
using RemoteForge.Shared;

namespace RemoteForge.Client;

public sealed class PipeInfo : IRemoteForge
{
    public static string ForgeId => "pipe";

    public IRemoteForgeTransport CreateTransport()
        => new PipeTransport();

    public static IRemoteForge Create(Uri info)
        => new PipeInfo();
}

public sealed class PipeTransport : IRemoteForgeTransport, IDisposable
{
    private Process? _proc;

    public void CreateConnection(CancellationToken cancellationToken)
    {
        // Thread.Sleep(5000);
        // System.Threading.Tasks.Task.Delay(5000, cancellationToken).GetAwaiter().GetResult();
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
        _proc?.Kill();
        _proc?.WaitForExit();
    }

    public void WriteMessage(string message, CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);
        // Console.WriteLine($"Writing: {message}");
        _proc.StandardInput.WriteLine(message);
    }

    public string? WaitMessage(CancellationToken cancellationToken)
    {
        Debug.Assert(_proc != null);
        string? msg = _proc.StandardOutput.ReadLineAsync(cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        // Console.WriteLine($"Receiving: {msg}");
        return msg;
    }

    public void Dispose()
    {
        _proc?.Dispose();
        _proc = null;
    }
}
