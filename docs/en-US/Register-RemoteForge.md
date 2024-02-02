---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Register-RemoteForge.md
schema: 2.0.0
---

# Register-RemoteForge

## SYNOPSIS
Registers a remote forge.

## SYNTAX

### Explicit (Default)
```
Register-RemoteForge -Name <String> [-ForgeFactory] <ScriptBlock> [-Description <String>] [-PassThru] [-Force]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Assembly
```
Register-RemoteForge [-Assembly] <Assembly> [-PassThru] [-Force] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
This cmdlet can be used to register a remote forge.
The forge details can either can gathered through a provided dotnet assembly object or through manual parameters.
It is recommended for forge implementation modules to use the `-Assembly` parameter as that will scan the assembly and automatically load any classes that implement the `IRemoteForge` interface.

A forge name is case insensitive, for example `MyTest` is the same as `mytest`.
A forge name must be unique, while it is not advised, the `-Force` parameter can be used to overwrite an existing registration.

This cmdlet can also be used to provide a custom user provided forge string that can produce any `IRemoteForge` or `RunspaceConnectionInfo` from a custom string format.

The registrations are scoped to the current PowerShell session, new sessions in another Runspace will not share the same forge registrations.

## EXAMPLES

### Example 1: Register all forges found in a module assembly
```powershell
PS C:\> Register-RemoteForge -Assembly ([MyTypeInModule].Assembly)
```

Registers all the `IRemoteForge` implementations found in the `MyTypeInModule` assembly.
In this scenario `MyTypeInModule` is an assembly loaded by a specific module.

### Example 2: Create custom forge factory
```powershell
PS C:\> Register-RemoteForge -Name UntrustedSSH -ForgeFactory {
    param ([string]$Info)

    New-SSHForgeInfo -HostName $Info -Options @{
        StrictHostKeyChecking = 'no'
        UserKnownHostsFile = '/dev/null'
    }
}
PS C:\> Invoke-Remote UntrustedSSH:foo, UntrustedSSH:bar:2222 { whoami }
```

Registers a forge called `UntrustedSSH` that is based on the `ssh` forge.
This forge will automatically add in the options `-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null` for every new connection created under that forge name.

## PARAMETERS

### -Assembly
The dotnet assembly to scan for forge implementations.
Any type defined in the assembly that inherits the `RemoteForge.IRemoteForge` interface will be registered in the current session.

```yaml
Type: Assembly
Parameter Sets: Assembly
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
The description of the forge to register.

```yaml
Type: String
Parameter Sets: Explicit
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Overwrite an existing forge registration if there is one with the same name.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ForgeFactory
A scriptblock that is run when a connection specified with the forge name that was registered.
This should either output an `IRemoteForge` implementation from a custom transport or a `RunspaceConnctionInfo` object that describes the connection.
Use this parameter to easily define aliases for transports of existing forges with a common set of options.

The scriptblock is provided with the string argument that is the `-ConnectionInfo` value without the forge name.
It should output either the `IRemoteForge` object or a `RunspaceConnectionInfo` object.
For example `Invoke-Remote MyForge:hostname,123` will call this factory with the value `hostname,123`.
It is up to the forge implementation to parse the string provided in whatever manner they see fit.

```yaml
Type: ScriptBlock
Parameter Sets: Explicit
Aliases: Factory

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The forge name to register the factory scriptblock with.

```yaml
Type: String
Parameter Sets: Explicit
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Output each registered forge object.
For `-Assembly` there can be many forge registrations depending on what was found in the assembly provided.

```yaml
Type: SwitchParameter
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
This cmdlet does not accept any pipeline input.

## OUTPUTS

### None
No objects are outputted by default.

### RemoteForge.RemoteForgeRegistration
The remote forge registration details are outputted if `-PassThru` was specified.

## NOTES

## RELATED LINKS
