using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using RemoteForge.Shared;

namespace RemoteForge;

[Cmdlet(VerbsCommon.New, "RemoteForgeSession")]
[OutputType(typeof(PSSession))]
public sealed class NewRemoteForgeSession : PSCmdlet
{
    private ManualResetEvent? _openEvent = null;
    private Runspace? _runspace = null;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public IRemoteForge[] ConnectionInfo { get; set; } = Array.Empty<IRemoteForge>();

    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = "";

    protected override void ProcessRecord()
    {
        foreach (IRemoteForge forge in ConnectionInfo)
        {
            RemoteForgeConnectionInfo connInfo = new(forge);

            Runspace runspace = RunspaceFactory.CreateRunspace(
                connectionInfo: connInfo,
                host: Host,
                typeTable: TypeTable.LoadDefaultTypeFiles(),
                applicationArguments: null,
                name: Name);
            _runspace = runspace;

            using (_openEvent = new ManualResetEvent(false))
            {
                runspace.StateChanged += HandleRunspaceStateChanged;
                runspace.OpenAsync();
                _openEvent?.WaitOne();

                if (runspace.RunspaceStateInfo.State == RunspaceState.Broken)
                {
                    // Reason message here is most likely useless but it's better than nothing.
                    ErrorRecord err = new(
                        runspace.RunspaceStateInfo.Reason,
                        "RemoteForgeFailedConnection",
                        ErrorCategory.ConnectionError,
                        null);

                    WriteError(err);
                    continue;
                }

                WriteObject(PSSession.Create(
                    runspace: runspace,
                    transportName: "RemoteForge",
                    psCmdlet: this));
            }
        }
    }

    protected override void StopProcessing()
    {
        // FUTURE: Should somehow cancel the open process/kill it
        SetOpenEvent();
    }

    private void HandleRunspaceStateChanged(object? source, RunspaceStateEventArgs stateEventArgs)
    {
        switch (stateEventArgs.RunspaceStateInfo.State)
        {
            case RunspaceState.Opened:
            case RunspaceState.Closed:
            case RunspaceState.Broken:
                if (_runspace != null)
                {
                    _runspace.StateChanged -= HandleRunspaceStateChanged;
                }

                SetOpenEvent();
                break;
        }
    }

    private void SetOpenEvent()
    {
        try
        {
            _openEvent?.Set();
        }
        catch (ObjectDisposedException) { }
    }
}
