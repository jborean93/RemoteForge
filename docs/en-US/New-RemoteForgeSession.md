---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/New-RemoteForgeSession.md
schema: 2.0.0
---

# New-RemoteForgeSession

## SYNOPSIS
Creates a persistent connection to the PSRemoting transport target.

## SYNTAX

```
New-RemoteForgeSession -ConnectionInfo <StringForgeConnectionInfoPSSession[]>
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The `New-RemoteForgeSession` cmdlet creates a PowerShell session (`PSSession`) to the remote target(s) specified.
A `PSSession` stays alive and connected until it has been closed with [Remove-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/remove-pssession).

The created `PSSession` objects can be used with the [Invoke-Remote](./Invoke-Remote.md) and [Enter-Remote](./Enter-Remote.md) cmdlets provided by this module.
They can also be used by the [Invoke-Command](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/invoke-command) and [Enter-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pssession) cmdlets provided by PowerShell.
This cmdlet is designed to replicate the functionality of [New-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/new-pssession) but with support for forges.

## EXAMPLES

### Example 1: Create an SSH connection
```powershell
PS C:\> $session = New-RemoteForgeSession ssh:hostname
PS C:\> Invoke-Command -Session $session { whoami }
PS C:\> $session | Remove-PSSession
```

This creates a `PSSession` using the `ssh` forge to the host `hostname`.
The session is then provided to `Invoke-Command` to run the `whoami` application before closing the session with `Remove-PSSession`.
It is possible to also use [Invoke-Remote](./Invoke-Remote.md) instead of `Invoke-Command` here.

## PARAMETERS

### -ConnectionInfo
The `-ConnectionInfo` parameter accepts four different objects types:

+ `String` - forge connection string

+ `IRemoteForge` - forge connection object

+ `RunspaceConnectionInfo` - PowerShell connection info object

+ `PSSession` - an already created PSSession object

See [about_RemoteForgeConnectionInfo](./about_RemoteForgeConnectionInfo.md) for more details.

This cmdlet will create a `PSSession` for each connection info specified.

```yaml
Type: StringForgeConnectionInfoPSSession[]
Parameter Sets: (All)
Aliases: ComputerName, Cn

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -ProgressAction
New common parameter introduced in PowerShell 7.4.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### RemoteForge.Commands.StringForgeConnectionInfoPSSession[]
This cmdlet accepts the `-ConnectionInfo` values as input.

## OUTPUTS

### System.Management.Automation.Runspaces.PSSession
This cmdlet outputs a `PSSession` object for each connection provided through `-ConnectionInfo`.

## NOTES
It is important that a session is closed with [Remove-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/remove-pssession) to ensure any remote resources are cleaned up.

## RELATED LINKS

[New-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/new-pssession)
