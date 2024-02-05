using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RemoteForge;

internal sealed class RemoteForgeClientSessionTransportManager : ClientSessionTransportManagerBase
{
    private readonly CancellationTokenSource _cancelSource = new();
    private readonly IRemoteForge _transportFactory;
    private readonly Channel<string> _inboundChannel = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _outboundChannel = Channel.CreateUnbounded<string>();
    private bool _hasRead;
    private Task? _transportWorker;

    private class MessageWriter : TextWriter
    {
        private readonly ChannelWriter<string> _writer;

        [ExcludeFromCodeCoverage]
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
    {
        SetMessageWriter(new MessageWriter(_outboundChannel.Writer));
        SendOneItem();

        Task _ = Task.Run(async () =>
        {
            while (true)
            {
                string msg;
                try
                {
                    msg = await _inboundChannel.Reader.ReadAsync(_cancelSource.Token);
                }
                catch (ChannelClosedException e)
                {
                    if (e.InnerException != null)
                    {
                        ThrowTransportException(e.InnerException);
                    }
                    break;
                }

                _hasRead = true;
                HandleOutputDataReceived(msg);
            }
        });

        _transportWorker = Task.Run(async () =>
        {
            using RemoteTransport transport = _transportFactory.CreateTransport();
            try
            {
                await transport.Run(
                    _outboundChannel.Reader,
                    _inboundChannel.Writer,
                    _cancelSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                if (_outboundChannel.Writer.TryComplete())
                {
                    ThrowTransportException(e);
                }
                else
                {
                    // The writer has been closed and we want to propagate the
                    // exception to the caller.
                    throw;
                }
            }
            finally
            {
                _inboundChannel.Writer.TryComplete();
            }
        });
    }

    private void ThrowTransportException(Exception e)
    {
        PSRemotingTransportException transportExc = e is PSRemotingTransportException te
            ? te
            : new(e.Message, e);
        TransportErrorOccuredEventArgs transportArgs = new(
            transportExc,
            TransportMethodEnum.ReceiveShellOutputEx);

        // Sigh, RaiseErrorHandler will pump the state manager which in
        // turn leads to Dispose() being called which waits for this task
        // to complete. We need to run this in a Task to avoid a deadlock.
        Task _ = Task.Run(() => RaiseErrorHandler(transportArgs));
    }

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
        => _outboundChannel.Writer.TryComplete();  // Signals the task to close the connection.

    /// <summary>
    /// Called when Runspace.Close()/CloseAsync()/Dispose() is called.
    /// </summary>
    /// <remarks>
    /// We use this as an opportunity to cancel our worker in case it hasn't
    /// successfully read any data yet.
    /// </remarks>
    public override void CloseAsync()
    {
        // We need to check if this is due to an abnormal close to stop our
        // worker.
        if (!_hasRead)
        {
            _cancelSource.Cancel();
            _outboundChannel.Writer.TryComplete();
        }

        base.CloseAsync();
    }

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
            if (!_outboundChannel.Reader.Completion.IsCompleted)
            {
                _cancelSource.Cancel();
                _outboundChannel.Writer.TryComplete();
            }

            try
            {
                _transportWorker?.Wait();
            }
            catch (AggregateException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }

            // Dispose the resources after it has been closed.
            _cancelSource.Dispose();
            _transportWorker?.Dispose();
        }

        base.Dispose(isDisposing);
    }
}
