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

### ScriptBlockArgList (Default)
```
Invoke-Remote [-ComputerName] <StringForgeConnectionInfoPSSession[]> -ScriptBlock <ScriptBlock>
 [-ArgumentList <PSObject[]>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### ScriptBlockParam
```
Invoke-Remote [-ComputerName] <StringForgeConnectionInfoPSSession[]> -ScriptBlock <ScriptBlock>
 [-ParamSplat <IDictionary>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### FilePathArgList
```
Invoke-Remote [-ComputerName] <StringForgeConnectionInfoPSSession[]> -FilePath <String>
 [-ArgumentList <PSObject[]>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### FilePathParam
```
Invoke-Remote [-ComputerName] <StringForgeConnectionInfoPSSession[]> -FilePath <String>
 [-ParamSplat <IDictionary>] [-InputObject <PSObject[]>] [-ThrottleLimit <Int32>]
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
Type: PSObject[]
Parameter Sets: ScriptBlockArgList, FilePathArgList
Aliases: Args

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ComputerName
{{ Fill ComputerName Description }}

```yaml
Type: StringForgeConnectionInfoPSSession[]
Parameter Sets: (All)
Aliases: Cn, Connection

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
Parameter Sets: FilePathArgList, FilePathParam
Aliases: PSPath

Required: True
Position: Named
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

### -ParamSplat
{{ Fill ParamSplat Description }}

```yaml
Type: IDictionary
Parameter Sets: ScriptBlockParam, FilePathParam
Aliases: Params

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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
Type: ScriptBlock
Parameter Sets: ScriptBlockArgList, ScriptBlockParam
Aliases:

Required: True
Position: Named
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

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
