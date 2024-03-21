using RemoteForge;
using System;
using System.Management.Automation.Runspaces;
using Xunit;

namespace RemoteForgeTests;

public class RemoteForgeRegistrationTests : IDisposable
{
    private readonly OnModuleImportAndRemove _remoteForgeModule = new();

    public RemoteForgeRegistrationTests() : base()
    {
        Runspace.DefaultRunspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
        Runspace.DefaultRunspace.Open();
        _remoteForgeModule.OnImport();
    }

    [Fact]
    public void GetRegistrationWithDefaultForge()
    {
        RunspaceConnectionInfo connInfo = RemoteForgeRegistration.CreateForgeConnectionInfo("foo");
        SSHConnectionInfo sshInfo = Assert.IsType<SSHConnectionInfo>(connInfo);
        Assert.Equal("foo", sshInfo.ComputerName);
        Assert.Equal(22, sshInfo.Port);
        Assert.Null(sshInfo.UserName);
        Assert.Empty(sshInfo.KeyFilePath);
    }

    [Theory]
    // Hostname
    [InlineData("foo", "foo", 22, null)]
    [InlineData("Foo", "Foo", 22, null)]
    [InlineData("foo:2222", "foo", 2222, null)]
    [InlineData("user@foo-host", "foo-host", 22, "user")]
    [InlineData("user@foo-host:1234", "foo-host", 1234, "user")]
    [InlineData("DOMAIN\\User@Foo", "Foo", 22, "DOMAIN\\User")]
    [InlineData("user@REALM.COM@foo", "foo", 22, "user@REALM.COM")]
    // IPv4
    [InlineData("127.0.0.1", "127.0.0.1", 22, null)]
    [InlineData("127.0.0.1:2222", "127.0.0.1", 2222, null)]
    [InlineData("user@192.168.1.1", "192.168.1.1", 22, "user")]
    [InlineData("DOMAIN\\user@192.168.1.1", "192.168.1.1", 22, "DOMAIN\\user")]
    [InlineData("user@realm.com@192.168.1.1", "192.168.1.1", 22, "user@realm.com")]
    // IPv6
    [InlineData("[2001:db8::1234:5678]", "2001:db8::1234:5678", 22, null)]
    [InlineData("[2001:DB8::1234:5678]", "2001:DB8::1234:5678", 22, null)]
    [InlineData("[2001:0db8:0001:0000:0000:0ab9:C0A8:0102]", "2001:0db8:0001:0000:0000:0ab9:C0A8:0102", 22, null)]
    [InlineData("[2001:DB8::1234:5678]:2222", "2001:DB8::1234:5678", 2222, null)]
    [InlineData("user@[2001:DB8::1234:5678]:2222", "2001:DB8::1234:5678", 2222, "user")]
    [InlineData("user@REALM@[2001:DB8::1234:5678]", "2001:DB8::1234:5678", 22, "user@REALM")]
    public void GetSSHConnectionInfo(string info, string computerName, int port, string? userName)
    {
        RunspaceConnectionInfo connInfo = RemoteForgeRegistration.CreateForgeConnectionInfo($"ssh:{info}");
        SSHConnectionInfo sshInfo = Assert.IsType<SSHConnectionInfo>(connInfo);
        Assert.Equal(computerName, sshInfo.ComputerName);
        Assert.Equal(port, sshInfo.Port);
        Assert.Equal(userName, sshInfo.UserName);
    }

    [Theory]
    [InlineData("host", "http", "host", 5985, "/wsman")]
    [InlineData("192.168.1.1", "http", "192.168.1.1", 5985, "/wsman")]
    [InlineData("[2001:db8::1234:5678]", "http", "[2001:db8::1234:5678]", 5985, "/wsman")]
    [InlineData("host:80", "http", "host", 80, "/wsman")]
    [InlineData("host:5985", "http", "host", 5985, "/wsman")]
    [InlineData("host:1234", "http", "host", 1234, "/wsman")]
    [InlineData("host:443", "https", "host", 443, "/wsman")]
    [InlineData("host:5986", "https", "host", 5986, "/wsman")]
    [InlineData("outlook.office365.com:443/PowerShell-LiveID/?BasicAuthToOAuthConversion=true", "https", "outlook.office365.com", 443, "/PowerShell-LiveID/?BasicAuthToOAuthConversion=true")]
    [InlineData("https://outlook.office365.com/PowerShell-LiveID/?BasicAuthToOAuthConversion=true", "https", "outlook.office365.com", 443, "/PowerShell-LiveID/?BasicAuthToOAuthConversion=true")]
    [InlineData("http://exchange/PowerShell", "http", "exchange", 80, "/PowerShell")]
    public void GetWSManConnectionInfo(string info, string scheme, string host, int port, string pathAndQuery)
    {
        RunspaceConnectionInfo connInfo = RemoteForgeRegistration.CreateForgeConnectionInfo($"wsman:{info}");
        WSManConnectionInfo wsmanInfo = Assert.IsType<WSManConnectionInfo>(connInfo);
        Assert.Equal(scheme, wsmanInfo.Scheme);
        Assert.Equal(host, wsmanInfo.ComputerName);
        Assert.Equal(port, wsmanInfo.Port);
        Assert.Equal(pathAndQuery, wsmanInfo.ConnectionUri.PathAndQuery);
    }

    [Theory]
    [InlineData("user@realm@host")]
    [InlineData("-foo")]
    public void GetWSManConnectionInfoInvalidHostname(string info)
    {
        string expected = $"WSMan connection string '{info}' must be a valid hostname for use in a URI";

        ArgumentException actual = Assert.Throws<ArgumentException>(
            () => RemoteForgeRegistration.CreateForgeConnectionInfo($"wsman:{info}"));

        Assert.Equal(expected, actual.Message);
    }

    public void Dispose()
    {
        _remoteForgeModule.OnRemove(null!);
        Runspace.DefaultRunspace?.Dispose();
    }
}
