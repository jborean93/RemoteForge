. (Join-Path $PSScriptRoot common.ps1)

Describe "New-SSHForgeInfo" {
    It "Uses defaults" {
        $actual = New-SSHForgeInfo -HostName foo

        $actual.AuthenticationMechanism | Should -Be Default
        $actual.CancelTimeout | Should -Be 60000
        $actual.CertificateThumbprint | Should -BeNullOrEmpty
        $actual.ComputerName | Should -Be foo
        $actual.ConnectingTimeout | Should -Be -1
        $actual.Credential | Should -BeNullOrEmpty
        $actual.Culture | Should -Be ([System.Globalization.CultureInfo]::CurrentCulture)
        $actual.IdleTimeout | Should -Be -1
        $actual.KeyFilePath | Should -BeNullOrEmpty
        $actual.MaxIdleTimeout | Should -Be 2147483647
        $actual.OpenTimeout | Should -Be 180000
        $actual.OperationTimeout | Should -Be 180000
        $actual.Port | Should -Be 22
        $actual.Subsystem | Should -Be powershell
        $actual.UICulture | Should -Be ([System.Globalization.CultureInfo]::CurrentUICulture)
        $actual.UserName | Should -BeNullOrEmpty
    }

    It "Uses -Port" {
        $actual = New-SSHForgeInfo -HostName foo -Port 1234

        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 1234
    }

    It "Uses port in hostname" {
        $actual = New-SSHForgeInfo -HostName foo:1234

        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 1234
    }

    It "Uses port in hostname with cmdlet override" {
        $actual = New-SSHForgeInfo -HostName foo:1234 -Port 5678

        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 5678
    }

    It "Uses -UserName" {
        $actual = New-SSHForgeInfo -HostName Foo -UserName user@REALM.COM

        $actual.ComputerName | Should -Be Foo
        $actual.UserName | Should -Be user@REALM.COM
    }

    It "Uses username in hostname" {
        $actual = New-SSHForgeInfo -HostName user@REALM.COM@Foo

        $actual.ComputerName | Should -Be Foo
        $actual.UserName | Should -Be user@REALM.COM
    }

    It "Uses username in hostname with cmdlet override" {
        $actual = New-SSHForgeInfo -HostName user@REALM.COM@Foo -UserName other

        $actual.ComputerName | Should -Be Foo
        $actual.UserName | Should -Be other
    }

    It "Uses -KeyFilePath" {
        Set-Content TestDrive:/priv_key 'value'

        $actual = New-SSHForgeInfo foo -KeyFilePath TestDrive:/priv_key

        $actual.ComputerName | Should -Be foo
        $actual.KeyFilePath | Should -Be (Get-Item TestDrive:/priv_key).FullName
    }

    It "Uses -KeyFilePath with relative path" {
        $keyDir = New-Item TestDrive:/test-dir -ItemType Directory
        $keyPath = Join-Path $keyDir 'priv_key'
        Set-Content $keyPath 'value'

        Push-Location -Path $keyDir
        try {
            $actual = New-SSHForgeInfo foo -KeyFilePath ./priv_key
        }
        finally {
            Pop-Location
        }

        $actual.ComputerName | Should -Be foo
        $actual.KeyFilePath | Should -Be (Get-Item $keyPath).FullName
    }

    It "Uses -KeyFilePath with missing path" {
        $actual = New-SSHForgeInfo foo -KeyFilePath TestDrive:/invalid -ErrorAction SilentlyContinue -ErrorVariable err

        $actual.ComputerName | Should -Be foo
        $actual.KeyFilePath | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be TestDrive:/invalid
        [string]$err[0] | Should -BeLike "Cannot find KeyFilePath '*invalid' because it does not exist"
    }

    It "Uses -KeyFilePath with invalid provider" {
        $actual = New-SSHForgeInfo foo -KeyFilePath env:/foo -ErrorAction SilentlyContinue -ErrorVariable err

        $actual.ComputerName | Should -Be foo
        $actual.KeyFilePath | Should -BeNullOrEmpty

        $err.Count | Should -Be 1
        $err[0].TargetObject | Should -Be env:/foo
        [string]$err[0] | Should -Be "The resolved KeyFilePath 'foo' is not a FileSystem path but Environment"
    }

    It "Uses -Subsystem" {
        $actual = New-SSHForgeInfo foo -Subsystem WinPowerShell

        $actual.ComputerName | Should -Be foo
        $actual.Subsystem | Should -Be WinPowerShell
    }

    It "Uses -ConnectingTimeout" {
        $actual = New-SSHForgeInfo foo -ConnectingTimeout 1234

        $actual.ComputerName | Should -Be foo
        $actual.ConnectingTimeout | Should -Be 1234
    }

    It "Uses -Options" {
        $actual = New-SSHForgeInfo foo -Options @{
            PubKeyAuth = $true
        }

        # Unfortunately SSHConnectionInfo doesn't expose the options publicly
        # so we cannot test them.
        $actual.ComputerName | Should -Be foo
    }
}

