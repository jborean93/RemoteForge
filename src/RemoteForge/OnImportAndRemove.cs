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
            description: "Builtin SSH transport");
        RemoteForgeRegistration.Register(
            "wsman",
            CreateWSManConnectionInfo,
            description: "Builtin WSMan/WinRM transport");
    }

    public void OnRemove(PSModuleInfo module)
    {
        RemoteForgeRegistration.Registrations.Clear();
    }

    private static SSHConnectionInfo CreateSshConnectionInfo(string info)
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

        return new(userName, hostname, string.Empty, port);
    }

    public static WSManConnectionInfo CreateWSManConnectionInfo(string info)
    {
        UriCreationOptions co = new();
        if (Uri.TryCreate(info, co, out Uri? infoUri) && (infoUri.Scheme == "http" || infoUri.Scheme == "https"))
        {
            return new(infoUri);
        }
        else if (Uri.TryCreate($"custom://{info}", co, out infoUri) &&
            Uri.CheckHostName(infoUri.DnsSafeHost) != UriHostNameType.Unknown)
        {
            string scheme = infoUri.Port == 443 || infoUri.Port == 5986
                ? "https"
                : "http";
            int port = infoUri.Port == -1
                ? 5985
                : infoUri.Port;
            string path = infoUri.PathAndQuery == "/"
                ? "/wsman"
                : infoUri.AbsolutePath;

            UriBuilder builder = new(scheme, infoUri.DnsSafeHost, port, path)
            {
                Query = infoUri.Query,
            };

            return new(builder.Uri);
        }
        else
        {
            throw new ArgumentException($"WSMan connection string '{info}' must be a valid hostname for use in a URI");
        }
    }
}
