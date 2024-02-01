. (Join-Path $PSScriptRoot common.ps1)

Describe "New-RemoteForgeSession tests" {
    It "Creates forge with string" {
        $s = New-RemoteForgeSession -ConnectionInfo pipetest:
        try {
            $s | Should -BeOfType ([System.Management.Automation.Runspaces.PSSession])
            $s.Runspace.OriginalConnectionInfo.ToString() | Should -Be PipeTest:
        }
        finally {
            $s | Remove-PSSession
        }
    }

    It "Creates forge with IRemoteForge" {
        $forge = [TestForge.PipeInfo]::Create("")
        $s = New-RemoteForgeSession -ConnectionInfo $forge
        try {
            $s | Should -BeOfType ([System.Management.Automation.Runspaces.PSSession])
            $s.Runspace.OriginalConnectionInfo.ToString() | Should -Be PipeTest:
        }
        finally {
            $s | Remove-PSSession
        }
    }

    It "Creates forge with connection info" {
        $forge = [TestForge.PipeInfo]::Create("")
        $connInfo = [RemoteForge.RemoteForgeConnectionInfo]::new($forge)
        $s = New-RemoteForgeSession -ConnectionInfo $connInfo
        try {
            $s | Should -BeOfType ([System.Management.Automation.Runspaces.PSSession])
            $s.Runspace.OriginalConnectionInfo.ToString() | Should -Be PipeTest:
        }
        finally {
            $s | Remove-PSSession
        }
    }

    It "Creates forge with PSSession" {
        $s = New-RemoteForgeSession -ConnectionInfo PipeTest:
        try {
            $actual = New-RemoteForgeSession $s
            $actual | Should -Be $s
        }
        finally {
            $s | Remove-PSSession
        }
    }

    It "Cancels hanging runspace creation" {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript({
                param($RemoteForge, $TestForge, $TestPath)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge

                New-RemoteForgeSession PipeTest:?hang=true
            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
            })

        $task = $ps.BeginInvoke()

        Start-Sleep -Seconds 1

        $ps.Stop()

        try {
            $ps.EndInvoke($task)
        }
        catch {}
    }

    It "Handles error when create failed" {
        $actual = New-RemoteForgeSession PipeTest:?failoncreate=true -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTest:?failoncreate=true'
        [string]$err[0] | Should -Be "Failed to open runspace for 'PipeTest:?failoncreate=true': Failed to create connection"
    }

    It "Handles error when read failed" {
        $actual = New-RemoteForgeSession PipeTest:?failonread=true -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTest:?failonread=true'
        [string]$err[0] | Should -Be "Failed to open runspace for 'PipeTest:?failonread=true': Failed to read message"
    }

    It "Handles error when read end" {
        $actual = New-RemoteForgeSession PipeTest:?EndOnRead=true -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTest:?EndOnRead=true'
        [string]$err[0] | Should -Be "Failed to open runspace for 'PipeTest:?EndOnRead=true': Transport has returned no data before it has been closed"
    }

    It "Handles error when write failed" {
        $actual = New-RemoteForgeSession pipetest:?failonwrite=true -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'pipetest:?failonwrite=true'
        [string]$err[0] | Should -Be "Failed to open runspace for 'pipetest:?failonwrite=true': Failed to write message"
    }
}
