using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;

namespace RemoteForge.Commands;

[Cmdlet(
    VerbsCommon.Enter,
    "Remote"
)]
[ExcludeFromCodeCoverage(Justification = "Cannot test this in CI, requires interaction")]
public sealed class EnterRemoteCommand : PSCmdlet, IDisposable
{
    private readonly CancellationTokenSource _cancelSource = new();

    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    [Alias("ComputerName", "Cn")]
    public StringForgeConnectionInfoPSSession? ConnectionInfo { get; set; }

    [Parameter]
    public PSPrimitiveDictionary? ApplicationArguments { get; set; }

    protected override void EndProcessing()
    {
        Debug.Assert(ConnectionInfo != null);

        if (!(Host is IHostSupportsInteractiveSession host))
        {
            ErrorRecord err = new(
                new ArgumentException("The host is not interactive and does not support Enter-Remote."),
                "HostDoesNotSupportPushRUnspace",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
            return;
        }

        Runspace? runspace = ConnectionInfo.PSSession?.Runspace;
        bool disposeRunspace = false;
        try
        {
            if (runspace == null)
            {
                RunspaceConnectionInfo? connInfo = ConnectionInfo.GetConnectionInfo(this);
                if (connInfo == null)
                {
                    return;
                }

                disposeRunspace = true;
                runspace = RunspaceHelper.CreateRunspaceAsync(
                    connInfo,
                    _cancelSource.Token,
                    Host,
                    typeTable: null,
                    applicationArguments: ApplicationArguments).GetAwaiter().GetResult();

                // This property is internal but we need the host to close the
                // runspace when it is popped. We cannot do it in this cmdlet
                // as it exits when the pushed runspace is still active.
                PropertyInfo shouldCloseProp = runspace.GetType().GetProperty(
                    "ShouldCloseOnPop",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new RuntimeException("Failed to find ShouldCloseOnPop");
                shouldCloseProp.SetValue(runspace, true);
            }

            if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                string msg = $"Runspace was in the '{runspace.RunspaceStateInfo.State}' state but must be Opened to enter";
                ErrorRecord err = new(
                    new InvalidOperationException(msg),
                    "EnterSessionInvalidState",
                    ErrorCategory.InvalidOperation,
                    null);
                ThrowTerminatingError(err);
            }

            if (runspace.RunspaceAvailability != RunspaceAvailability.Available)
            {
                string msg = $"Runspace availability was '{runspace.RunspaceAvailability}' but must be Available to enter";
                ErrorRecord err = new(
                    new InvalidOperationException(msg),
                    "EnterSessionInvalidAvailability",
                    ErrorCategory.InvalidOperation,
                    null);
                ThrowTerminatingError(err);
            }

            // The ComputerName is used to build the prompt, except for SSH or
            // WSMan we want to use the specified forge connection string.
            if (!(
                runspace.ConnectionInfo is SSHConnectionInfo ||
                runspace.ConnectionInfo is WSManConnectionInfo
            ))
            {
                runspace.ConnectionInfo.ComputerName = ConnectionInfo.ToString();
            }

            host.PushRunspace(runspace);
        }
        catch
        {
            if (runspace != null && disposeRunspace)
            {
                runspace.Dispose();
            }
            throw;
        }
    }

    protected override void StopProcessing()
    {
        _cancelSource?.Cancel();
    }

    public void Dispose()
    {
        _cancelSource?.Dispose();
    }
}
