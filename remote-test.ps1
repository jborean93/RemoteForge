Import-Module $PSScriptRoot/output/RemoteForge
Import-Module -Name "$PSScriptRoot/tests/TestForge/bin/Release/net7.0/TestForge.dll"
Invoke-Remote $args { 'foo' }
