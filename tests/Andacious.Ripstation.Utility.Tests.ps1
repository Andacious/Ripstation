BeforeAll {
    Import-Module "$PSScriptRoot\..\src\Andacious.Ripstation.psd1" -Force
}

Describe 'Get-TitleFileName' {
    It 'Replaces underscores with spaces and title-cases' {
        Get-TitleFileName 'my_movie_title' | Should -Be 'My Movie Title'
    }

    It 'Handles already clean names' {
        Get-TitleFileName 'GoodName' | Should -Be 'Goodname'
    }

    It 'Removes invalid filename characters' {
        Get-TitleFileName 'Movie: The "Sequel"' | Should -Be 'Movie The Sequel'
    }

    It 'Title-cases mixed input' {
        Get-TitleFileName 'ALL CAPS TITLE' | Should -Be 'All Caps Title'
    }

    It 'Handles single word' {
        Get-TitleFileName 'batman' | Should -Be 'Batman'
    }
}

Describe 'Get-MediaFilePath' {
    It 'Returns movie path when season and episode are 0' {
        $result = Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Movie' -season 0 -episode 0
        $result | Should -Be 'C:\Media\Movie.m4v'
    }

    It 'Returns TV show path with season and episode' {
        $result = Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Show' -season 1 -episode 5
        $result | Should -Be 'C:\Media\Show\Season 01\Show - s01e05.m4v'
    }

    It 'Pads single-digit season and episode numbers' {
        $result = Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Show' -season 2 -episode 3
        $result | Should -Be 'C:\Media\Show\Season 02\Show - s02e03.m4v'
    }

    It 'Handles double-digit season and episode numbers' {
        $result = Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Show' -season 12 -episode 24
        $result | Should -Be 'C:\Media\Show\Season 12\Show - s12e24.m4v'
    }

    It 'Returns movie path when only mediaName is provided' {
        $result = Get-MediaFilePath -outputPath 'C:\Out' -mediaName 'Solo Film'
        $result | Should -Be 'C:\Out\Solo Film.m4v'
    }
}
