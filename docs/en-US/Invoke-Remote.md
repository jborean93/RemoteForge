---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Invoke-Remote.md
schema: 2.0.0
---

# Invoke-Remote

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

### ScriptBlock (Default)
```
Invoke-Remote [-ConnectionInfo] <StringForgeConnectionInfoPSSession[]> [-ScriptBlock] <String>
 [[-ArgumentList] <ArgumentsOrParameters>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### FilePath
```
Invoke-Remote [-ConnectionInfo] <StringForgeConnectionInfoPSSession[]> [-FilePath] <String>
 [[-ArgumentList] <ArgumentsOrParameters>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -ArgumentList
{{ Fill ArgumentList Description }}

```yaml
Type: ArgumentsOrParameters
Parameter Sets: (All)
Aliases: Args, Param, Parameters

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConnectionInfo
{{ Fill ConnectionInfo Description }}

```yaml
Type: StringForgeConnectionInfoPSSession[]
Parameter Sets: (All)
Aliases: ComputerName, Cn

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FilePath
{{ Fill FilePath Description }}

```yaml
Type: String
Parameter Sets: FilePath
Aliases: PSPath

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
{{ Fill InputObject Description }}

```yaml
Type: PSObject[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

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

### -ScriptBlock
{{ Fill ScriptBlock Description }}

```yaml
Type: String
Parameter Sets: ScriptBlock
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ThrottleLimit
{{ Fill ThrottleLimit Description }}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.PSObject[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