Describe "New-WSManForgeInfo" {
    It "Uses -ComputerName" {
        $actual = New-WSManForgeInfo -ComputerName foo

        $actual.Scheme | Should -Be http
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 5985
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses -ComputerName with -Cn alias" {
        $actual = New-WSManForgeInfo -Cn foo

        $actual.Scheme | Should -Be http
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 5985
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses -ComputerName as positional argument" {
        $actual = New-WSManForgeInfo foo

        $actual.Scheme | Should -Be http
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 5985
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses -UseSSL" {
        $actual = New-WSManForgeInfo -ComputerName foo -UseSSL

        $actual.Scheme | Should -Be https
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 5986
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses explicit port" {
        $actual = New-WSManForgeInfo -ComputerName foo -Port 1234

        $actual.Scheme | Should -Be http
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be 1234
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses explicit HTTPS port <Port>" -TestCases @(@{ Port = 5986 }, @{ Port = 443 }) {
        param ($Port)
        $actual = New-WSManForgeInfo -ComputerName foo -Port $Port

        # You always need -UseSSL to opt into https here
        $actual.Scheme | Should -Be http
        $actual.ComputerName | Should -Be foo
        $actual.Port | Should -Be $Port
        $actual.AppName | Should -Be '/wsman'
    }

    It "Uses -ConnectionUri" {
        $actual = New-WSManForgeInfo -ConnectionUri 'https://outlook.office365.com/PowerShell-LiveID/?BasicAuthToOAuthConversion=true'

        $actual.Scheme | Should -Be https
        $actual.ComputerName | Should -Be outlook.office365.com
        $actual.Port | Should -Be 443
        $actual.AppName | Should -Be '/PowerShell-LiveID/'
        $actual.ConnectionUri | Should -Be 'https://outlook.office365.com/PowerShell-LiveID/?BasicAuthToOAuthConversion=true'
    }

    It "Uses the defaults" {
        $actual = New-WSManForgeInfo foo

        $actual.AppName | Should -Be '/wsman'
        $actual.AuthenticationMechanism | Should -Be Default
        $actual.CancelTimeout | Should -Be 60000
        $actual.CertificateThumbprint | Should -BeNullOrEmpty
        $actual.ComputerName | Should -Be foo
        $actual.ConnectionUri | Should -Be http://foo:5985/wsman
        $actual.Credential | Should -BeNullOrEmpty
        $actual.Culture | Should -Be ([System.Globalization.CultureInfo]::CurrentCulture)
        $actual.EnableNetworkAccess | Should -BeFalse
        $actual.IdleTimeout | Should -Be -1
        $actual.IncludePortSPN | Should -BeFalse
        $actual.MaxConnectionRetryCount | Should -Be 5
        $actual.MaximumConnectionRedirectionCount | Should -Be 5
        $actual.MaximumReceivedDataSizePerCommand | Should -BeNullOrEmpty
        $actual.NoEncryption | Should -BeFalse
        $actual.NoMachineProfile | Should -BeFalse
        $actual.OpenTimeout | Should -Be 180000
        $actual.OperationTimeout | Should -Be 180000
        $actual.OutputBufferingMode | Should -Be None
        $actual.Port | Should -Be 5985
        $actual.ProxyAccessType | Should -Be None
        $actual.ProxyAuthentication | Should -Be Default
        $actual.ProxyCredential | Should -BeNullOrEmpty
        $actual.Scheme | Should -Be http
        $actual.ShellUri | Should -Be "http://schemas.microsoft.com/powershell/Microsoft.PowerShell"
        $actual.SkipCACheck | Should -BeFalse
        $actual.SkipCNCheck | Should -BeFalse
        $actual.SkipRevocationCheck | Should -BeFalse
        $actual.UICulture | Should -Be ([System.Globalization.CultureInfo]::CurrentUICulture)
        $actual.UseCompression | Should -BeTrue
        $actual.UseUTF16 | Should -BeFalse
    }

    It "Uses explicit culture" {
        $so = [System.Management.Automation.Remoting.PSSessionOption]::new()
        $so.Culture = 'en-NZ'
        $so.UICulture = 'en-NZ'

        $actual = New-WSManForgeInfo foo -Sessionoption $so
        $actual.Culture | Should -Be 'en-NZ'
        $actual.UICulture | Should -Be 'en-NZ'
    }

    It "Sets proxy not None" {
        $so = [System.Management.Automation.Remoting.PSSessionOption]::new()
        $so.ProxyAccessType = 'AutoDetect'
        $so.ProxyCredential = [pscredential]::new('user', (ConvertTo-SecureString -AsPlainText -Force pass))

        $actual = New-WSManForgeInfo foo -Sessionoption $so
        $actual.ProxyAccessType | Should -Be AutoDetect
        $actual.ProxyCredential | Should -Not -BeNullOrEmpty
        $actual.ProxyCredential.UserName | Should -Be user
        $actual.ProxyCredential.GetNetworkCredential().Password | Should -Be pass
    }

    It "Sets AppName" {
        $actual = New-WSManForgeInfo -ComputerName foo -ApplicationName PowerShell-LiveID

        $actual.AppName | Should -Be '/PowerShell-LiveID'
        $actual.ConnectionUri | Should -Be http://foo:5985/PowerShell-LiveID
    }

    It "Sets ConfigurationName" {
        $actual = New-WSManForgeInfo foo -ConfigurationName PowerShell.7

        $actual.ShellUri | Should -Be "http://schemas.microsoft.com/powershell/PowerShell.7"
    }

    It "Sets Credential" {
        $cred = [PSCredential]::new('user', (ConvertTo-SecureString -AsPlainText -Force pass))

        $actual = New-WSManForgeInfo foo -Credential $cred
        $actual.Credential.UserName | Should -Be user
        $actual.Credential.GetNetworkCredential().Password | Should -Be pass
    }

    It "Sets CertificateThumbprint" {
        $actual = New-WSManForgeInfo foo -CertificateThumbprint DE3B96F7569822EF2B28837ABE03825DA9CF8F52

        $actual.CertificateThumbprint | Should -Be DE3B96F7569822EF2B28837ABE03825DA9CF8F52
    }
}
