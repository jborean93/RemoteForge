$ErrorActionPreference = 'Stop'

if (-not (Get-RemoteForge -Name pipe)) {
    Register-RemoteForge -Assembly ([RemoteForge.Client.PipeInfo].Assembly)
}

Invoke-Remote -ComputerName pipe://, ssh://vagrant-domain@server2022.domain.test -ScriptBlock {
    $user = [Environment]::UserName
    $hostname = [System.Net.Dns]::GetHostName()

    "Running PID $pid under User '$user' on Host '$hostname'"
}

# $session = New-RemoteForgeSession -ComputerName pipe://, ssh://vagrant-domain@server2022.domain.test
# try {
#     Invoke-Command -Session $session -ScriptBlock {
#         $user = [Environment]::UserName
#         $hostname = [System.Net.Dns]::GetHostName()

#         "Running PID $pid under User '$user' on Host '$hostname'"
#     }
# }
# finally {
#     $session | Remove-PSSession
# }
