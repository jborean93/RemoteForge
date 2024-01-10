using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace RemoteForge.Shared;

public abstract class NewRemoteForgeSessionBase : PSCmdlet, IDisposable
{
    private CancellationTokenSource _cancelTokenSource = new();

    /// <summary>
    /// Helper class to encapsulate
    /// </summary>
    private class OpenState : IDisposable
    {
        // We use the Slim variant so we have access to Wait with a
        // cancellation token. Set the spin count to 0 to always use a kernel
        // event as opening a runspace will most likely exceed the normal spin
        // timeframe.
        private readonly ManualResetEventSlim _openEvent = new(false, 0);

        public Runspace Runspace { get; }

        public OpenState(Runspace runspace)
        {
            Runspace = runspace;
            Runspace.StateChanged += HandleRunspaceStateChanged;

            // This blocks until the client transport CreateAsync returns.
            Runspace.OpenAsync();
        }

        private void HandleRunspaceStateChanged(object? source, RunspaceStateEventArgs stateEventArgs)
        {
            RunspaceState state = stateEventArgs.RunspaceStateInfo.State;
            if (state == RunspaceState.Opened || state == RunspaceState.Closed || state == RunspaceState.Broken)
            {
                Runspace.StateChanged -= HandleRunspaceStateChanged;
                _openEvent.Set();
            }
        }

        public void Wait(CancellationToken cancellationToken)
            => _openEvent.Wait(cancellationToken);

        public void Dispose()
        {
            _openEvent?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    protected IEnumerable<PSSession> CreatePSSessions(
        IEnumerable<RunspaceConnectionInfo> connections,
        PSHost? host = null,
        TypeTable? typeTable = null,
        PSPrimitiveDictionary? applicationArguments = null
    )
    {
        host ??= Host;
        typeTable ??= TypeTable.LoadDefaultTypeFiles();

        List<OpenState> openingRunspaces = new();
        foreach (RunspaceConnectionInfo connInfo in connections)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace(
                connectionInfo: connInfo,
                host: host,
                typeTable: typeTable,
                applicationArguments: applicationArguments);
            openingRunspaces.Add(new(runspace));
        }

        foreach (OpenState state in openingRunspaces)
        {
            Runspace runspace = state.Runspace;

            bool disposeRunspace = false;
            try
            {
                state.Wait(_cancelTokenSource.Token);

                if (runspace.RunspaceStateInfo.State == RunspaceState.Broken)
                {
                    disposeRunspace = true;

                    // Reason message here is most likely useless but it's better than nothing.
                    ErrorRecord err = new(
                        runspace.RunspaceStateInfo.Reason,
                        "RemoteForgeFailedConnection",
                        ErrorCategory.ConnectionError,
                        null);

                    WriteError(err);
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                disposeRunspace = true;
                continue;
            }
            finally
            {
                if (disposeRunspace)
                {
                    runspace.Dispose();
                }
                state.Dispose();
            }

            yield return PSSession.Create(
                runspace: runspace,
                transportName: "RemoteForge",
                psCmdlet: this);
        }
    }

    protected override void StopProcessing()
        => _cancelTokenSource?.Cancel();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _cancelTokenSource?.Dispose();
        }
    }
}
