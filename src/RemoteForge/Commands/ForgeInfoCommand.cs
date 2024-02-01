using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace RemoteForge.Commands;

[Cmdlet(VerbsCommon.New, "SSHForgeInfo")]
[OutputType(typeof(SSHConnectionInfo))]
public sealed class NewSSHForgeInfoCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0
    )]
    public string HostName { get; set; } = string.Empty;

    [Parameter]
    public int Port { get; set; } = -1;

    [Parameter]
    public string? UserName { get; set; }

    [Parameter]
    public string? KeyFilePath { get; set; }

    [Parameter]
    public string Subsystem { get; set; } = "powershell";

    [Parameter]
    public int ConnectingTimeout { get; set; } = Timeout.Infinite;

    [Parameter]
    public Hashtable? Options { get; set; }

    protected override void EndProcessing()
    {
        (string hostname, int port, string? userName) = ParseSSHInfo(HostName);
        SSHConnectionInfo connInfo = new(
            string.IsNullOrWhiteSpace(UserName) ? userName : UserName,
            hostname,
            KeyFilePath,
            Port == -1 ? port : Port,
            Subsystem,
            ConnectingTimeout,
            Options
        );
        WriteObject(connInfo);
    }

    internal static (string, int, string?) ParseSSHInfo(string info)
    {
        // Split out the username portion first to allow UPNs that contain
        // @ as well before the last @ that separates the user from the
        // hostname. This is done because the Uri class will not work if the
        // user contains two '@' chars.
        string? userName = null;
        string hostname;
        int userSplitIdx = info.LastIndexOf('@');
        int hostNameOffset = 0;
        if (userSplitIdx == -1)
        {
            hostname = info;
        }
        else
        {
            hostNameOffset = userSplitIdx + 1;
            userName = info.Substring(0, userSplitIdx);
            hostname = info.Substring(userSplitIdx + 1);
        }

        // While we use the Uri class to validate and inspect the provided host
        // string, it does canonicalise the value so we need to extract the
        // original value used.
        Uri sshUri = new($"ssh://{hostname}");

        int port = sshUri.Port == -1 ? 22 : sshUri.Port;
        if (sshUri.HostNameType == UriHostNameType.IPv6)
        {
            // IPv6 is enclosed with [] and is canonicalised so we need to just
            // extract the value enclosed by [] from the original string for
            // the hostname.
            hostname = info[(1 + hostNameOffset)..info.IndexOf(']')];
        }
        else
        {
            // As the hostname is lower cased we need to extract the original
            // string value.
            int originalHostIndex = sshUri.OriginalString.IndexOf(
                sshUri.Host,
                StringComparison.OrdinalIgnoreCase);
            hostname = originalHostIndex == -1
                ? sshUri.Host
                : sshUri.OriginalString.Substring(originalHostIndex, sshUri.Host.Length);
        }

        return (hostname, port, userName);
    }
}

[Cmdlet(
    VerbsCommon.New,
    "WSManForgeInfo",
    DefaultParameterSetName = "ComputerName")]
[OutputType(typeof(WSManConnectionInfo))]
public sealed class NewWSManForgeInfoCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        ParameterSetName = "ConnectionUri"
    )]
    public Uri? ConnectionUri { get; set; }

    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "ComputerName"
    )]
    [Alias("Cn")]
    public string ComputerName { get; set; } = string.Empty;

    [Parameter(ParameterSetName = "ComputerName")]
    public int Port { get; set; } = -1;

    [Parameter(ParameterSetName = "ComputerName")]
    public SwitchParameter UseSSL { get; set; }

    [Parameter(ParameterSetName = "ComputerName")]
    public string? ApplicationName { get; set; } = "wsman";

    [Parameter]
    public string? ConfigurationName { get; set; } = "Microsoft.PowerShell";

    [Parameter]
    public AuthenticationMechanism Authentication { get; set; } = AuthenticationMechanism.Default;

    [Parameter]
    public PSSessionOption? SessionOption { get; set; }

    [Parameter]
    [Credential]
    public PSCredential? Credential { get; set; }

    [Parameter]
    public string CertificateThumbprint { get; set; } = string.Empty;

    protected override void EndProcessing()
    {
        if (ConnectionUri == null)
        {
            string scheme = UseSSL ? "https" : "http";
            int port = Port == -1
                ? UseSSL ? 5986 : 5985
                : Port;
            UriBuilder uriBuilder = new(scheme, ComputerName, port, $"/{ApplicationName}");
            ConnectionUri = uriBuilder.Uri;
        }

        PSSessionOption so = SessionOption ?? new();
        WSManConnectionInfo connInfo = new(ConnectionUri)
        {
            AuthenticationMechanism = Authentication,
            CancelTimeout = (int)so.CancelTimeout.TotalMilliseconds,
            CertificateThumbprint = CertificateThumbprint,
            Credential = Credential,
            Culture = so.Culture ?? CultureInfo.CurrentCulture,
            IdleTimeout = (int)so.IdleTimeout.TotalMilliseconds,
            IncludePortInSPN = so.IncludePortInSPN,
            MaxConnectionRetryCount = so.MaxConnectionRetryCount,
            MaximumConnectionRedirectionCount = so.MaximumConnectionRedirectionCount,
            MaximumReceivedDataSizePerCommand = so.MaximumReceivedDataSizePerCommand,
            NoEncryption = so.NoEncryption,
            NoMachineProfile = so.NoMachineProfile,
            OpenTimeout = (int)so.OpenTimeout.TotalMilliseconds,
            OperationTimeout = (int)so.OperationTimeout.TotalMilliseconds,
            OutputBufferingMode = so.OutputBufferingMode,
            ProxyAccessType = so.ProxyAccessType,
            ProxyAuthentication = so.ProxyAuthentication,
            SkipCACheck = so.SkipCACheck,
            SkipCNCheck = so.SkipCNCheck,
            SkipRevocationCheck = so.SkipRevocationCheck,
            UICulture = so.UICulture ?? CultureInfo.CurrentUICulture,
            UseCompression = !so.NoCompression,
            UseUTF16 = so.UseUTF16,
        };
        if (so.ProxyAccessType != ProxyAccessType.None)
        {
            connInfo.ProxyCredential = so.ProxyCredential;
        }
        WriteObject(connInfo);
    }
}
