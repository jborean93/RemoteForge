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

    It "Invokes with extra options" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest:foo=bar -ScriptBlock {
            $env:foo
        }

        $actual | Should -Be bar
    }

    It "Invokes with default scheme" {
        $PSRemoteForgeDefault = 'PipeTest'

        $actual = Invoke-Remote '' -ScriptBlock { 'foo' }
        $actual | Should -Be foo
    }

    It "Uses -ConnectionInfo as ComputerName alias" {
        $actual = Invoke-Remote -ComputerName PipeTest: -ScriptBlock {
            "foo"
        }

        $actual | Should -Be foo
    }

    It "Uses -ConnectionInfo as Cn alias" {
        $actual = Invoke-Remote -Cn PipeTest: -ScriptBlock {
            "foo"
        }

        $actual | Should -Be foo
    }

    It "Uses -ConnectionInfo as positional argument" {
        $actual = Invoke-Remote PipeTest: {
            "foo"
        }

        $actual | Should -Be foo
    }

    It "Uses IRemoteForge connection object" {
        $forge = [TestForge.PipeInfo]::Create("")
        Invoke-Remote -ConnectionInfo $forge { 'foo' } | Should -Be foo
    }

    It "Uses RunspaceConnectionInfo connection object" {
        $forge = [TestForge.PipeInfo]::Create("")
        $connInfo = [RemoteForge.RemoteForgeConnectionInfo]::new($forge)
        Invoke-Remote $connInfo { 'foo' } | Should -Be foo
    }

    It "Uses PSSession as connection info" {
        $s = New-RemoteForgeSession -ConnectionInfo PipeTest:
        try {
            Invoke-Remote -ConnectionInfo $s { 'foo' } | Should -Be foo
            Invoke-Remote $s { 'foo' } | Should -Be foo
        }
        finally {
            $s | Remove-PSSession
        }
    }

    It "Writes complex object" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            [PSCustomObject]@{
                Foo = 'Bar'
            }
        }

        $actualProps = $actual.PSObject.Properties.Name | Sort-Object
        $actualProps.Count | Should -Be 4
        'Foo' | Should -BeIn $actualProps
        'PSComputerName' | Should -BeIn $actualProps
        'RunspaceId' | Should -BeIn $actualProps
        'PSShowComputerName' | Should -BeIn $actualProps
        $actual.Foo | Should -Be Bar
    }

    It "Runs script from file path" {
        Set-Content TestDrive:/test.ps1 -Value '"foo"'

        $actual = Invoke-Remote -ConnectionInfo PipeTest: -FilePath TestDrive:/test.ps1
        $actual | Should -Be foo
    }

    It "Runs script from file path relative" {
        Set-Content TestDrive:/test.ps1 -Value '"foo"'

        Push-Location TestDrive:/
        try {
            $actual = Invoke-Remote -ConnectionInfo PipeTest: -FilePath ./test.ps1
            $actual | Should -Be foo
        }
        finally {
            Pop-Location
        }
    }

    It "Passes arguments through -ArgumentList" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, $Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } -ArgumentList 1, 2

        $actual.Foo | Should -Be 1
        $actual.Bar | Should -Be 2
    }

    It "Passes single argument through -ArgumentList" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo)

            $Foo
        } -ArgumentList 1

        $actual | Should -Be 1
    }

    It "Passes null argument through -ArgumentList" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo)

            $null -eq $Foo
        } -ArgumentList $null

        $actual | Should -BeTrue
    }

    It "Passes PSObject argument through -ArgumentList" {
        $obj = "value" | Add-Member -NotePropertyName MyProperty -NotePropertyValue test -PassThru
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo)

            $Foo.MyProperty
        } -ArgumentList $obj

        $actual | Should -Be test
    }

    It "Passes arguments through -ArgumentList Positional" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, $Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } 1, 2

        $actual.Foo | Should -Be 1
        $actual.Bar | Should -Be 2
    }

    It "Passes arguments through -ArgumentList Args alias" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, $Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } -Args 1, 2

        $actual.Foo | Should -Be 1
        $actual.Bar | Should -Be 2
    }

    It "Passes parameters through -ArgumentList" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, [Switch]$Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } -ArgumentList @{ Bar = $true; Foo = 'test' }

        $actual.Foo | Should -Be test
        $actual.Bar | Should -BeTrue
    }

    It "Passes parameters through -ArgumentList as positional parameters" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, [Switch]$Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } @{ Bar = $true; Foo = 'test' }

        $actual.Foo | Should -Be test
        $actual.Bar | Should -BeTrue
    }

    It "Passes parameters through -ArgumentList as Parameters alias" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo, [Switch]$Bar)

            [PSCustomObject]@{
                Foo = $Foo
                Bar = $Bar
            }
        } -Parameters @{ Bar = $true; Foo = 'test' }

        $actual.Foo | Should -Be test
        $actual.Bar | Should -BeTrue
    }

    It "Passes parameters through -ArgumentList with PSObject value" {
        $obj = "value" | Add-Member -NotePropertyName MyProperty -NotePropertyValue test -PassThru
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo)

            $Foo.MyProperty
        } @{ Foo = $obj }

        $actual | Should -Be test
    }

    It "Passes parameters through -ArgumentList with null value" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            param ($Foo)

            $null -eq $Foo
        } @{ Foo = $null }

        $actual | Should -BeTrue
    }

    It "Fails when file path does not exist" {
        {
            Invoke-Remote -ConnectionInfo PipeTest: -FilePath TestDrive:/fake.ps1
        } | Should -Throw "Cannot find path '*fake.ps1' because it does not exist"
    }

    It "Fails when file path points to provider that isn't a FileSystem" {
        {
            Invoke-Remote -ConnectionInfo PipeTest: -FilePath env:/test
        } | Should -Throw "The resolved path 'test' is not a FileSystem path but Environment"
    }

    It "Fails with unknown forge registration name" {
        $actual = Invoke-Remote InvalidForge: { 'foo' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        [string]$err[0] | Should -Be "No valid forge registration found with the name 'InvalidForge'"
    }

    It "Writes information records" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            Write-Host host-test

            $obj = [PSCustomObject]@{
                Foo = 'bar'
            }
            Write-Information $obj
        } 6>&1

        $actual.Count | Should -Be 2
        $actual[0].MessageData | Should -Be host-test
        $actual[0].Source | Should -Be Write-Host
        $actual[0].Tags.Count | Should -Be 2
        $actual[0].Tags | Should -Contain 'PSHOST'
        $actual[0].Tags | Should -Contain 'FORWARDED'

        $actual[1].MessageData.Foo | Should -Be bar
        $actual[1].Source | Should -Be Write-Information
        $actual[1].Tags.Count | Should -Be 0
    }

    It "Accepts pipeline input" {
        $actual = 1..5 |
            ForEach-Object { $_; Start-Sleep -Milliseconds 500 } |
            Invoke-Remote PipeTest: {
                param (
                    [Parameter(ValueFromPipeline)]
                    [int[]]
                    $InputObject
                )

                process {
                    foreach ($obj in $InputObject) {
                        "Test: $obj"
                    }
                }
            }

        $actual.Count | Should -Be 5
        $actual[0] | Should -Be 'Test: 1'
        $actual[1] | Should -Be 'Test: 2'
        $actual[2] | Should -Be 'Test: 3'
        $actual[3] | Should -Be 'Test: 4'
        $actual[4] | Should -Be 'Test: 5'
    }

    It "Stops running pipeline with transport end" {
        $testFile = (New-Item -Path TestDrive:/test.txt -Value foo -Force).FullName
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript({
                param($RemoteForge, $TestForge, $TestFile)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge

                Invoke-Remote PipeTest: {
                    $pid
                    Start-Sleep -Seconds 20
                }
            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
                TestFile = $testFile
            })

        $out = [System.Management.Automation.PSDataCollection[PSObject]]::new()
        $null = $ps.BeginInvoke([System.Management.Automation.PSDataCollection[PSObject]]::new(), $out)

        $attempt = 0
        while ($out.Count -eq 0) {
            $attempt += 1
            if ($attempt -eq 25) {
                throw "Timed out will waiting for test output"
            }
            Start-Sleep -Milliseconds 200
        }

        $remotePid = $out[0]
        $ps.Stop()
        Wait-Process -Id $remotePid -Timeout 5 -ErrorAction SilentlyContinue
        if (Get-Process -Id $remotePid -ErrorAction SilentlyContinue) {
            throw "Expected remote process to end on stop signal"
        }
    }

    It "Throttles concurrent sessions" {
        $testDir = Join-Path TestDrive:/ ThrottleTests
        $testPath = (New-Item -Path $testDir -ItemType Directory -Force).FullName

        $ps = [PowerShell]::Create()
        $null = $ps.AddScript({
                param($RemoteForge, $TestForge, $TestPath)

                $ErrorActionPreference = 'Stop'

                Import-Module -Name $RemoteForge
                Import-Module -Name $TestForge

                Invoke-Remote @(
                    'PipeTest:'
                    'PipeTest:'
                    'PipeTest:'
                    'PipeTest:'
                ) -ScriptBlock {
                    $fileName = "$($using:TestPath)/$pid"
                    [Console]::WriteLine("Writing to '$fileName'")
                    Set-Content -LiteralPath $fileName -Value ''
                    $pid

                    $start = Get-Date
                    while (Test-Path -LiteralPath $fileName) {
                        $time = (Get-Date) - $start
                        if ($time.TotalSeconds -gt 60) {
                            throw "Timed out waiting for file to be deleted"
                        }

                        Start-Sleep -Milliseconds 200
                    }
                } -ThrottleLimit 2

            }).AddParameters(@{
                RemoteForge = (Get-Module -Name RemoteForge).Path
                TestForge = (Get-Module -Name TestForge).Path
                TestPath = $testPath
            })

        $out = [System.Management.Automation.PSDataCollection[PSObject]]::new()
        $eventId = (New-Guid).Guid
        Register-ObjectEvent -InputObject $out -EventName DataAdded -SourceIdentifier $eventId
        try {
            $task = $ps.BeginInvoke([System.Management.Automation.PSDataCollection[PSObject]]::new(), $out)

            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 30
            if (-not $dataAddedEvent) {
                throw "Timed out waiting for pid info for first connection"
            }
            $dataAddedEvent | Remove-Event
            $pid1 = $out[$dataAddedEvent.SourceEventArgs.Index]

            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 30
            if (-not $dataAddedEvent) {
                throw "Timed out waiting for pid info for second connection"
            }
            $dataAddedEvent | Remove-Event
            $pid2 = $out[$dataAddedEvent.SourceEventArgs.Index]

            # -ThrottleLimit 2 limits the concurrent sessions to just 2
            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 1
            $dataAddedEvent | Should -BeNullOrEmpty

            # Deleting the first pid file will stop the first connection and
            # queue up the 3rd one.
            Remove-Item -LiteralPath (Join-Path $testPath $pid1) -Force

            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 30
            if (-not $dataAddedEvent) {
                throw "Timed out waiting for pid info for second connection"
            }
            $dataAddedEvent | Remove-Event
            $pid3 = $out[$dataAddedEvent.SourceEventArgs.Index]

            # The fourth should still not have started yet
            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 1
            $dataAddedEvent | Should -BeNullOrEmpty

            Remove-Item -LiteralPath (Join-Path $testPath $pid2) -Force

            # After removing the above task pid the last connection will start.
            $dataAddedEvent = Wait-Event -SourceIdentifier $eventId -Timeout 30
            if (-not $dataAddedEvent) {
                throw "Timed out waiting for pid info for second connection"
            }
            $dataAddedEvent | Remove-Event
            $pid4 = $out[$dataAddedEvent.SourceEventArgs.Index]

            Remove-Item -LiteralPath (Join-Path $testPath $pid3) -Force
            Remove-Item -LiteralPath (Join-Path $testPath $pid4) -Force

            $ps.EndInvoke($task)
        }
        finally {
            Unregister-Event -SourceIdentifier $eventId
        }
    }

    It "Handles error with invalid script" {
        $n = [Environment]::NewLine
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock '$a $b' -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTest:'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTest:': At line:1 char:4${n}+ `$a `$b${n}+    ~~${n}Unexpected token '`$b' in expression or statement."
    }

    It "Writes errors from remote script" {
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            Write-Error -Message "Error 1"
            'foo'
            Write-Error -Message "Error 2"
        } -ErrorAction SilentlyContinue -ErrorVariable err

        $actual | Should -Be foo
        $err.Count | Should -Be 2
        $err[0].TargetObject | Should -Be 'PipeTest:'
        [string]$err[0] | Should -Be "Error 1"
        $err[0].Exception.ErrorRecord.ScriptStackTrace | Should -Be 'at <ScriptBlock>, <No file>: line 2'

        $err[1].TargetObject | Should -Be 'PipeTest:'
        [string]$err[1] | Should -Be "Error 2"
        $err[1].Exception.ErrorRecord.ScriptStackTrace | Should -Be 'at <ScriptBlock>, <No file>: line 4'
    }

    It "Handles error when create failed" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?failoncreate=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?failoncreate=true'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTestWithOptions:?failoncreate=true': Failed to create connection"
    }

    It "Handles error when close failed" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?failonclose=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -Be test

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?failonclose=true'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTestWithOptions:?failonclose=true': Failed to close connection"
    }

    It "Handles error when read failed" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?failonread=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?failonread=true'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTestWithOptions:?failonread=true': Failed to read message"
    }

    It "Handles error when read end" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?endonread=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?endonread=true'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTestWithOptions:?endonread=true': Transport has returned no data before it has been closed"
    }

    It "Handles error when write failed" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?failonwrite=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?failonwrite=true'
        [string]$err[0] | Should -Be "Failed to run script on 'PipeTestWithOptions:?failonwrite=true': Failed to write message"
    }

    It "Handles error from process stderr" {
        $actual = Invoke-Remote -ConnectionInfo PipeTestWithOptions:?writestderr=true -ScriptBlock { 'test' } -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be 'PipeTestWithOptions:?writestderr=true'
        [string]$err[0] | Should -BeLike "Failed to run script on 'PipeTestWithOptions:?writestderr=true': The argument 'FakeArg' is not recognized *"
    }
}

