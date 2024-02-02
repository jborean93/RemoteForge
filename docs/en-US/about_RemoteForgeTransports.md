# Remote Forge Transports
## about_RemoteForgeTransports

# SHORT DESCRIPTION
Describes how to implement a custom remote forge transport that can be used by this module.
See [TestForge](https://github.com/jborean93/RemoteForge/tree/main/tests/TestForge) for a basic implementation of an `IRemoteForge` and `IRemoteForgeTransport`.

# LONG DESCRIPTION
Implementing a custom forge transport requires three things

+ An implementation of [IRemoteForge](https://github.com/jborean93/RemoteForge/blob/main/src/RemoteForge/IRemoteForge.cs)
+ An implementation of [IRemoteForgeTransport](https://github.com/jborean93/RemoteForge/blob/main/src/RemoteForge/IRemoteForgeTransport.cs)
+ Registration of the forge

The [IRemoteForge](https://github.com/jborean93/RemoteForge/blob/main/src/RemoteForge/IRemoteForge.cs) type is a simple implementation that provides the structure needed to generate the forge transport.
An implementation must define the `ForgeName`, `Create`, and `CreateTransport()` methods.
For example:

```csharp
using RemoteForge;

namespace MyForge;

public sealed class MyForgeInfo : IRemoteForge
{
    // This is the name the forge will be registered under
    public static string ForgeName => "MyForge";

    // Optional description associated with the registration
    public static string ForgeDescription => "Description here";

    public static IRemoteForge Create(string info)
    {
        // Create an instance of this object from the string provided
        return MyForgeInfo(...);
    }

    public IRemoteForgeTransport CreateTransport()
    {
        // Called when a new session is being opened, it should return
        // the IRemoteForgeTransport instance defined later on.
        return new MyForgeTransport(...);
    }
}
```

In this example `Invoke-Remote MyForge:foo` will call the `Create` method with the value `foo`.
It is up to this implementation to parse the string provided and construct the `MyForgeInfo` object containing the connection details.
When the session is being created, the `CreateTransport()` method will be called that provides the `IRemoteForgeTransport` object used by the actual connection.
This method should use any class properties stored on the object that defines the transport itself.

The [IRemoteForgeTransport](https://github.com/jborean93/RemoteForge/blob/main/src/RemoteForge/IRemoteForgeTransport.cs) implementation must define the following four methods:

```csharp
using RemoteForge;
using System.Threading;
using System.Threading.Tasks;

namespace MyForge;

public sealed class MyForgeTransport : IRemoteForgeTransport
{
    public async Task CreateConnection(CancellationToken cancelToken)
    {
        // Creates the connection to the target
    }

    public async Task CloseConnection(CancellationToken cancelToken)
    {
        // Closes the connection to the target
    }

    public async Task WriteMessage(string message, CancellationToken cancelToken)
    {
        // Writes the PSRemoting message to the target
    }

    public async Task<string?> WaitMessage(CancellationToken cancelToken)
    {
        // Wait until a PSRemoting message has been received from the target
        // and returns the message. This is called repeatedly until it returns
        // "", null, or the connection is closed.
    }
}
```

Any exceptions raised on the above methods will mark the transport as broken and the connection will be closed as best as it can.

The class can also optionally implement `IDisposable` and the remote forge transport mechanism will call `Dispose()` once it is finished with the transport.

The implementation has been designed around the async Task model in C# as it is quite effective with blocking IO operations that remote transports typically use.
The cancellation token provided to each method will be set when the transport gets into a bad state and the caller needs to close the transport.

Once implemented a module can register the forge in one of two ways:

+ Using `OnModuleImportAndRemove`

A binary module can implement this interface which means PowerShell will automatically run the `OnImport` and `OnRemove` methods when importing/removing the module in the session.
This provides it the easiest opportunity to register the forge(s) in an assembly.
For example:

```csharp
using System.Management.Automation;
using RemoteForge;

namespace MyForge;

public class OnModuleImportAndRemove : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    public void OnImport()
    {
        RemoteForgeRegistration.Register(typeof(MyForgeInfo).Assembly);
    }

    public void OnRemove(PSModuleInfo module)
    {
        RemoteForgeRegistration.Unregister(MyForgeInfo.ForgeName);
    }
}

```

+ Using [Register-RemoteForge](./Register-RemoteForge.md) in the `.psm1`.

An alternative is to use a module `.psm1` and call `Register-RemoteForge` as it is loaded.
For example:

```powershell
# MyForge.psm1

# The path may differ based on your binary module setup.
# Add-Type -Path can also be used if the dll does not contain cmdlets to load
Import-Module -Name $PSScriptRoot/bin/net7.0/MyForge.dll

Register-RemoteForge -Assembly ([MyForgeInfo].Assembly)
```

A benefit of this approach is it makes registering a forge optional, for example when running a certain PowerShell version or only when certain dependencies are met.

# Implementation Guidelines
A module that implements a custom forge should try and follow these guidelines.

+ Provides a function/cmdlet that can build the `IRemoteForge` implementation

This is beneficial as it provides a user a more object focused interface for creating custom connection info objects that may be hard to express as a string.
It also provides better documentation that describes the connection options and how they can be used.

+ Documents their forge connection string processing rules

As `-ConnectionInfo` can be provided as a string, e.g. `MyForge:foo`, each implementation should describe how it interprets the connection value, in this case `foo`.

+ Uses a unique `ForgeName`

Try not to use a forge name that could conflict with others, using the module name is a nice way to be unique.

+ Adds the `RemoteForge` tag in their module's `psd1`

By adding the `RemoteForge` tag in the module's `psd1` file, users can easily find `RemoteForge` implementations.
