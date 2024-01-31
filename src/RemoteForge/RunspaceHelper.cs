using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Commands;

public static class RunspaceHelper
{
    private class RunspaceStateWaiter
    {
        private readonly Runspace _runspace;
        private readonly TaskCompletionSource<RunspaceStateInfo> _tcs = new();

        private RunspaceStateWaiter(Runspace runspace)
        {
            _runspace = runspace;
            _runspace.StateChanged += HandleRunspaceStateChanged;
        }

        private void HandleRunspaceStateChanged(object? source, RunspaceStateEventArgs stateEventArgs)
        {
            RunspaceState state = stateEventArgs.RunspaceStateInfo.State;
            if (state == RunspaceState.Opened || state == RunspaceState.Closed || state == RunspaceState.Broken)
            {
                _runspace.StateChanged -= HandleRunspaceStateChanged;
                _tcs.SetResult(stateEventArgs.RunspaceStateInfo);
            }
        }

        private async Task<RunspaceStateInfo> Wait(CancellationToken cancellationToken)
            => await _tcs.Task.WaitAsync(cancellationToken);

        public static async Task<RunspaceStateInfo> OpenRunspaceAsync(
            Runspace runspace,
            CancellationToken cancellationToken)
        {
            RunspaceStateWaiter waiter = new(runspace);

            bool disposeRunspace = false;
            try
            {
                // SSHConnectionInfo (maybe others as well) is reliant on there
                // being a Runspace on the thread. As we run this in a task
                // that isn't guaranteed so we create a new runspace here.
                if (Runspace.DefaultRunspace == null)
                {
                    Runspace.DefaultRunspace = RunspaceFactory.CreateRunspace();
                    Runspace.DefaultRunspace.Open();
                    disposeRunspace = true;
                }

                runspace.OpenAsync();
            }
            finally
            {
                if (disposeRunspace)
                {
                    Runspace.DefaultRunspace.Dispose();
                    Runspace.DefaultRunspace = null;
                }
            }

            return await waiter.Wait(cancellationToken);
        }
    }

    /// <summary>
    /// Create a Runspace with the provided connection info.
    /// </summary>
    /// <param name="connInfo">The connection info used when connecting the Runspace.</param>
    /// <param name="cancellationToken">The CancellationToken to observe.</param>
    /// <param name="host">PSHost to associate with the Runspace.</param>
    /// <param name="typeTable">The TypeTable to use during serialization.</param>
    /// <param name="applicationArguments">Application arguments to send to the server.</param>
    /// <returns>A task that waits for the connected runspace.</returns>
    public static async Task<Runspace> CreateRunspaceAsync(
        RunspaceConnectionInfo connInfo,
        CancellationToken cancellationToken,
        PSHost? host = null,
        TypeTable? typeTable = null,
        PSPrimitiveDictionary? applicationArguments = null)
    {
        Runspace runspace = RunspaceFactory.CreateRunspace(
            connectionInfo: connInfo,
            host: host,
            typeTable: typeTable ?? TypeTable.LoadDefaultTypeFiles(),
            applicationArguments: applicationArguments);

        bool disposeRunspace = false;
        try
        {
            RunspaceStateInfo stateInfo = await RunspaceStateWaiter.OpenRunspaceAsync(
                runspace,
                cancellationToken);

            if (stateInfo.State == RunspaceState.Broken)
            {
                throw stateInfo.Reason;
            }
        }
        catch (Exception)
        {
            disposeRunspace = true;
            throw;
        }
        finally
        {
            if (disposeRunspace)
            {
                runspace.Dispose();
            }
        }

        return runspace;
    }
}
