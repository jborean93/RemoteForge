# Remote Forge Connection Info
## about_RemoteForgeConnectionInfo

# SHORT DESCRIPTION
Describes how a forge connection info value is parsed and used in this module.

# LONG DESCRIPTION
There are four ways a connection can be specified as part of the `-ConnectionInfo` parameter:

+ A forge connection string
+ A `IRemoteForge` object
+ A `RunspaceConnectionInfo` object
+ A `PSSession` object

The first 3 will create the connection and clean it up once finished, while the `PSSession` is just used as provided without closing at the end.

Each forge implementation should provide a cmdlet that can generate a `IRemoteForge` or `RunspaceConnectionInfo` object specific to that transport.
For example this module provides the [New-SSHForgeInfo](./New-SSHForgeInfo.md) and [New-WSManForgeInfo](./New-WSManForgeInfo.md) cmdlets to create a `RunspaceConnectionInfo` object for the builtin `ssh` and `wsman` forges respectively.

The forge connection string is a simple string in the format `forgeName:connectionDetails` where the `forgeName` associates the connection details to a registered forge.
If the `forgeName` is not specified then the default forge set by the variable `$PSRemoteForgeDefault` is used with a final fallback to `ssh` if that variable is undefined.
Some examples of a forge connection string are:

+ `hostname` - Uses the `$PSRemoteForgeDefault` forge (defaults to `ssh`)
+ `ssh:hostname` - Uses the `ssh` forge
+ `wsman:hostname` - Uses the `wsman` forge

The `connectionDetails` value is dependent on the forge it is used with.
This typically is the target hostname but it could be interpreted in any way and each forge implementation should describe how this string is parsed.

The [Register-RemoteForge](./Register-RemoteForge.md) cmdlet can also be used to register a custom forge factory that can interpret the `connectionDetails` string in any way desired.
The following example shows how the forge `UntrustedSSH` can be registered to automatically create an SSH connection with specific options.
The value is just interpreted as a normal SSH hostname.

```powershell
Register-RemoteForge -Name UntrustedSSH -ForgeFactory {
    param ([string]$Info)

    New-SSHForgeInfo -HostName $Info -Options @{
        StrictHostKeyChecking = 'no'
        UserKnownHostsFile = '/dev/null'
    }
}
Invoke-Remote UntrustedSSH:foo, UntrustedSSH:bar:2222 { whoami }
```
