BeforeAll {
    Import-Module "$PSScriptRoot\..\src\Andacious.Ripstation.psd1" -Force
}

Describe 'Backup-Title' {
    It 'Has required TitleId parameter' {
        $cmd = Get-Command Backup-Title
        $cmd.Parameters['TitleId'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] } |
            ForEach-Object { $_.Mandatory | Should -BeTrue }
    }

    It 'Has required OutputPath parameter' {
        $cmd = Get-Command Backup-Title
        $cmd.Parameters['OutputPath'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] } |
            ForEach-Object { $_.Mandatory | Should -BeTrue }
    }

    It 'Has optional DiskNumber parameter' {
        $cmd = Get-Command Backup-Title
        $cmd.Parameters.ContainsKey('DiskNumber') | Should -BeTrue
    }

    It 'Has Progress switch parameter' {
        $cmd = Get-Command Backup-Title
        $cmd.Parameters['Progress'].SwitchParameter | Should -BeTrue
    }

    It 'Supports -WhatIf' {
        $cmd = Get-Command Backup-Title
        $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
    }
}

Describe 'Get-DiskAndTitleInfo' {
    It 'Has DiskNumber parameter' {
        $cmd = Get-Command Get-DiskAndTitleInfo
        $cmd.Parameters.ContainsKey('DiskNumber') | Should -BeTrue
    }

    It 'Has Progress switch parameter' {
        $cmd = Get-Command Get-DiskAndTitleInfo
        $cmd.Parameters['Progress'].SwitchParameter | Should -BeTrue
    }
}
