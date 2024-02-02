---
external help file: RemoteForge.dll-Help.xml
Module Name: RemoteForge
online version: https://www.github.com/jborean93/RemoteForge/blob/main/docs/en-US/Invoke-Remote.md
schema: 2.0.0
---

# Invoke-Remote

## SYNOPSIS
Runs commands on a PSRemoting transport.

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
The `Invoke-Remote` cmdlet runs commands on a PSRemoting transport and returns all output from the commands, including errors.
It can be used to run the same script on a single host or on multiple transports in parallel.
This cmdlet is designed to replicate the functionality of [Invoke-Command](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/invoke-command) but with support for forges.

## EXAMPLES

### Example 1: Invokes a command to an SSH target
```powershell
PS C:\> Invoke-Remote -ConnectionInfo ssh:target { whoami }
```

Runs the `whoami` command on `target` through the builtin SSH transport.

### Example 2: Invoke a command on multiple targets
```powershell
PS C:\> Invoke-Remote host1, host2, wsman:host3 { whoami }
```

Runs the `whoami` command on `host1`, `host2`, `host3` at the same time.
The hosts `host1` and `host2` will use the default forge transport `ssh` (or as defined by `$PSRemoteForgeDefault`) while `host3` will use the `wsman` forge.

### Example 3: Pass in arguments as parameters
```powershell
PS C:\> Invoke-Remote host1 -ScriptBlock {
    param ($Param1, $Param2)

    "Final value $Param1 - $Param2"
} -ArgumentList @{Param1 = 'foo'; Param2 = 'bar'}
```

Runs the script specified and passes in the parameters from the hashtable.
Each parameter will be matched up to the parameter name in the scriptblock.
For switch parameters, either omit the switch value in the hashtable provided or use a boolean value with `$true` being the switch is set and `$false` being unset.

### Example 4: Pass in arguments as positional values
```powershell
PS C:\> Invoke-Remote host1 -ScriptBlock {
    param ($Param1, $Param2)

    "Final value $Param1 - $Param2"
} -ArgumentList @('foo', 'bar')
```

Runs the script specified and passes in the parameters through positional arguments.

### Example 5: Pass in variables with $using:
```powershell
PS C:\> $var = 'value'
PS C:\> $arrayVar = @('foo', 'bar')
PS C:\> Invoke-Remote host1 -ScriptBlock {
    $var = $using:var  # value

    # Passes in the whole array value
    $arrayVar = $using:arrayVar

    # Passes in the value of the index specified
    $arrayVarValue = $using:arrayVar[1]  # 'bar'

    # Passes in the whole array with a more complex lookup
    $idx = 1
    ($using:arrayVar)[$idx]
}
```

Runs the script specified but brings in the values for `$var` and `$arrayVar[1]` respectively.
The first example passes in the value of `$var` as is.
The second example `$using:arrayVar` will pass in the whole array
The third example only passes in the value of `'bar'`.
The fourth example passes in the whole array and does the index lookup on the remote side.
Indexing a `$using:var` only supports numeric constants, constant vars `$true`, `$false`, and simple strings.
For more complex index lookups, like a calculated variable value, composed string, use the fourth example `($using:arrayVar)[$idx]`.

## PARAMETERS

### -ArgumentList
Supplies the arguments or parameters to invoke with the `-ScriptBlock` or `-FilePath` provided.
The value can either be an array/list of values that are passed positionally to the provided script.
It can also be a hashtable/dictionary of values that are passed as parameters like a splat.

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
The `-ConnectionInfo` parameter accepts four different objects types:

+ `String` - forge connection string

+ `IRemoteForge` - forge connection object

+ `RunspaceConnectionInfo` - PowerShell connection info object

+ `PSSession` - an already created PSSession object

See [about_RemoteForgeConnectionInfo](./about_RemoteForgeConnectionInfo.md) for more details.

This cmdlet will invoke the command on each connection info value specified.
The commands are run in parallel up to the `-ThrottleLimit` value specified.

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
Specifies a local script that this cmdlet runs on the target connections.
The script must exist on the local computer and not the target connection.
Use `-ArgumentList` to specify arguments or parameters to invoke with this script.

When this parameter is used, PowerShell reads the contents of the specified script file to a script block, transmits the script block to the target, and runs it.

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
Specifies input to the command.
The input can be read by the target `-ScriptBlock` or `-FilePath` script throught the `$input` variable or as a `param()` parameter that defines a parameter from input.

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

### -ScriptBlock
Specifies the commands to run.
Enclose the commands in braces (`{}`) to validate the script is a valid PowerShell script.

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
Specifies the maximum number of concurrent connections that can be established to run at the same time using this cmdlet.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 32
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.PSObject[]
This cmdlet pipes any input to the target remote command.

## OUTPUTS

### System.Object
This cmdlet outputs any objects from the target session. Each object is annotated with the note properties `PSComputerName`, `RunspaceId`, and `PSShowComputerName`.

## NOTES
Each script invocation is run in a separate process to the caller of this cmdlet.
This has two implications; 1. the input and output objects are serialized causing them to become readonly, and 2. it does not have access to the variables of the current session.
A serialized object is essentially a read only copy of the object, the parameters have a value at the time of serialization but methods cannot be run on the object aside from a few specific types.
To provide variables from the current session either use `-ArgumentList` or using `$using:var` inside the `-ScriptBlock`.

## RELATED LINKS

[Invoke-Command](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/invoke-command)
