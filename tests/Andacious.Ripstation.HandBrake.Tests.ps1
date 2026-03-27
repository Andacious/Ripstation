BeforeAll {
    Import-Module "$PSScriptRoot\..\src\Andacious.Ripstation.psd1" -Force
}

Describe 'Convert-Video' {
    It 'Has required InputFile parameter' {
        $cmd = Get-Command Convert-Video
        $cmd.Parameters['InputFile'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] } |
            ForEach-Object { $_.Mandatory | Should -BeTrue }
    }

    It 'Has required OutputFile parameter' {
        $cmd = Get-Command Convert-Video
        $cmd.Parameters['OutputFile'].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] } |
            ForEach-Object { $_.Mandatory | Should -BeTrue }
    }

    It 'Has PresetName parameter' {
        $cmd = Get-Command Convert-Video
        $cmd.Parameters.ContainsKey('PresetName') | Should -BeTrue
    }

    It 'Supports -WhatIf' {
        $cmd = Get-Command Convert-Video
        $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
    }
}
