$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

function Get-TitleFileName([string]$fileName)
{
    $invalidFileChars = [IO.Path]::GetInvalidFileNameChars() -join ''
    $invalidPathChars = [IO.Path]::GetInvalidPathChars() -join ''
    $regexChars = [RegEx]::Escape($invalidFileChars + $invalidPathChars)
    $safeFileName = ($fileName -replace "[$regexChars]")
    $spacedFileName = ($safeFileName -replace '_', ' ')
    return [CultureInfo]::CurrentCulture.TextInfo.ToTitleCase($spacedFileName.ToLower())
}

function Get-MediaFilePath([string]$outputPath, [string]$mediaName, [int]$season, [int]$episode)
{
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
    [CmdletBinding()]
    [Alias('eject')]
    param
    (
        [int]$DriveNumber = 0
    )

    $com = New-Object -com 'WMPlayer.OCX.7'
    $drive = $com.cdromCollection.item($DriveNumber)

    Write-Verbose "Ejecting disk drive: $($drive.driveSpecifier)"
    $drive.eject();
}