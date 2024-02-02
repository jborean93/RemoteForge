---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Get-RemoteForge.md
schema: 2.0.0
---

# Get-RemoteForge

## SYNOPSIS
Gets the registered forges.

## SYNTAX

```
Get-RemoteForge [[-Name] <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets the forges that have been registered in the current session.
This includes details such as the forge name and description if provided during registration.

Forges can be registered with [Register-RemoteForge](./Register-RemoteForge.md) and unregistered with [Unregister-RemoteForge](./Unregister-RemoteForge.md).

## EXAMPLES

### Example 1: Get all registered forges
```powershell
PS C:\> Get-RemoteForge
```

Gets all registered forges.

### Example 2: Get specific forge
```powershell
PS C:\> Get-RemoteForge -Name ssh
```

Gets the details of the `ssh` forge.

### Example 3: Get forge with wildcard
```powershell
PS C:\> Get-RemoteForge -Name ws*
```

Gets all forges that match the wildcard `ws*`.

## PARAMETERS

### -Name
The name of the forge to filter by.
This supports simple wildcard matching with `*`, `?`, `[`, and `]`.
Omit this parameter to get all registered forges.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
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

### RemoteForge.RemoteForgeRegistration
This cmdlet outputs a `RemoteForgeRegistration` object for each registered forge. This object contains the `Name` property being the forge name and a `Description` of the forge.

## NOTES

## RELATED LINKS
