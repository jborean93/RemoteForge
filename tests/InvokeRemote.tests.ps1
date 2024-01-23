. (Join-Path $PSScriptRoot common.ps1)

Describe "Invoke-Remote tests" {
    It "Invokes command with custom transport" {
        $actual = Invoke-Remote -ComputerName PipeTest: -ScriptBlock {
            "string value"
        }

        $actual | Should -Be 'string value'
        $actual | Should -BeOfType ([string])
        $actualProps = $actual.PSObject.Properties.Name | Sort-Object
        $actualProps.Count | Should -Be 4
        'Length' | Should -BeIn $actualProps
        'PSComputerName' | Should -BeIn $actualProps
        'RunspaceId' | Should -BeIn $actualProps
        'PSShowComputerName' | Should -BeIn $actualProps
        $actual.PSComputerName | Should -Be PipeTest:
        $actual.PSShowComputerName | Should -BeTrue
    }
}
