$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

function Get-TitleFileName
{
    <#
    .SYNOPSIS
    Converts a filename to a safe, title-cased format.

    .DESCRIPTION
    Removes invalid characters from a filename, replaces underscores with spaces,
    and converts the text to title case.

    .PARAMETER fileName
    The filename to sanitize and format.

    .EXAMPLE
    Get-TitleFileName 'my_movie_title'
    Returns: 'My Movie Title'
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$fileName
    )

    $invalidFileChars = [IO.Path]::GetInvalidFileNameChars() -join ''
    $invalidPathChars = [IO.Path]::GetInvalidPathChars() -join ''
    $regexChars = [RegEx]::Escape($invalidFileChars + $invalidPathChars)
    $safeFileName = ($fileName -replace "[$regexChars]")
    $spacedFileName = ($safeFileName -replace '_', ' ')
    return [CultureInfo]::CurrentCulture.TextInfo.ToTitleCase($spacedFileName.ToLower())
}

function Get-MediaFilePath
{
    <#
    .SYNOPSIS
    Generates a media file path based on naming conventions.

    .DESCRIPTION
    Creates a properly formatted file path for media files, supporting both
    TV show episodes (with season/episode numbers) and movies.

    .PARAMETER outputPath
    The base output directory path.

    .PARAMETER mediaName
    The name of the media (TV show or movie).

    .PARAMETER season
    The season number for TV shows (optional).

    .PARAMETER episode
    The episode number for TV shows (optional).

    .EXAMPLE
    Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Movie' -season 0 -episode 0
    Returns: 'C:\Media\Movie.m4v'

    .EXAMPLE
    Get-MediaFilePath -outputPath 'C:\Media' -mediaName 'Show' -season 1 -episode 5
    Returns: 'C:\Media\Show\Season 01\Show - s01e05.m4v'
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$outputPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$mediaName,

        [int]$season,

        [int]$episode
    )
    $path = $outputPath

    if ($mediaName -and $season -and $episode)
    {
        $seasonNumber = "$season".PadLeft(2, '0')
        $episodeNumber = "$episode".PadLeft(2, '0')

        $path = Join-Path $path $mediaName
        $path = Join-Path $path "Season $seasonNumber"

        $file = "$mediaName - s$($seasonNumber)e$($episodeNumber).m4v"
    }
    else
    {
        $file = "$mediaName.m4v"
    }

    return (Join-Path $path $file)
}

function Open-DiskDrive
{
    <#
    .SYNOPSIS
    Ejects a CD/DVD drive.

    .DESCRIPTION
    Opens (ejects) the specified CD/DVD drive tray using Windows Media Player COM object.

    .PARAMETER DriveNumber
    The drive number to eject (default is 0 for the first drive).

    .EXAMPLE
    Open-DiskDrive

    .EXAMPLE
    Open-DiskDrive -DriveNumber 1

    .EXAMPLE
    eject
    Uses the alias to eject the default drive.
    #>
    [CmdletBinding()]
    [Alias('eject')]
    param
    (
        [ValidateRange(0, 10)]
        [int]$DriveNumber = 0
    )

    $com = New-Object -com 'WMPlayer.OCX.7'
    $drive = $com.cdromCollection.item($DriveNumber)

    Write-Verbose "Ejecting disk drive: $($drive.driveSpecifier)"
    $drive.eject();
}