# PowerShell RemoteForge

[![Test workflow](https://github.com/jborean93/RemoteForge/workflows/Test%20RemoteForge/badge.svg)](https://github.com/jborean93/RemoteForge/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/jborean93/RemoteForge/branch/main/graph/badge.svg?token=b51IOhpLfQ)](https://codecov.io/gh/jborean93/RemoteForge)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/RemoteForge.svg)](https://www.powershellgallery.com/packages/RemoteForge)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/jborean93/RemoteForge/blob/main/LICENSE)

PowerShell RemoteForge module used for managing and using custom PSRemoting transports.
By itself this module only exposes the builtin transports `ssh` and `wsman`, it is designed to be used as a dependency of other modules to provide a simpler way of writing transports and a common interface for end users to use those transports.

See [RemoteForge index](docs/en-US/RemoteForge.md) for more details.

## Requirements

These cmdlets have the following requirements

* PowerShell v7.3 or newer

## Builtin Forges

The following forges are provided by this module:

|Name|Description|ConnectionInfo Cmdlet|
|-|-|-|
|`ssh`|The builtin PowerShell SSH transport|[New-SSHForgeInfo](./docs/en-US/New-SSHForgeInfo.md)|
|`wsman`|The builtin PowerShell WSMan/WinRM transport|[New-WSManForgeInfo](./docs/en-US/New-WSManForgeInfo.md)|

The WSMan transport on Linux requires the WSMan C library to be setup properly which is outside the scope of this module.
The ConnectionInfo Cmdlet specified can be used to create a [RunspaceConnectionInfo](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.runspaces.runspaceconnectioninfo?view=powershellsdk-7.3.0) for that transport which can be used in the underlying PowerShell .NET API, provided to [Invoke-Remote](./docs/en-US/Invoke-Remote.md), [Enter-Remote](./docs/en-US/Enter-Remote.md), or [New-RemoteForgeSession](./docs/en-US/New-RemoteForgeSession.md).

## Extra Forges

These forges are not part of this module but can be installed separated and used with the [Invoke-Remote](./docs/en-US/Invoke-Remote.md), [Enter-Remote](./docs/en-US/Enter-Remote.md), and [New-RemoteForgeSession](./docs/en-US/New-RemoteForgeSession.md) cmdlets provided by this module.

|Name|Description|ConnectionInfo Cmdlet|
|-|-|-|

See the documentation for each linked forge module for more information on how to use them.

## Examples

The following cmdlets can be used with a registered forge:

+ [Invoke-Remote](./docs/en-US/Invoke-Remote.md) - replacement for [Invoke-Command](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/invoke-command)
+ [Enter-Remote](./docs/en-US/Enter-Remote.md) - replacement for [Enter-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pssession)
+ [New-RemoteForgeSession](./docs/en-US/New-RemoteForgeSession.md) - replacement for [New-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/new-pssession)

These three cmdlets accept a forge connection through the `-ConnectionInfo` parameter which describe the connection details.
See [about_RemoteForgeConnectionInfo](./docs/en-US/about_RemoteForgeConnectionInfo.md) for more details on how this connection an be specified.

The [Invoke-Remote](./docs/en-US/Invoke-Remote.md) cmdlet can be used to invoke a scriptblock or script file on the target host(s)

```powershell
# Connects over ssh to hostname
Invoke-Remote -ConnectionInfo ssh:hostname {
    Get-Item foo
}

# Connects over wsman to hostname
Invoke-Remote -ConnectionInfo wsman:hostname {
    Get-Item foo
}

# Connects to both hostname over ssh and a custom forge called
# process (not included). This is done in parallel up to the
# defined -ThrottleLimit
Invoke-Remote ssh:hostname, process:foo {
    Get-Item foo
}
```

If the forge name is not used in the connection string, the default forge is used.
The variable `$PSRemoteForgeDefault` can be used to specify the default forge in the current scope, if unset it defaults to `ssh`.

```powershell
# Will run host1, host2, host3 all over wsman in parallel.
$PSRemoteForgeDefault = 'wsman'
Invoke-Remote host1, host2, host3 {
    Get-Item foo
}
```

The [Enter-Remote](./docs/en-US/Enter-Remote.md) cmdlet can only be used in interactive scenarios.
Just like `Enter-PSSession`, it provides an interactive PSHost over the connection specified.
Type in `exit` to exit the remote PSHost and return back to the local host that entered the session.
Do not use this cmdlet in a script as it will not work.

The [New-RemoteForgeSession](./docs/en-US/New-RemoteForgeSession.md) cmdlet can be used to create a [PSSession](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.runspaces.pssession?view=powershellsdk-7.4.0) object.
This is useful for long lived session that run multiple commands with `Invoke-Remote` or when needing to interact with the builtin cmdlets `Invoke-Command`, `Enter-PSSession`, etc.

```powershell
# Creates the PSSession object
$session = New-RemoteForgeSession ssh:foo

# Runs two separate commands on the PSSession using the builtin
# Invoke-Command cmdlet.
Invoke-Command -Session $session { 'foo' }

Invoke-Command -Session $session { 'bar' }

# The session should be closed when no longer needed
$session | Remove-PSSession
```

## Installing

The easiest way to install this module is through [PowerShellGet](https://docs.microsoft.com/en-us/powershell/gallery/overview).

You can install this module by running either of the following `Install-PSResource` or `Install-Module` command.

```powershell
# Install for only the current user
Install-PSResource -Name RemoteForge -Scope CurrentUser
Install-Module -Name RemoteForge -Scope CurrentUser

# Install for all users
Install-PSResource -Name RemoteForge -Scope AllUsers
Install-Module -Name RemoteForge -Scope AllUsers
```

The `Install-PSResource` cmdlet is part of the new `PSResourceGet` module from Microsoft available in newer versions while `Install-Module` is present on older systems.

## Contributing

Contributing is quite easy, fork this repo and submit a pull request with the changes.
To build this module run `.\build.ps1 -Task Build` in PowerShell.
To test a build run `.\build.ps1 -Task Test` in PowerShell.
This script will ensure all dependencies are installed before running the test suite.
