using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Commands;

[Cmdlet(VerbsCommon.New, "RemoteForgeSession")]
[OutputType(typeof(PSSession))]
public sealed class NewRemoteForgeSession : PSCmdlet, IDisposable
{
    private CancellationTokenSource _cancelTokenSource = new();

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    [Alias("ComputerName", "Cn")]
    public StringForgeConnectionInfoPSSession[] ConnectionInfo { get; set; } = Array.Empty<StringForgeConnectionInfoPSSession>();

    protected override void ProcessRecord()
    {
        List<Task<PSSession>> creationTasks = new();
        foreach (StringForgeConnectionInfoPSSession connection in ConnectionInfo)
        {
            if (connection.PSSession != null)
            {
                creationTasks.Add(new(() => connection.PSSession));
            }
            else
            {
                creationTasks.Add(Task.Run(async () =>
                {
                    Runspace rs = await RunspaceHelper.CreateRunspaceAsync(
                        connection.ConnectionInfo,
                        _cancelTokenSource.Token,
                        host: Host);

                    return PSSession.Create(
                        runspace: rs,
                        transportName: "RemoteForge",
                        psCmdlet: this);
                }));
            }
        }

        foreach (Task<PSSession> task in creationTasks)
        {
            try
            {
                PSSession session = task.GetAwaiter().GetResult();
                WriteObject(session);
            }
            catch (OperationCanceledException)
            {
                continue;
            }
            catch (Exception e)
            {
                ErrorRecord err = new(
                    e,
                    "RemoteForgeFailedConnection",
                    ErrorCategory.ConnectionError,
                    null)
                {
                    ErrorDetails = new($"Failed to open runspace for 'FIXME': {e.Message}")
                };

                WriteError(err);
            }
        }
    }

    protected override void StopProcessing()
        => _cancelTokenSource?.Cancel();

    public void Dispose()
    {
        _cancelTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class StringForgeConnectionInfoPSSession
{
    private readonly string? _originalString;
    internal RunspaceConnectionInfo ConnectionInfo { get; }
    internal PSSession? PSSession { get; }

    public StringForgeConnectionInfoPSSession(string info)
    {
        _originalString = info;
        ConnectionInfo = RemoteForgeRegistration.CreateForgeConnectionInfo(new Uri(info));
    }

    public StringForgeConnectionInfoPSSession(IRemoteForge forge)
    {
        ConnectionInfo = new RemoteForgeConnectionInfo(forge);
    }

    public StringForgeConnectionInfoPSSession(RunspaceConnectionInfo info)
    {
        ConnectionInfo = info;
    }

    public StringForgeConnectionInfoPSSession(PSSession session)
    {
        ConnectionInfo = session.Runspace.OriginalConnectionInfo;
        PSSession = session;
    }

    public override string ToString()
    {
        // TODO: Get better value for other examples
        return _originalString ?? ConnectionInfo.GetType().FullName!;
    }
}
