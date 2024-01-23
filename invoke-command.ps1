[CmdletBinding()]
param ()
# $ErrorActionPreference = 'Stop'

$s1 = New-PSSession -UseWindowsPowerShell
$s2 = New-PSSession -UseWindowsPowerShell
# try {
Invoke-Command -Session $s1 -ScriptBlock {
    $VerbosePreference = 'Continue'
    $WarningPreference = 'Continue'
    $DebugPreference = 'Continue'
    $InformationPreference = 'Continue'
    $ProgressPreference = 'Continue'

    # Write-Error -Message error -ErrorAction Stop
    throw "exception"
    # Write-Verbose -Message verbose
    # Write-Warning -Message warning
    # Write-Debug -Message debug
    # Write-Information -MessageData information
    # Write-Progress -Activity progress
}
# "IsNull: $($null -eq $a)"
# $a

'foo'
# }
# finally {
$s1 | Remove-PSSession
$s2 | Remove-PSSession
# }
