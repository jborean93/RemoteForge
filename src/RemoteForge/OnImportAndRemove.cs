using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace RemoteForge;

public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    public void OnImport()
    {
        RemoteForgeRegistrations.Register(
            "ssh",
            CreateSshConnectionInfo,
            description: "Builtin SSH transport",
            isDefault: true);
        // winrm
        // hyperv
        // pwsh
        // container
    }

    private static SSHConnectionInfo CreateSshConnectionInfo(Uri info)
    {
        return new(info.UserInfo, info.Host, null, 22);
    }

    public void OnRemove(PSModuleInfo module)
    {}
}
