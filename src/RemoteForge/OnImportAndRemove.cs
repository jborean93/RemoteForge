using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RemoteForge;

public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    public void OnImport()
    {
        RemoteForgeRegistration.Register(
            "ssh",
            CreateSshConnectionInfo,
            description: "Builtin SSH transport",
            isDefault: true);
        // winrm
        // hyperv
        // pwsh
        // container
    }

    public void OnRemove(PSModuleInfo module)
    { }

    private static SSHConnectionInfo CreateSshConnectionInfo(string info)
    {
        // Split out the username portion first to allow UPNs that contain
        // @ as well before the last @ that separates the user from the
        // hostname. This is done because the Uri class will not work if the
        // user contains two '@' chars.
        string? userName = null;
        string hostname;
        int userSplitIdx = info.LastIndexOf('@');
        if (userSplitIdx == -1)
        {
            hostname = info;
        }
        else
        {
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
            hostname = info[1..(info.IndexOf(']') - 1)];
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

        return new(userName, hostname, string.Empty, port);
    }
}
