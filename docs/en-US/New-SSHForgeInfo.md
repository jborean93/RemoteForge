---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/New-SSHForgeInfo.md
schema: 2.0.0
---

# New-SSHForgeInfo

## SYNOPSIS
Creates an SSH connection info object.

## SYNTAX

```
New-SSHForgeInfo [-HostName] <String> [-Port <Int32>] [-UserName <String>] [-KeyFilePath <String>]
 [-Subsystem <String>] [-ConnectingTimeout <Int32>] [-Options <Hashtable>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Creates an object that describes how to connect to the target host through SSH.
This object can be provided as part of the `-ConnectionInfo` parameter on [Invoke-Remote](./Invoke-Remote.md), [Enter-Remote](./Enter-Remote.md), and [New-RemoteForgeSession](./New-RemoteForgeSession.md).

It is designed to provide a way to describe an SSH connection that cannot be done through a simple forge connection string.

## EXAMPLES

### Example 1: Create a standard connection object
```powershell
PS C:\> $connInfo = New-SSHForgeInfo -HostName foo
PS C:\> Invoke-Remote $connInfo { whoami }
```

Creates an SSH connection info object that targets `foo` with the default parameters.

### Example 2: Create a connection object for an IPv6 address
```powershell
PS C:\> $connInfo = New-SSHForgeInfo -HostName '[::1]'
PS C:\> $connInfo = New-SSHForgeInfo -HostName '[::1]:2222'
PS C:\> $connInfo = New-SSHForgeInfo -HostName 'user@[::1]'
PS C:\> $connInfo = New-SSHForgeInfo -HostName 'user@[::1]:2222'
```

Various permutations of an SSH connection info structure for an IPv6 address with and without a username/port.

### Example 3: Create a connection object with a domain UPN user
```powershell
PS C:\> $connInfo = New-SSHForgeInfo -HostName user@REALM.COM@foo
```

Creates an SSH connection info object for the user `user@REALM.COM` to the host `foo`.

### Example 4: Create a connection object with custom options
```powershell
PS C:\> $connInfo = New-SSHForgeInfo -HostName foo -Options @{
    StrictHostKeyChecking = 'no'
    UserKnownHostsFile = '/dev/null'
}
```

Creates an SSH connection info object that will run the connection with `-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null` that disables host key checking during the connection.

## PARAMETERS

### -ConnectingTimeout
The time, in milliseconds, after which a connection attempt is terminated.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -HostName
The target hostname to connect to.
If specifying an IPv6 address, enclose the address with `[]`, for example `-HostName '[::1]'`.
An optional username can be prefixed with `username@hostname`, the last `@` in the string is what separates the username from the host.
An optional port can be suffxied with `hostname:22`.

The value specified here is supported by the `ssh` forge, for example `New-SSHForgeInfo -HostName foo:2222` is the same as `ssh:foo:2222` as a forge connection string.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -KeyFilePath
The path to the private key to use for authentication.
This path must exist and be a `FileSystem` path.
If the path does not exist or is not a `FileSystem` path an error will be written.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Options
A hashtable of SSH options used with the SSH connection.
The possible options are values supported by the underlying `ssh` command through the `-o` argument.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Port
The SSH port to connect to, defaults to `22`.
If a port is specified in the `-HostName` string, this parameter will override that value.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
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

### -Subsystem
The SSH subsystem to use, defaults to `powershell`.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: powershell
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserName
The SSH username to use in the connection.
If a username is specified in the `-HostName` string, this parameter will override that value.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

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

### System.Management.Automation.Runspaces.SSHConnectionInfo
This cmdlet outputs the `SSHConnectionInfo` object that describes the SSH connection details. This object can be used as part of the `-ConnectionInfo` parameter when creating a forge transport.

## NOTES

## RELATED LINKS

[SSHConnectionInfo](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.runspaces.sshconnectioninfo)
