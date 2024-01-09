$ErrorActionPreference = 'Stop'

$connInfo = [RemoteForge.Client.PipeInfo]::new()
$session = New-RemoteForgeSession -ConnectionInfo $connInfo
try {
    Invoke-Command -Session $session -ScriptBlock { "Pid from remote: $pid" }
}
finally {
    Write-Host Closing
    $session | Remove-PSSession
}
