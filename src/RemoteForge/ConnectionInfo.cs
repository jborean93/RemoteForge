using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Shared;

internal sealed class RemoteForgeConnectionInfo : RunspaceConnectionInfo
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

    public RemoteForgeConnectionInfo(IRemoteForge transportFactory)
    {
        _transportFactory = transportFactory;
    }

    public override RunspaceConnectionInfo Clone()
        => this;

    public override BaseClientSessionTransportManager CreateClientSessionTransportManager(
        Guid instanceId,
        string sessionName,
        PSRemotingCryptoHelper cryptoHelper)
    {
        return new RemoteForgeClientSessionTransportManager(
            transport: _transportFactory.CreateTransport(),
            runspaceId: instanceId,
            cryptoHelper: cryptoHelper);
    }
}

internal sealed class RemoteForgeClientSessionTransportManager : ClientSessionTransportManagerBase
{
    private readonly IRemoteForgeTransport _transport;
    private readonly CancellationTokenSource _cancelSource = new();
    private bool _isClosed;

    private class MessageWriter : TextWriter
    {
        private readonly IRemoteForgeTransport _transport;
        private readonly CancellationToken _cancelToken;

        public override Encoding Encoding => Encoding.UTF8;

        public MessageWriter(IRemoteForgeTransport transport, CancellationToken cancellationToken)
        {
            _transport = transport;
            _cancelToken = cancellationToken;
        }

        public override void WriteLine(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _transport.WriteMessage(value, _cancelToken);
            }
        }
    }

    internal RemoteForgeClientSessionTransportManager(
        IRemoteForgeTransport transport,
        Guid runspaceId,
        PSRemotingCryptoHelper cryptoHelper
    ) : base(runspaceId, cryptoHelper)
    {
        _transport = transport;
    }

    /// <summary>
    /// Starts the transport connection.
    /// </summary>
    /// <remarks>
    /// This is called when Runspace.OpenAsync() is called and will block
    /// that call until it returns.
    /// </remarks>
    public override void CreateAsync()
        => Task.Run(() => RunTransport(_cancelSource.Token));

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
    {
        _isClosed = true;
        _transport.CloseConnection(_cancelSource.Token);
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
            _cancelSource?.Cancel();
            _cancelSource?.Dispose();

            if (_transport is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        base.Dispose(isDisposing);
    }


    /// <summary>
    /// Transport task that will start the connection and read any output.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the task.</param>
    private void RunTransport(CancellationToken cancellationToken)
    {
        TransportMethodEnum currentStage = TransportMethodEnum.CreateShellEx;
        bool isOpened = false;

        try
        {
            _transport.CreateConnection(cancellationToken);
            isOpened = true;
            cancellationToken.ThrowIfCancellationRequested();

            // Sets the writer to our custom TextWriter which calls the transport
            // WriteMessage method when data needs to be sent.
            currentStage = TransportMethodEnum.SendShellInputEx;
            using MessageWriter writer = new(_transport, cancellationToken);
            SetMessageWriter(writer);

            // Starts the writing process by sending data to the writer.
            SendOneItem();

            currentStage = TransportMethodEnum.ReceiveShellOutputEx;
            while (!cancellationToken.IsCancellationRequested)
            {
                // We continously run
                string? message = _transport.WaitMessage(cancellationToken);
                if (string.IsNullOrWhiteSpace(message))
                {
                    break;
                }
                HandleOutputDataReceived(message);
            }

            if (!_isClosed)
            {
                string msg = "Transport has returned no data before it has been closed";
                PSRemotingTransportException err = new(msg);
                RaiseErrorHandler(new(err, currentStage));
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            TransportErrorOccuredEventArgs transportArgs = new(
                new PSRemotingTransportException(e.Message, e),
                currentStage);
            RaiseErrorHandler(transportArgs);
        }
        finally
        {
            // This is a failsafe cleanup in case of a critical error.
            if (isOpened && !_isClosed)
            {
                _transport.CloseConnection(default);
            }
        }
    }
}
