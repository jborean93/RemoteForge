using System.Management.Automation;
using RemoteForge;

namespace TestForge;

public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    public void OnImport()
    {
        RemoteForgeRegistration.Register(typeof(OnModuleImportAndRemove).Assembly);
    }

    public void OnRemove(PSModuleInfo module)
    { }
}
