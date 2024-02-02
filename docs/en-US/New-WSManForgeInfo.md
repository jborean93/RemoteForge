---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/New-WSManForgeInfo.md
schema: 2.0.0
---

# New-WSManForgeInfo

## SYNOPSIS
Creates a WSMan connection info object.

## SYNTAX

### ComputerName (Default)
```
New-WSManForgeInfo [-ComputerName] <String> [-Port <Int32>] [-UseSSL] [-ApplicationName <String>]
 [-ConfigurationName <String>] [-Authentication <AuthenticationMechanism>] [-SessionOption <PSSessionOption>]
 [-Credential <PSCredential>] [-CertificateThumbprint <String>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### ConnectionUri
```
New-WSManForgeInfo -ConnectionUri <Uri> [-ConfigurationName <String>]
 [-Authentication <AuthenticationMechanism>] [-SessionOption <PSSessionOption>] [-Credential <PSCredential>]
 [-CertificateThumbprint <String>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Creates an object that describes how to connect to the target host through WSMan/WinRM.
This object can be provided as part of the `-ConnectionInfo` parameter on [Invoke-Remote](./Invoke-Remote.md), [Enter-Remote](./Enter-Remote.md), and [New-RemoteForgeSession](./New-RemoteForgeSession.md).

It is designed to provide a way to describe a WSMan connection that cannot be done through a simple forge connection string.

## EXAMPLES

### Example 1: Create basic WSMan connection info
```powershell
PS C:\> $connInfo = New-WSManForgeInfo -ComputerName foo
```

Creates a WSMan connection info that targets the host `foo`.
This will connect over the URI `http://foo:5985/wsman`.

### Example 2: Create WSMan connection info for PowerShell 7 configuration
```powershell
PS C:\> $connInfo = New-WSManForgeInfo -ComputerName foo -ConfigurationName PowerShell.7
```

Creates a WSMan connection info that targets the host `foo` but connects to the `PowerShell.7` configuration.
This configuration is the standard configuration registered for PowerShell 7+ on the target server if installed.

### Example 3: Connect to exchange endpoint
```powershell
PS C:\> $connParams = @{
    ConnectionUri = 'http://exchange-host/PowerShell'
    ConfigurationName = 'Microsoft.Exchange'
    Authentication = 'Kerberos'
}
PS C:\> $connInfo = New-WSManForgeInfo @connParams
```

Creates a WSMan connection info to an Exchange host using the Exchange connection settings.

## PARAMETERS

### -ApplicationName
Specifies the path/application name segment of the WSMan connection URI, default is `wsman`.

```yaml
Type: String
Parameter Sets: ComputerName
Aliases:

Required: False
Position: Named
Default value: wsman
Accept pipeline input: False
Accept wildcard characters: False
```

### -Authentication
Specifies the mechanism that is used to authenticate the user's credentials, the default value is `Default`.

```yaml
Type: AuthenticationMechanism
Parameter Sets: (All)
Aliases:
Accepted values: Default, Basic, Negotiate, NegotiateWithImplicitCredential, Credssp, Digest, Kerberos

Required: False
Position: Named
Default value: Default
Accept pipeline input: False
Accept wildcard characters: False
```

### -CertificateThumbprint
Specifies the digital public key certificate thumbprint of a user account that has permission to connect to the WSMan server.
Certificates are used in client certificate-based authentication.
They can be mapped only to local user accounts and do not work with domain account.

To get a certificate thumbprint use `Get-Item Cert:\CurrentUser\My\*` or `Get-Item Cert:\LocalMachine\My*`.

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

### -ComputerName
The target computer the connection is for.

```yaml
Type: String
Parameter Sets: ComputerName
Aliases: Cn

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConfigurationName
Specifies the session configuration that is used for the target connection.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Microsoft.PowerShell
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectionUri
Specifies the full connection URI string for the WSMan target.

```yaml
Type: Uri
Parameter Sets: ConnectionUri
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Specifies the credentials to use for the WSMan connection.

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Port
The port to use for the connection, defaults to `5985` or `5986` if `-UseSSL` is specified.

```yaml
Type: Int32
Parameter Sets: ComputerName
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

### -SessionOption
Custom session options for WSMan that can be created through the [New-PSSessionOption](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/new-pssessionoption) cmdlet.

```yaml
Type: PSSessionOption
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseSSL
Connects over HTTPS and not HTTP.

```yaml
Type: SwitchParameter
Parameter Sets: ComputerName
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

### System.Management.Automation.Runspaces.WSManConnectionInfo
This cmdlet outputs the `WSManConnectionInfo` object that describes the WSMan connection details. This object can be used as part of the `-ConnectionInfo` parameter when creating a forge transport.

## NOTES

## RELATED LINKS

[WSManConnectionInfo](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.runspaces.wsmanconnectioninfo)
