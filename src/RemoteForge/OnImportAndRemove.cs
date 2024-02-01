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
        (string hostname, int port, string? userName) = Commands.NewSSHForgeInfoCommand.ParseSSHInfo(info);
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
