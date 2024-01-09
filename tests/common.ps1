using namespace System.IO
using namespace System.Management.Automation
using namespace System.Management.Automation.Runspaces
using namespace System.Security.Principal

$moduleName = (Get-Item ([IO.Path]::Combine($PSScriptRoot, '..', 'module', '*.psd1'))).BaseName
$global:ModuleManifest = [IO.Path]::Combine($PSScriptRoot, '..', 'output', $moduleName)

if (-not (Get-Module -Name $moduleName -ErrorAction SilentlyContinue)) {
    Import-Module $ModuleManifest
}

if (-not (Get-Variable IsWindows -ErrorAction SilentlyContinue)) {
    # Running WinPS so guaranteed to be Windows.
    Set-Variable -Name IsWindows -Value $true -Scope Global
}

$global:IsAdmin = ([WindowsPrincipal][WindowsIdentity]::GetCurrent()).IsInRole([WindowsBuiltInRole]::Administrator)

Function Global:Complete {
    [OutputType([System.Management.Automation.CompletionResult])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]
        $Expression
    )

    [System.Management.Automation.CommandCompletion]::CompleteInput(
        $Expression,
        $Expression.Length,
        $null).CompletionMatches
}
