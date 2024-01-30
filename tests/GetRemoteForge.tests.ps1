. (Join-Path $PSScriptRoot common.ps1)

Describe "Get-RemoteForge tests" {
    It "Gets all registered forges" {
        $actual = Get-RemoteForge

        $actual.Count | Should -BeGreaterOrEqual 2

        $sshReg = $actual | Where-Object Id -EQ ssh
        $sshReg.Description | Should -Be 'Builtin SSH transport'
        $sshReg.IsDefault | Should -BeTrue

        $pipeTestReg = $actual | Where-Object Id -EQ PipeTest
        $pipeTestReg.Description | Should -Be 'Test pipe transport'
        $pipeTestReg.IsDefault | Should -BeFalse
    }

    It "Gets forge with wildcard match" {
        $actual = Get-RemoteForge Fake*, PipeTe*

        $actual.Count | Should -Be 1
        $actual.Id | Should -Be PipeTest
        $actual.Description | Should -Be 'Test pipe transport'
        $actual.IsDefault | Should -BeFalse
    }

    It "Gets no results with no match" {
        $actual = Get-RemoteForge Fake

        $actual | Should -BeNullOrEmpty
    }
}

Describe "Unregister-RemoteForge tests" {
    It "Unregisters transport by name" {
        $ps = [PowerShell]::Create()
        $actual = $ps.AddScript({
                param ($RemoteForge, $TestForge)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge

                Unregister-RemoteForge -Name PipeTest

                Get-RemoteForge
            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
            }).Invoke()

        $actual | Where-Object Id -EQ PipeTest | Should -BeNullOrEmpty
        Get-RemoteForge -Name PipeTest | Should -Not -BeNullOrEmpty
    }

    It "Unregisters transport by piped input" {
        $ps = [PowerShell]::Create()
        $actual = $ps.AddScript({
                param ($RemoteForge, $TestForge)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge

                Get-RemoteForge -Name PipeTest | Unregister-RemoteForge

                Get-RemoteForge
            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
            }).Invoke()

        $actual | Where-Object Id -EQ PipeTest | Should -BeNullOrEmpty
        Get-RemoteForge -Name PipeTest | Should -Not -BeNullOrEmpty
    }

    It "Writes error when unregistering an invalid name" {
        Unregister-RemoteForge -Id Fake -ErrorAction SilentlyContinue -ErrorVariable err

        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "No forge has been registered with the id 'Fake'"
    }
}
