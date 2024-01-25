Import-Module $PSScriptRoot/output/RemoteForge
Import-Module -Name "$PSScriptRoot/tests/TestForge/bin/Release/net7.0/TestForge.dll"
# Invoke-Remote $args { 'foo' }

$a = @(1, 2, 3)
$b = @{foo = 'test'}
$c = 'foo'
$aIdx = 0
# $s = New-RemoteForgeSession PipeTest:?logmessages=true
$s = New-RemoteForgeSession PipeTest:
Invoke-Remote PipeTest: {
# Invoke-Command -Session $s -ScriptBlock {
    $aIdx = 'o'
    $using:b["Foo"]
    $using:c
}

$s | Remove-PSSession
