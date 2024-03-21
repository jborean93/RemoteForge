using RemoteForge;
using RemoteForge.Commands;
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Xunit;

namespace RemoteForgeTests;

internal sealed class CustomRunspaceConnectionInfo : RunspaceConnectionInfo
{
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

    public CustomRunspaceConnectionInfo(string computerName)
    {
        ComputerName = computerName;
    }
}

public class StringForgeConnectionInfoPSSessionTests : IDisposable
{
    private readonly OnModuleImportAndRemove _remoteForgeModule = new();

    public StringForgeConnectionInfoPSSessionTests() : base()
    {
        Runspace.DefaultRunspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
        Runspace.DefaultRunspace.Open();
        _remoteForgeModule.OnImport();
    }

    [Theory]
    [InlineData("foo", "foo")]
    [InlineData("Foo", "Foo")]
    [InlineData("Test:foo", "Test:foo")]
    [InlineData("Test:Foo", "Test:Foo")]
    public void ToStringFromString(string conn, string expected)
    {
        StringForgeConnectionInfoPSSession connInfo = new(conn);
        string actual = connInfo.ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", "computer", 22, "ssh:computer")]
    [InlineData("user", "computer", 22, "ssh:user@computer")]
    [InlineData("DOMAIN\\user", "computer", 22, "ssh:DOMAIN\\user@computer")]
    [InlineData("user@REALM.COM", "computer", 22, "ssh:user@REALM.COM@computer")]
    [InlineData("", "Foo", 21, "ssh:Foo:21")]
    [InlineData("user", "BAR", 1234, "ssh:user@BAR:1234")]
    [InlineData("", "127.0.0.1", 22, "ssh:127.0.0.1")]
    [InlineData("user", "127.0.0.1", 22, "ssh:user@127.0.0.1")]
    [InlineData("", "192.168.1.1", 2222, "ssh:192.168.1.1:2222")]
    [InlineData("User", "172.56.0.1", 12, "ssh:User@172.56.0.1:12")]
    [InlineData("", "2001:db8::1234:5678", 22, "ssh:[2001:db8::1234:5678]")]
    [InlineData("", "2001:db8::1234:5678", 2222, "ssh:[2001:db8::1234:5678]:2222")]
    [InlineData("user", "2001:db8::1234:5678", 22, "ssh:user@[2001:db8::1234:5678]")]
    [InlineData("user", "2001:db8::1234:5678", 1234, "ssh:user@[2001:db8::1234:5678]:1234")]
    public void ToStringFromSSHConnectionInfo(string user, string host, int port, string expected)
    {
        SSHConnectionInfo sshConnInfo = new(user, host, "", port);

        StringForgeConnectionInfoPSSession connInfo = new(sshConnInfo);
        string actual = connInfo.ToString();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, "foo", 80, "wsman", "wsman:http://foo/wsman")]
    [InlineData(true, "foo", 443, "wsman", "wsman:https://foo/wsman")]
    [InlineData(false, "foo", 5985, "wsman", "wsman:foo")]
    [InlineData(false, "foo", 5985, "PowerShell", "wsman:http://foo:5985/PowerShell")]
    [InlineData(true, "foo", 5986, "wsman", "wsman:https://foo")]
    [InlineData(false, "Host-Name", 12, "other", "wsman:http://host-name:12/other")]
    public void ToStringFromWSManConnectionInfo(
        bool useSsl,
        string computerName,
        int port,
        string appName,
        string expected)
    {
        WSManConnectionInfo wsmanConnInfo = new(useSsl, computerName, port, appName, null, null);

        StringForgeConnectionInfoPSSession connInfo = new(wsmanConnInfo);
        string actual = connInfo.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringFromCustomConnectionInfo()
    {
        const string expected = "CustomRunspaceConnectionInfo:Foo";

        CustomRunspaceConnectionInfo customConnInfo = new("Foo");
        StringForgeConnectionInfoPSSession connInfo = new(customConnInfo);
        string actual = connInfo.ToString();

        Assert.Equal(expected, actual);
    }

    public void Dispose()
    {
        _remoteForgeModule.OnRemove(null!);
        Runspace.DefaultRunspace?.Dispose();
    }
}
