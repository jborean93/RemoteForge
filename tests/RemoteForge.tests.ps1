. (Join-Path $PSScriptRoot common.ps1)

Describe "Get-RemoteForge tests" {
    It "Gets all registered forges" {
        $actual = Get-RemoteForge

        $actual.Count | Should -BeGreaterOrEqual 2

        $sshReg = $actual | Where-Object Name -EQ ssh
        $sshReg.Description | Should -Be 'Builtin SSH transport'

        $pipeTestReg = $actual | Where-Object Name -EQ PipeTest
        $pipeTestReg.Description | Should -Be 'Test pipe transport'
    }

    It "Gets forge with wildcard match" {
        $actual = Get-RemoteForge Fake*, PipeTe*

        $actual.Count | Should -Be 2
        $actual.Name | Should -Contain 'PipeTest'
        $actual.Name | Should -Contain 'PipeTestWithOptions'
        $actual.Description | Should -Contain 'Test pipe transport'
    }

    It "Gets no results with no match" {
        $actual = Get-RemoteForge Fake

        $actual | Should -BeNullOrEmpty
    }
}

Describe "Register-RemoteForge tests" {
    It "Registers by assembly" {
        $ps = [PowerShell]::Create()
        $actual = $ps.AddScript({
                param ($RemoteForge, $TestForge)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge
                Unregister-RemoteForge -Name PipeTest

                Register-RemoteForge -Assembly ([TestForge.PipeInfo].Assembly)

                Get-RemoteForge
            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
            }).Invoke()

        $pipeTest = $actual | Where-Object Name -EQ PipeTest
        $pipeTest | Should -Not -BeNullOrEmpty
        $pipeTest.Name | Should -Be PipeTest
        $pipeTest.Description | Should -Be 'Test pipe transport'
    }

    It "Registers by assembly if already registered" {
        # It is already registered so this tests it is idempotent with the same details
        Register-RemoteForge -Assembly ([TestForge.PipeInfo].Assembly)

        $actual = Get-RemoteForge
        $pipeTest = $actual | Where-Object Name -EQ PipeTest
        $pipeTest | Should -Not -BeNullOrEmpty
        $pipeTest.Name | Should -Be PipeTest
        $pipeTest.Description | Should -Be 'Test pipe transport'
    }

    It "Registers by assembly with PassThru" {
        $actual = Register-RemoteForge -Assembly ([TestForge.PipeInfo].Assembly) -PassThru

        $actual.Count | Should -Be 2
        $actual.Name | Should -Contain 'PipeTest'
        $actual.Name | Should -Contain 'PipeTestWithOptions'
        $actual.Description | Should -Contain 'Test pipe transport'
    }

    It "Registers custom factory that returns RunspaceConnectionInfo" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            [RemoteForge.RemoteForgeConnectionInfo]::new([TestForge.PipeInfo]::Create($Info))
        }

        try {
            $forge = Get-RemoteForge -Name TestFactory
            $forge.Count | Should -Be 1
            $forge.Name | Should -Be TestFactory
            $forge.Description | Should -Be 'My Desc'

            $actual = Invoke-Remote TestFactory: { $pid }
            $actual | Should -Not -Be $pid
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
    }

    It "Registers custom factory that returns RunspaceConnectionInfo with -PassThru" {
        $forge = Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            [RemoteForge.RemoteForgeConnectionInfo]::new([TestForge.PipeInfo]::Create($Info))
        } -PassThru

        try {
            $actual = Invoke-Remote TestFactory: { $pid }
            $actual | Should -Not -Be $pid
        }
        finally {
            $forge | Unregister-RemoteForge
        }
    }

    It "Registers custom factory that returns IRemoteForge" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            [TestForge.PipeInfo]::Create($Info)
        }

        try {
            $forge = Get-RemoteForge -Name TestFactory
            $forge.Count | Should -Be 1
            $forge.Name | Should -Be TestFactory
            $forge.Description | Should -Be 'My Desc'

            $actual = Invoke-Remote TestFactory: { $pid }
            $actual | Should -Not -Be $pid
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
    }

    It "Registers custom factory that fails due to not returning the correct value" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            $null
            1
            $Info
        }

        try {
            $forge = Get-RemoteForge -Name TestFactory
            $forge.Count | Should -Be 1
            $forge.Name | Should -Be TestFactory
            $forge.Description | Should -Be 'My Desc'

            $actual = Invoke-Remote TestFactory: { $pid } -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -BeNullOrEmpty
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Factory result for 'TestFactory:' did not output a RunspaceConnectionInfo or IRemoteForge object"

            $err = $null
            $actual = New-RemoteForgeSession TestFactory: -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -BeNullOrEmpty
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Factory result for 'TestFactory:' did not output a RunspaceConnectionInfo or IRemoteForge object"
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
    }

    It "Uses failed factory and good connection info" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            $null
            1
            $Info
        }

        try {
            $forge = Get-RemoteForge -Name TestFactory
            $forge.Count | Should -Be 1
            $forge.Name | Should -Be TestFactory
            $forge.Description | Should -Be 'My Desc'

            $actual = Invoke-Remote TestFactory:, PipeTest: { 'foo' } -ErrorAction SilentlyContinue -ErrorVariable err
            $actual.Count | Should -Be 1
            $actual | Should -Be foo
            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "Factory result for 'TestFactory:' did not output a RunspaceConnectionInfo or IRemoteForge object"
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
    }

    It "Fails with name already registered" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            [TestForge.PipeInfo]::Create($Info)
        }

        try {
            Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
                param($Info)

                "foo"
            } -ErrorAction SilentlyContinue -ErrorVariable err

            $err.Count | Should -Be 1
            [string]$err[0] | Should -Be "A forge with the name 'TestFactory' has already been registered"
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
    }

    It "Overwrites already registered configuration with Force" {
        Register-RemoteForge -Name TestFactory -Description 'My Desc' -ForgeFactory {
            param($Info)

            [TestForge.PipeInfo]::Create($Info)
        }

        try {
            Register-RemoteForge -Name TestFactory -Description 'My Desc 2' -ForgeFactory {
                param($Info)

                "foo"
            } -Force

            $forge = Get-RemoteForge -Name TestFactory
            $forge.Count | Should -Be 1
            $forge.Name | Should -Be TestFactory
            $forge.Description | Should -Be 'My Desc 2'
        }
        finally {
            Unregister-RemoteForge -Name TestFactory
        }
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

        $actual | Where-Object Name -EQ PipeTest | Should -BeNullOrEmpty
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

        $actual | Where-Object Name -EQ PipeTest | Should -BeNullOrEmpty
        Get-RemoteForge -Name PipeTest | Should -Not -BeNullOrEmpty
    }

    It "Writes error when unregistering an invalid name" {
        Unregister-RemoteForge Fake -ErrorAction SilentlyContinue -ErrorVariable err

        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "No forge has been registered with the name 'Fake'"
    }
}
