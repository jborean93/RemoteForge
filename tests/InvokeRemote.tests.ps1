. (Join-Path $PSScriptRoot common.ps1)

Describe "Invoke-Remote tests" {
    It "Invokes command with custom transport" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
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

    It "Handles error when create failed" {
        {
            Invoke-Remote -ConnectionInfo PipeTest:?failoncreate=true -ScriptBlock {'test'}
        } | Should -Throw "*. (Failed to create connection)"
    }

    It "Handles error when close failed" {
        # FIXME: This is wrapped in another exception, handle better
        {
            Invoke-Remote -ConnectionInfo PipeTest:?failonclose=true -ScriptBlock {'test'}
        } | Should -Throw "*. (Failed to close connection))"
    }

    It "Handles error when read failed" {
        {
            Invoke-Remote -ConnectionInfo PipeTest:?failonread=true -ScriptBlock {'test'}
        } | Should -Throw "*. (Failed to read message)"
    }

    It "Handles error when write failed" {
        {
            Invoke-Remote -ConnectionInfo PipeTest:?failonwrite=true -ScriptBlock {'test'}
        } | Should -Throw "*. (Failed to write message)"
    }
}
