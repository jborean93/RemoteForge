---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Unregister-RemoteForge.md
schema: 2.0.0
---

# Unregister-RemoteForge

## SYNOPSIS
Unregisters a remote forge.

## SYNTAX

```
Unregister-RemoteForge -Name <String[]> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Unregisters a forge registration.
Once unregistered the forge can only be added back with [Register-RemoteForge](./Register-RemoteForge.md).

## EXAMPLES

### Example 1: Unregisters a custom forge call MyForge
```powershell
PS C:\> Unregister-RemoteForge -Name MyForge
```

Unregisters the forge `MyForge.`

## PARAMETERS

### -Name
The name of the forge to unregister.
This value is case insensitive.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
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

## RELATED LINKS
