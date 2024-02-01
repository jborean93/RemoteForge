. (Join-Path $PSScriptRoot common.ps1)

Describe "RemoteForgeConnectionInfo tests" {
    It "Returns connection string for ToString" {
        $forge = [TestForge.PipeInfo]::Create("fooey")
        $connInfo = [RemoteForge.RemoteForgeConnectionInfo]::new($forge)

        $connInfo.ToString() | Should -Be "PipeTest:fooey"
    }

    It "Gets and sets AuthenticationMechanism" {
        $forge = [TestForge.PipeInfo]::Create("fooey")
        $connInfo = [RemoteForge.RemoteForgeConnectionInfo]::new($forge)

        $connInfo.AuthenticationMechanism | Should -Be Default

        {
            $connInfo.AuthenticationMechanism = "Negotiate"
        } | Should -Throw
    }

    It "Gets and sets CertificateThumbprint" {
        $forge = [TestForge.PipeInfo]::Create("fooey")
        $connInfo = [RemoteForge.RemoteForgeConnectionInfo]::new($forge)

        $connInfo.CertificateThumbprint | Should -BeNullOrEmpty

        {
            $connInfo.CertificateThumbprint = "foo"
        } | Should -Throw
    }
}
