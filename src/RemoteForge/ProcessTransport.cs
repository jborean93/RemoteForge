using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge;

public class ProcessTransport : RemoteTransport
{
    protected Process Proc { get; }

    public ProcessTransport(string executable, IEnumerable<string> arguments) : this(executable, arguments, null)
    { }

    public ProcessTransport(
        string executable,
        IEnumerable<string>? arguments,
        Dictionary<string, string>? environment)
    {
        Proc = new()
        {
            StartInfo = new()
            {
                FileName = executable,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
            },
        };
        if (arguments != null)
        {
            foreach (string arg in arguments)
            {
                Proc.StartInfo.ArgumentList.Add(arg);
            }
        }
        if (environment != null)
        {
            foreach (KeyValuePair<string, string> kvp in environment)
            {
                Proc.StartInfo.Environment.Add(kvp.Key, kvp.Value);
            }
        }
    }

    protected override Task Open(CancellationToken cancellationToken)
    {
        Proc.Start();
        return Task.CompletedTask;
    }

    protected override async Task Close(CancellationToken cancellationToken)
    {
        Proc.Kill();
        await Proc.WaitForExitAsync(cancellationToken);
    }

    protected override async Task WriteInput(string line, CancellationToken cancellationToken)
        => await Proc.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);

    protected override async Task<string?> ReadOutput(CancellationToken cancellationToken)
        => await Proc.StandardOutput.ReadLineAsync(cancellationToken);

    protected override async Task<string?> ReadError(CancellationToken cancellationToken)
        => await Proc.StandardError.ReadToEndAsync(cancellationToken);

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            Proc?.Dispose();
        }
        base.Dispose(isDisposing);
    }
}
