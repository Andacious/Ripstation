using module ..\src\Andacious.Ripstation.psd1

BeforeAll {
    Import-Module "$PSScriptRoot\..\src\Andacious.Ripstation.psd1" -Force
}

Describe 'Title class' {
    It 'Maps raw hashtable values to properties' {
        $raw = @{
            '33' = '2'
            '2'  = 'My Title'
            '27' = 'D1_t02.mkv'
            '8'  = '10'
            '9'  = '01:30:00'
            '11' = '5000000000'
        }

        $title = [Title]::new($raw)

        $title.Id | Should -Be 2
        $title.Name | Should -Be 'My Title'
        $title.FileName | Should -Be 'D1_t02.mkv'
        $title.Chapters | Should -Be 10
        $title.Duration | Should -Be ([timespan]'01:30:00')
        $title.SizeInBytes | Should -Be 5000000000
    }

    It 'Handles missing duration gracefully' {
        $raw = @{
            '33' = '0'
            '2'  = 'No Duration'
            '27' = 'D1_t00.mkv'
            '8'  = '5'
            '9'  = $null
            '11' = '1000'
        }

        $title = [Title]::new($raw)
        $title.Duration | Should -Be ([timespan]::Zero)
    }

    It 'Throws on null input' {
        { [Title]::new($null) } | Should -Throw '*RawDiskInfo required*'
    }
}

Describe 'Disk class' {
    It 'Maps raw hashtable values to properties' {
        $raw = @{
            '33' = '1'
            '1'  = 'Blu-ray disc'
            '2'  = 'MY_DISK'
            '30' = 'Alternate Name'
        }

        $disk = [Disk]::new($raw)

        $disk.Id | Should -Be 1
        $disk.Type | Should -Be 'Blu-ray disc'
        $disk.Name | Should -Be 'MY_DISK'
        $disk.AlternateName | Should -Be 'Alternate Name'
    }

    It 'Throws on null input' {
        { [Disk]::new($null) } | Should -Throw '*RawDiskInfo required*'
    }
}

Describe 'Backup-DiskMedia' {
    BeforeAll {
        $script:titleRaw = @{
            '33' = '2'
            '2'  = 'Test Title'
            '27' = 'C1_t02.mkv'
            '8'  = '9'
            '9'  = '01:10:37'
            '11' = '2654605312'
        }
        $script:diskRaw = @{
            '33' = '1'
            '1'  = 'Blu-ray disc'
            '2'  = 'TEST_DISK'
            '30' = 'Test Disk'
        }
    }

    BeforeEach {
        $script:title = [Title]::new($script:titleRaw)
        $script:title.Id = 2
        $script:disk = [Disk]::new($script:diskRaw)
        $script:disk.Id = 1

        Mock Backup-Title {} -ModuleName Andacious.Ripstation
        Mock Convert-Video {} -ModuleName Andacious.Ripstation
        # Default $true so Convert-Video's ValidateScript on InputFile passes
        Mock Test-Path { $true } -ModuleName Andacious.Ripstation
        Mock New-Item {} -ModuleName Andacious.Ripstation
        Mock Remove-Item {} -ModuleName Andacious.Ripstation
    }

    It 'Passes DiskNumber to Backup-Title' {
        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Backup-Title -ModuleName Andacious.Ripstation -ParameterFilter {
            $DiskNumber -eq 1
        }
    }

    It 'Passes TitleId to Backup-Title' {
        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Backup-Title -ModuleName Andacious.Ripstation -ParameterFilter {
            $TitleId -eq '2'
        }
    }

    It 'Passes IntermediatePath as OutputPath to Backup-Title' {
        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Backup-Title -ModuleName Andacious.Ripstation -ParameterFilter {
            $OutputPath -eq 'C:\mkv'
        }
    }

    It 'Uses title name when MediaName is not provided' {
        Mock Get-TitleFileName { 'Test Title' } -ModuleName Andacious.Ripstation

        Backup-DiskMedia -Title $script:title -Disk $script:disk `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Get-TitleFileName -ModuleName Andacious.Ripstation -ParameterFilter {
            $fileName -eq 'Test Title'
        }
    }

    It 'Falls back to disk name when title name is null' {
        $noNameTitle = [Title]::new(@{
            '33' = '0'; '2' = $null; '27' = 'D1_t00.mkv'
            '8'  = '1'; '9' = '00:01:00'; '11' = '1000'
        })
        $noNameTitle.Id = 0

        Mock Get-TitleFileName { 'Test Disk' } -ModuleName Andacious.Ripstation

        Backup-DiskMedia -Title $noNameTitle -Disk $script:disk `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Get-TitleFileName -ModuleName Andacious.Ripstation -ParameterFilter {
            $fileName -eq 'TEST_DISK'
        }
    }

    It 'Deletes existing intermediate MKV before ripping' {
        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Remove-Item -ModuleName Andacious.Ripstation -ParameterFilter {
            $Path -eq 'C:\mkv\C1_t02.mkv'
        }
    }

    It 'Calls Convert-Video with correct input file' {
        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke Convert-Video -ModuleName Andacious.Ripstation -ParameterFilter {
            $InputFile -eq 'C:\mkv\C1_t02.mkv'
        }
    }

    It 'Creates output directory when it does not exist' {
        Mock Test-Path { $false } -ModuleName Andacious.Ripstation `
            -ParameterFilter { $Path -like 'C:\out*' }

        Backup-DiskMedia -Title $script:title -Disk $script:disk -MediaName 'Test' `
            -IntermediatePath 'C:\mkv' -OutputPath 'C:\out'

        Should -Invoke New-Item -ModuleName Andacious.Ripstation
    }
}
