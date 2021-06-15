using module .\src\Andacious.Ripstation.psd1

[CmdletBinding()]
param
(
    [string]$MediaName = '',
    [string]$DiskNumber = '0',
    [string]$IntermediatePath = 'S:\MKV',
    [string]$OutputPath = 'S:\Plex\Media\Movies',
    [int]$Season = 0,
    [int]$EpisodeStart = 0,
    [int]$EpisodeEnd = 0,
    [switch]$Eject
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0
Import-Module .\src\Andacious.Ripstation.psd1 -Force

$scanningMessage = "Scanning disk $DiskNumber for titles..."
Write-Host $scanningMessage -ForegroundColor Cyan

$disk, $titleInfo = Get-DiskAndTitleInfo -DiskNumber $DiskNumber -Progress

Write-Host "Found $($titleInfo.Count) titles on disk $DiskNumber - $($disk.Name):" -ForegroundColor Cyan

foreach ($title in $titleInfo)
{
    Write-Host "Title ID $($title.Id): $($title.FileName) - $($title.Duration) - $($title.SizeInBytes) - $($title.Chapters) chapters"
}

if ($titleInfo.Count -eq 1)
{
    $titles = ,[string]$titleInfo[0].Id
    Write-Warning "Only one title was found on disk $diskNumber; defaulting to title $titles"
}
else
{
    Write-Host 'Select title ID(s):'
    $titles = (Read-Host).Split(',', [System.StringSplitOptions]::RemoveEmptyEntries)
}

Write-Host "Ripping $($titles.Count) titles: $titles"

$episodeNumbers = ($EpisodeStart..$EpisodeEnd)

if ($episodeNumbers.Count -ne $titles.Count)
{
    throw "Episode <-> title count mismatch"
}

for ($i = 0; $i -lt $titles.Count; $i++)
{
    $titleId = $titles[$i]
    $title = $titleInfo | Where-Object -Property Id -EQ $titleId
    $episode = $episodeNumbers[$i]

    Backup-DiskMedia -Title $title -Disk $disk -Episode $episode -MediaName $MediaName -IntermediatePath $IntermediatePath -OutputPath $OutputPath
}

if ($Eject)
{
    Open-DiskDrive
}