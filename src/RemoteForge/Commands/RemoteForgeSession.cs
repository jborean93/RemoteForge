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
        List<(string, Task<PSSession>)> creationTasks = new();
        foreach (StringForgeConnectionInfoPSSession connection in ConnectionInfo)
        {
            if (connection.PSSession != null)
            {
                creationTasks.Add((connection.ToString(), new(() => connection.PSSession)));
            }
            else
            {
                creationTasks.Add((connection.ToString(), Task.Run(async () =>
                {
                    Runspace rs = await RunspaceHelper.CreateRunspaceAsync(
                        connection.ConnectionInfo,
                        _cancelTokenSource.Token,
                        host: Host);

                    return PSSession.Create(
                        runspace: rs,
                        transportName: "RemoteForge",
                        psCmdlet: this);
                })));
            }
        }

        foreach ((string connInfo, Task<PSSession> task) in creationTasks)
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
                    connInfo)
                {
                    ErrorDetails = new($"Failed to open runspace for '{connInfo}': {e.Message}")
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
    private readonly string _originalString;
    internal RunspaceConnectionInfo ConnectionInfo { get; }
    internal PSSession? PSSession { get; }

    public StringForgeConnectionInfoPSSession(string info)
    {
        _originalString = info;
        ConnectionInfo = RemoteForgeRegistration.CreateForgeConnectionInfo(info);
    }

    public StringForgeConnectionInfoPSSession(IRemoteForge forge)
    {
        _originalString = forge.GetTransportString();
        ConnectionInfo = new RemoteForgeConnectionInfo(forge);
    }

    public StringForgeConnectionInfoPSSession(RunspaceConnectionInfo info)
    {
        _originalString = GetConnectionInfoString(info);
        ConnectionInfo = info;
    }

    public StringForgeConnectionInfoPSSession(PSSession session)
    {
        ConnectionInfo = session.Runspace.OriginalConnectionInfo;
        _originalString = GetConnectionInfoString(ConnectionInfo);
        PSSession = session;
    }

    private static string GetConnectionInfoString(RunspaceConnectionInfo info) => info switch
    {
        RemoteForgeConnectionInfo f => f.ConnectionUri,
        SSHConnectionInfo => $"ssh:{info.ComputerName}",
        WSManConnectionInfo => $"wsman:{info.ComputerName}",
        NamedPipeConnectionInfo p => $"pipe:{p.CustomPipeName}",
        ContainerConnectionInfo => $"container:{info.ComputerName}",
        VMConnectionInfo => $"vm:{info.ComputerName}",
        _ => $"{info.GetType().Name}:{info.ComputerName}",
    };

    public override string ToString()
        => _originalString;
}
