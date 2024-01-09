$importModule = Get-Command -Name Import-Module -Module Microsoft.PowerShell.Core
$moduleName = [System.IO.Path]::GetFileNameWithoutExtension($PSCommandPath)

$isReload = $true
if (-not ('RemoteForge.Shared.LoadContext' -as [type])) {
    $isReload = $false

    Add-Type -Path ([System.IO.Path]::Combine($PSScriptRoot, 'bin', 'net7.0', "$moduleName.Shared.dll"))
}

$mainModule = [RemoteForge.Shared.LoadContext]::Initialize()
$innerMod = &$importModule -Assembly $mainModule -PassThru:$isReload

if ($innerMod) {
    # Bug in pwsh, Import-Module in an assembly will pick up a cached instance
    # and not call the same path to set the nested module's cmdlets to the
    # current module scope. This is only technically needed if someone is
    # calling 'Import-Module -Name ... -Force' a second time. The first import
    # is still fine.
    # https://github.com/PowerShell/PowerShell/issues/20710
    $addExportedCmdlet = [System.Management.Automation.PSModuleInfo].GetMethod(
        'AddExportedCmdlet',
        [System.Reflection.BindingFlags]'Instance, NonPublic'
    )
    foreach ($cmd in $innerMod.ExportedCmdlets.Values) {
        $addExportedCmdlet.Invoke($ExecutionContext.SessionState.Module, @(, $cmd))
    }
}
