using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Commands;

[Cmdlet(VerbsCommon.New, "RemoteForgeSession")]
[OutputType(typeof(PSSession))]
public sealed class NewRemoteForgeSessionCommand : PSCmdlet, IDisposable
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
                creationTasks.Add((connection.ToString(), Task.FromResult(connection.PSSession)));
            }
            else
            {
                RunspaceConnectionInfo? connInfo = connection.GetConnectionInfo(this);
                if (connInfo == null)
                {
                    continue;
                }

                creationTasks.Add((connection.ToString(), Task.Run(async () =>
                {
                    Runspace rs = await RunspaceHelper.CreateRunspaceAsync(
                        connInfo,
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
    internal readonly Func<RunspaceConnectionInfo> _connInfoFactory;
    internal PSSession? PSSession { get; }

    public StringForgeConnectionInfoPSSession(string info)
    {
        _originalString = info;
        _connInfoFactory = () => RemoteForgeRegistration.CreateForgeConnectionInfo(info);
    }

    public StringForgeConnectionInfoPSSession(IRemoteForge forge)
    {
        _originalString = forge.GetTransportString();
        _connInfoFactory = () => new RemoteForgeConnectionInfo(forge);
    }

    public StringForgeConnectionInfoPSSession(RunspaceConnectionInfo info)
    {
        _originalString = GetConnectionInfoString(info);
        _connInfoFactory = () => info;
    }

    public StringForgeConnectionInfoPSSession(PSSession session)
    {
        _originalString = GetConnectionInfoString(session.Runspace.OriginalConnectionInfo);
        _connInfoFactory = () => session.Runspace.OriginalConnectionInfo;
        PSSession = session;
    }

    // We use a Func so the parameter binding works and we can provide a
    // better error at runtime.
    internal RunspaceConnectionInfo? GetConnectionInfo(PSCmdlet cmdlet)
    {
        try
        {
            return _connInfoFactory();
        }
        catch (Exception e)
        {
            ErrorRecord err = new(
                e,
                "InvalidForgeConnection",
                ErrorCategory.InvalidArgument,
                _originalString);
            cmdlet.WriteError(err);
            return null;
        }
    }

    private static string GetConnectionInfoString(RunspaceConnectionInfo info) => info switch
    {
        RemoteForgeConnectionInfo f => f.ConnectionUri,
        SSHConnectionInfo s => GetSSHConnectionInfoString(s),
        WSManConnectionInfo w => GetWSManConnectionInfoString(w),
        _ => $"{info.GetType().Name}:{info.ComputerName}",
    };

    private static string GetSSHConnectionInfoString(SSHConnectionInfo connInfo)
    {
        StringBuilder connString = new("ssh:");
        if (!string.IsNullOrWhiteSpace(connInfo.UserName))
        {
            connString.Append(connInfo.UserName).Append('@');
        }

        if (
            IPAddress.TryParse(connInfo.ComputerName, out IPAddress? addr) &&
            addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            connString.Append('[').Append(connInfo.ComputerName).Append(']');
        }
        else
        {
            connString.Append(connInfo.ComputerName);
        }

        if (connInfo.Port != 22)
        {
            connString.Append(':').Append(connInfo.Port);
        }

        return connString.ToString();
    }

    private static string GetWSManConnectionInfoString(WSManConnectionInfo connInfo)
    {
        StringBuilder connString = new();

        if (connInfo.AppName == "wsman")
        {
            if (connInfo.Scheme == "http" && connInfo.Port == 5985)
            {
                connString.Append(connInfo.ComputerName);
            }
            else if (connInfo.Scheme == "https" && connInfo.Port == 5986)
            {
                connString.Append("https://").Append(connInfo.ComputerName);
            }
        }

        if (connString.Length == 0)
        {
            connString.Append(connInfo.ConnectionUri);
        }

        return $"wsman:{connString}";
    }

    public override string ToString()
        => _originalString;
}
