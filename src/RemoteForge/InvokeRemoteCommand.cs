using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using RemoteForge;
using RemoteForge.Shared;

[Cmdlet(VerbsLifecycle.Invoke, "Remote")]
public sealed class InvokeRemoteCommand : NewRemoteForgeSessionBase
{
    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [Alias("Cn")]
    public StringOrForge[] ComputerName { get; set; } = Array.Empty<StringOrForge>();

    [Parameter(
        Mandatory = true
    )]
    public ScriptBlock? ScriptBlock { get; set; }

    [Parameter]
    public PSObject[] InputObject { get; set; } = Array.Empty<PSObject>();

    protected override void EndProcessing()
    {
        Debug.Assert(ScriptBlock != null);

        PSSession[] sessions = CreatePSSessions(ComputerName.Select(c => c.ConnectionInfo)).ToArray();
        try
        {
            List<(PowerShell, IAsyncResult)> tasks = new();
            foreach (PSSession s in sessions)
            {
                PowerShell ps = PowerShell.Create(sessions[0].Runspace);
                ps.AddScript(ScriptBlock.ToString());
                IAsyncResult invokeTask = ps.BeginInvoke();
                tasks.Add((ps, invokeTask));
            }

            WaitHandle.WaitAll(tasks.Select(t => t.Item2.AsyncWaitHandle).ToArray());
            foreach ((PowerShell ps, IAsyncResult invokeTask) in tasks)
            {
                PSDataCollection<PSObject> result = ps.EndInvoke(invokeTask);
                WriteObject(result, true);
                ps.Dispose();
            }

        }
        finally
        {
            foreach (PSSession s in sessions)
            {
                s.Runspace.Close();
                s.Runspace.Dispose();
            }
        }
    }
}
