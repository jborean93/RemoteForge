using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RemoteForge;

[Cmdlet(VerbsCommon.New, "RemoteForgeSession")]
[OutputType(typeof(PSSession))]
public sealed class NewRemoteForgeSession : NewRemoteForgeSessionBase
{
    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public StringOrForge[] ComputerName { get; set; } = Array.Empty<StringOrForge>();


    protected override void ProcessRecord()
    {
        foreach (PSSession session in CreatePSSessions(ComputerName.Select(c => c.ConnectionInfo)))
        {
            WriteObject(session);
        }
    }
}

public sealed class StringOrForge
{
    internal RunspaceConnectionInfo ConnectionInfo { get; }

    public StringOrForge(string info)
    {
        ConnectionInfo = RemoteForgeRegistrations.CreateForgeConnectionInfo(new Uri(info));
    }

    public StringOrForge(IRemoteForge forge)
    {
        ConnectionInfo = new RemoteForgeConnectionInfo(forge);
    }

    public StringOrForge(RunspaceConnectionInfo info)
    {
        ConnectionInfo = info;
    }
}
