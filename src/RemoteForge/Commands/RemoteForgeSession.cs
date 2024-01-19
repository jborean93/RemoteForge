using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RemoteForge.Commands;

[Cmdlet(VerbsCommon.New, "RemoteForgeSession")]
[OutputType(typeof(PSSession))]
public sealed class NewRemoteForgeSession : NewRemoteForgeSessionBase
{
    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    [Alias("Cn", "Connection")]
    public StringForgeConnectionInfoPSSession[] ComputerName { get; set; } = Array.Empty<StringForgeConnectionInfoPSSession>();

    protected override void ProcessRecord()
    {
        foreach (StringForgeConnectionInfoPSSession connection in ComputerName)
        {
            if (connection.PSSession != null)
            {
                WriteObject(connection.PSSession);
            }
            else
            {
                foreach (PSSession s in CreatePSSessions(new [] { connection.ConnectionInfo }))
                {
                    WriteObject(s);
                }
            }
        }
    }
}

public sealed class StringForgeConnectionInfoPSSession
{
    internal RunspaceConnectionInfo ConnectionInfo { get; }
    internal PSSession? PSSession { get; }

    public StringForgeConnectionInfoPSSession(string info)
    {
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
}
