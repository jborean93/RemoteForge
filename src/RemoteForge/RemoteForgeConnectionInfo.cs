using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;

namespace RemoteForge;

public sealed class RemoteForgeConnectionInfo : RunspaceConnectionInfo
{
    private IRemoteForge _transportFactory;

    public override string? ComputerName { get; set; }

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

    public override string ToString()
        => ConnectionUri;
}
