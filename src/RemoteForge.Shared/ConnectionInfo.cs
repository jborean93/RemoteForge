using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteForge.Shared;

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

    public RemoteForgeConnectionInfo(IRemoteForge transportFactory)
    {
        _transportFactory = transportFactory;
    }

    public override RunspaceConnectionInfo Clone()
        => new RemoteForgeConnectionInfo(_transportFactory);

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
    private readonly MessageWriter _writer;
    private Task? _recvTask;
    private CancellationTokenSource _cancelSource = new();

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
            Console.WriteLine($"Test: '{value}'");
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
        _writer = new(transport, _cancelSource.Token);
    }

    public override void CreateAsync()
    {
        _transport.CreateConnection(_cancelSource.Token);
        _recvTask = Task.Run(() =>
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                string message = _transport.WaitMessage(_cancelSource.Token);
                if (string.IsNullOrWhiteSpace(message))
                {
                    break;
                }
                HandleOutputDataReceived(message);
            }
        });
        SetMessageWriter(_writer);
        SendOneItem();
        Console.WriteLine("Writer set");
    }

    public override void CloseAsync()
    {
        Console.WriteLine("CloseAsync");
        base.CloseAsync();
        Console.WriteLine("Close sent");
        _transport.CloseConnection(_cancelSource.Token);
        Console.WriteLine("Connection closed");
    }

    protected override void Dispose(bool isDisposing)
    {
        Console.WriteLine("Dispose");
        base.Dispose(isDisposing);

        if (isDisposing)
        {
            CleanupConnection();
            _writer?.Dispose();
            _cancelSource?.Dispose();
        }
    }

    protected override void CleanupConnection()
    {
        Console.WriteLine("CleanupConnection");
        if (_transport is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
