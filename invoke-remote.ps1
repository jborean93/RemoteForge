[CmdletBinding()]
param ()
# $ErrorActionPreference = 'Stop'

# Import-Module -Name "$PSScriptRoot/output/RemoteForge"
Import-Module -Name "$PSScriptRoot/tests/TestForge/bin/Release/net7.0/TestForge.dll"
# Get-RemoteForge

# Trace-Command -PSHost -Name CRSessionFSM -Expression {
#     # Invoke-Remote -ComputerName pipe://, ssh://vagrant-domain@server2022.domain.test -ScriptBlock {
#     Invoke-Remote -ComputerName pipe:// -ScriptBlock {
#         $user = [Environment]::UserName
#         $hostname = [System.Net.Dns]::GetHostName()

#         "Running PID $pid under User '$user' on Host '$hostname'"
#     }
# }

# $ErrorActionPreference = 'Stop'

Invoke-Remote -ComputerName PipeTest:// -ScriptBlock {
    # $user = [Environment]::UserName
    # $hostname = [System.Net.Dns]::GetHostName()
    # Write-Error -Message error -ErrorAction Stop
    throw "exception"

    # "Running PID $pid under User '$user' on Host '$hostname'"
}

'foo'

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
