---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Enter-Remote.md
schema: 2.0.0
---

# Enter-Remote

## SYNOPSIS
Starts an interactive session with a PSRemoting transport.

## SYNTAX

```
Enter-Remote [-ConnectionInfo] <StringForgeConnectionInfoPSSession>
 [-ApplicationArguments <PSPrimitiveDictionary>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The `Enter-Remote` cmdlet starts an interactive session with a single remote transport.
During the session, the commands typed into host are run on the transport target, just as if it was a local process.
This cmdlet is designed to replicate the functionality of [Enter-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pssession) but with support for forges.

To end the interactive session and disconnect from the remote transport, use the `Exit-PSSession` cmdlet or type `exit`.

## EXAMPLES

### Example 1: Start an interactive session
```powershell
PS C:\> Enter-Remote ssh:hostname
```

Starts an interactive session over `ssh` to `hostname`.

### Example 2: Start session using connection info object
```powershell
PS C:\> $connInfo = New-SSHForgeInfo -HostName foo -UserName user
PS C:\> Enter-Remote $connInfo
```

Creates an `SSHConnectionInfo` object using [New-SSHForgeInfo](./New-SSHForgeInfo.md) and uses it as the transport target.

## PARAMETERS

### -ApplicationArguments
A PrimitiveDictionary that is sent to the remote session.
The value specified here can be accessed through the `$PSSenderInfo.ApplicationArguments` property value.

```yaml
Type: PSPrimitiveDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectionInfo
The `-ConnectionInfo` parameter accepts four different objects types:

+ `String` - forge connection string

+ `IRemoteForge` - forge connection object

+ `RunspaceConnectionInfo` - PowerShell connection info object

+ `PSSession` - an already created PSSession object

See [about_RemoteForgeConnectionInfo](./about_RemoteForgeConnectionInfo.md) for more details.

```yaml
Type: StringForgeConnectionInfoPSSession
Parameter Sets: (All)
Aliases: ComputerName, Cn

Required: True
Position: 0
Default value: None
Accept pipeline input: False
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

### None
This cmdlet does not accept any pipeline input.

## OUTPUTS

### None
This cmdlet does not output any object.

## NOTES
Do not attempt to run this cmdlet in a script, it can only be used interactively.
See [Invoke-Remote](./Invoke-Remote.md) to run a scriptblock in a remote transport session instead.

While the session is interactive it cannot be used to invoke external applications that require interaction through the console.
For example running a command like `vim`, `sudo`, etc that require interaction through the console will not work.
This is a fundamental limiation of PSRemoting and cannot be fixed.

## RELATED LINKS

[Enter-PSSession](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pssession)