Describe 'Invoke-Remote $using: tests' {
    It "Passes along simple var" {
        $var = 'foo'
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var
        }

        $actual | Should -Be foo
    }

    It "Passes along simple var with different case" {
        $vaR = 'foo'
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:Var
        }

        $actual | Should -Be foo
    }

    It "Passes along complex var" {
        $var = [PSCustomObject]@{
            Foo = 1
            Bar = 'string'
        }
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $res = $using:var
            $res.Foo.GetType().FullName
            $res.Foo
            $res.Bar.GetType().FullName
            $res.Bar
        }

        $actual.Count | Should -Be 4
        $actual[0] | Should -Be System.Int32
        $actual[1] | Should -Be 1
        $actual[2] | Should -Be System.String
        $actual[3] | Should -Be string
    }

    It "Passes ScriptBlock" {
        # Pwsh serializes scriptblocks as strings
        $var = { 'scriptblock' }
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $res = $using:var
            $res.GetType().FullName
            $res
        }

        $actual.Count | Should -Be 2
        $actual[0] | Should -Be System.String
        $actual[1] | Should -Be " 'scriptblock' "
    }

    It "Passes with index lookup as integer" {
        $var = @('foo', 'bar')
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var[1]
        }

        $actual | Should -Be bar
    }

    It "Passes with index lookup as single quoted string" {
        $var = @{
            foo = 1
            bar = 2
        }
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var['bar']
        }

        $actual | Should -Be 2
    }

    It "Passes with index lookup as double quoted string" {
        $var = @{
            foo = 1
            bar = 2
        }
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var["bar"]
        }

        $actual | Should -Be 2
    }

    It "Passes with index lookup with constant variable" {
        $var = @('foo', 'bar')
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var[$true]
        }

        $actual | Should -Be bar
    }

    It "Uses null for invalid index" {
        $var = @('foo')
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var[1]
        }

        $actual | Should -BeNullOrEmpty
    }

    It "Passes with index and member lookup" {
        $var = @{
            'Prop With Space' = @(
                @{ Foo = 'value 1' }
                @{ Foo = 'value 2' }
            )
        }
        $actual = Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock {
            $using:var.'Prop With Space'[0]['Foo']
            $using:var["Prop With Space"][1].Foo
        }

        $actual.Count | Should -Be 2
        $actual[0] | Should -Be 'value 1'
        $actual[1] | Should -Be 'value 2'
    }

    It "Fails if variable not defined locally" {
        {
            Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock { $using:UndefinedVar }
        } | Should -Throw 'The value of the using variable ''$using:UndefinedVar'' cannot be retrieved because it has not been set in the local session.'
    }

    It "Fails if variable with index lookup not defined locally" {
        {
            Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock { $using:UndefinedVar["index"] }
        } | Should -Throw 'The value of the using variable ''$using:UndefinedVar`["index"`]'' cannot be retrieved because it has not been set in the local session.'
    }

    It "Warns when attempting to pass with index lookup with variable index" {
        # This isn't considered a failure as PowerShell won't allow you to build
        # a scriptblock with that expression. It is only kept here in case we
        # are providing a string for the server to intepret specially.
        $var = @('foo', 'bar')
        Invoke-Remote -ConnectionInfo PipeTest: -ScriptBlock '$idx = 0; $using:var[$idx]' -WarningAction SilentlyContinue -WarningVariable warn -ErrorAction Ignore

        $warn.Count | Should -Be 1
        $warn[0].Message | Should -Be 'Failed to extract $using value: Index operation failed; the array index evaluated to null.'
    }
}
