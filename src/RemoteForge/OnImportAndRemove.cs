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

    private static SSHConnectionInfo CreateSshConnectionInfo(string info)
    {
        Uri sshUri = new($"ssh://{info}");
        return new(sshUri.UserInfo, sshUri.Host, string.Empty, 22);
    }

    public void OnRemove(PSModuleInfo module)
    { }
}
