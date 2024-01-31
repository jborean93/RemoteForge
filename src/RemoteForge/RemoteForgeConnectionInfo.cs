using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RemoteForge;

public sealed class RemoteForgeConnectionInfo : RunspaceConnectionInfo
{
    private IRemoteForge _transportFactory;

    public int ConnectingTimeout { get; set; }

    public override string ComputerName { get; set; } = string.Empty;

    public override PSCredential? Credential { get; set; }

    public override AuthenticationMechanism AuthenticationMechanism
    {
        get => AuthenticationMechanism.Default;
        set => throw new NotImplementedException();
    }

    public override string CertificateThumbprint
    {
        get => string.Empty;
        set => throw new NotImplementedException();
    }

    internal string ConnectionUri { get; }

    public RemoteForgeConnectionInfo(IRemoteForge transportFactory)
    {
        _transportFactory = transportFactory;
        ConnectionUri = transportFactory.GetTransportString();
    }

    public override RunspaceConnectionInfo Clone()
        => this;

    public override BaseClientSessionTransportManager CreateClientSessionTransportManager(
        Guid instanceId,
        string sessionName,
        PSRemotingCryptoHelper cryptoHelper)
    {
        return new RemoteForgeClientSessionTransportManager(
            transportFactory: _transportFactory,
            runspaceId: instanceId,
            cryptoHelper: cryptoHelper);
    }
}

internal sealed class RemoteForgeClientSessionTransportManager : ClientSessionTransportManagerBase
{
    private readonly CancellationTokenSource _cancelSource = new();
    private readonly IRemoteForge _transportFactory;
    private readonly TaskCompletionSource _closeTask = new();
    private Task? _worker;

    private class MessageWriter : TextWriter
    {
        private readonly ChannelWriter<string> _writer;

        public override Encoding Encoding => Encoding.UTF8;

        public MessageWriter(ChannelWriter<string> writer)
        {
            _writer = writer;
        }

        public override void WriteLine(string? value)
        {
            // If TryWrite returned false on an unbounded channel then it has
            // been marked as completed.
            if (!_writer.TryWrite(value ?? string.Empty))
            {
                // It is important this exception is an IOException. The
                // PowerShell code treats this exception on a failure to
                // signal the transport has been closed.
                throw new IOException("Transport has been closed");
            }
        }
    }

    internal RemoteForgeClientSessionTransportManager(
        IRemoteForge transportFactory,
        Guid runspaceId,
        PSRemotingCryptoHelper cryptoHelper
    ) : base(runspaceId, cryptoHelper)
    {
        _transportFactory = transportFactory;
    }

    /// <summary>
    /// Starts the transport connection.
    /// </summary>
    /// <remarks>
    /// This is called when Runspace.OpenAsync() is called and will block
    /// that call until it returns.
    /// </remarks>
    public override void CreateAsync()
        => _worker = Task.Run(RunTransport);

    /// <summary>
    /// Performs any connection cleanup tasks after it is no longer needed.
    /// </summary>
    /// <remarks>
    /// This is called once the transport managed as received the CloseAck
    /// message for the Runspace. It is designed to provide a way for the
    /// transport to close anything once the session is no longer used. This
    /// might be missed if the session is interrupted/closed before the
    /// CloseAck is received.
    /// </remarks>
    protected override void CleanupConnection()
        => _closeTask.SetResult(); // Signals the task to close the connection.

    /// <summary>
    /// Disposes the client transport and attempts to clean everything up.
    /// </summary>
    /// <param name="isDisposing">This has been called from an explicit Dispose() call.</param>
    /// <remarks>
    /// Called when the Runspace is disposed, we use this as an opportunity to
    /// dispose the underlying transport if it is an IDisposable type.
    /// </remarks>
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            // If the close task hasn't been signaled then we've reached
            // dispose with a broken runspace. We want to ensure the worker has
            // been cancelled.
            if (!_closeTask.Task.IsCompleted)
            {
                _cancelSource.Cancel();
            }
            try
            {
                _worker?.Wait();
            }
            catch (AggregateException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }

            // Dispose the resources after it has been closed.
            _cancelSource.Dispose();
            _worker?.Dispose();
        }

        base.Dispose(isDisposing);
    }

    /// <summary>
    /// Transport task that will start the connection and read any output.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the task.</param>
    private async Task RunTransport()
    {
        Channel<string> messageChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,  // MessageWriter and us use it to mark as completed
        });
        ChannelWriter<string> writer = messageChannel.Writer;
        ChannelReader<string> reader = messageChannel.Reader;

        TransportMethodEnum currentStage = TransportMethodEnum.CreateShellEx;
        bool isOpened = false;

        IRemoteForgeTransport transport = _transportFactory.CreateTransport();
        try
        {
            await transport.CreateConnection(_cancelSource.Token);
            isOpened = true;

            // Start the reader and writer tasks now the connection is opened.
            Task readTask = Task.Run(async () => await ReaderWorker(reader, transport));
            Task writeTask = Task.Run(async () => await WriterWorker(transport));

            // Sets the writer to our custom TextWriter which writes the
            // messages to the channel which our task above reads from.
            currentStage = TransportMethodEnum.SendShellInputEx;
            SetMessageWriter(new MessageWriter(writer));

            // Starts the writing process by sending data to the writer.
            SendOneItem();

            // Wait for either the read or write task to end, will indicate it
            // is done or failed.
            currentStage = TransportMethodEnum.ReceiveShellOutputEx;
            Task doneTask = await Task.WhenAny(readTask, writeTask, _closeTask.Task);
            if (doneTask == writeTask && writeTask.IsCompletedSuccessfully)
            {
                // If we reached here PowerShell doesn't know the transport is
                // closed so we raise the exception.
                throw new PSRemotingTransportException(
                    "Transport has returned no data before it has been closed");
            }

            // If the read or write task threw an exception it should be raised
            // here.
            await doneTask;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            // On an error we want to ensure the writer raises IOException so
            // that it is seen that the transport is closed.
            writer.Complete();

            PSRemotingTransportException transportExc = e is PSRemotingTransportException te
                ? te
                : new(e.Message, e);
            TransportErrorOccuredEventArgs transportArgs = new(
                transportExc,
                currentStage);

            // Sigh, RaiseErrorHandler will pump the state manager which in
            // turn leads to Dispose() being called which waits for this task
            // to complete. We need to run this in a Task to avoid a deadlock.
            Task _ = Task.Run(() => RaiseErrorHandler(transportArgs));
        }
        finally
        {
            if (isOpened)
            {
                // We wrap this to ensure the cancel token isn't cancelled on
                // us while we are closing the connection normally.
                try
                {
                    await transport.CloseConnection(_cancelSource.Token);
                }
                catch (OperationCanceledException)
                { } // Dispose() was called so this is expected.
            }

            if (transport is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private async Task ReaderWorker(ChannelReader<string> reader, IRemoteForgeTransport transport)
    {
        while (true)
        {
            // Throws ChannelClosedException if the writer is complete
            string msg = await reader.ReadAsync(_cancelSource.Token);
            await transport.WriteMessage(msg, _cancelSource.Token);
        }
    }

    private async Task WriterWorker(IRemoteForgeTransport transport)
    {
        while (true)
        {
            // We continuously run this until the transport has been
            // cancelled or the transport has reported it has been closed
            // with a null/empty message.
            string? message = await transport.WaitMessage(_cancelSource.Token);
            if (string.IsNullOrWhiteSpace(message))
            {
                break;
            }
            HandleOutputDataReceived(message);
        }
    }
}
