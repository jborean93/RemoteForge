using namespace System.IO
using namespace System.Runtime.InteropServices

#Requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Build', 'Test')]
    [string]
    $Task = 'Build',

    [Parameter()]
    [Version]
    $PowerShellVersion = $PSVersionTable.PSVersion,

    [Parameter()]
    [Architecture]
    $PowerShellArch = [RuntimeInformation]::ProcessArchitecture,

    [Parameter()]
    [string]
    $ModuleNupkg
)

$ErrorActionPreference = 'Stop'

. ([Path]::Combine($PSScriptRoot, "tools", "common.ps1"))

$manifestPath = ([Path]::Combine($PSScriptRoot, 'manifest.psd1'))
$Manifest = [Manifest]::new($Configuration, $PowerShellVersion, $PowerShellArch, $manifestPath)

if ($ModuleNupkg) {
    Write-Host "Expanding module nupkg to '$($Manifest.ReleasePath)'" -ForegroundColor Cyan
    Expand-Nupkg -Path $ModuleNupkg -DestinationPath $Manifest.ReleasePath
}

Write-Host "Installing PowerShell dependencies" -ForegroundColor Cyan
$deps = $Task -eq 'Build' ? $Manifest.BuildRequirements : $Manifest.TestRequirements
$deps | Install-BuildDependencies

# This is a special step to setup test dependencies
Write-Host "Compiling test Forge module" -ForegroundColor Cyan

$projectRoot = [Path]::Combine($PSScriptRoot, 'tests', 'TestForge')
$buildArgs = @(
    'publish'
    '--framework', 'net7.0'
    '--configuration', 'Release'
    "-p:Version=$($Manifest.Module.Version)"
    [Path]::Combine($projectRoot, 'TestForge.csproj')
)
dotnet @buildArgs
if ($LASTEXITCODE) {
    throw "Failed to compile TestForge for testing"
}

$buildScript = [Path]::Combine($PSScriptRoot, "tools", "InvokeBuild.ps1")
$invokeBuildSplat = @{
    Task = $Task
    File = $buildScript
    Manifest = $manifest
}
Invoke-Build @invokeBuildSplat
