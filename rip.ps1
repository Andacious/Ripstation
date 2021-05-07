[CmdletBinding()]
param
(
    [string]$MediaName = '',
    [string]$DiskNumber = '0',
    [string]$IntermediatePath = 'S:\MKV',
    [string]$OutputPath = 'S:\Plex\Media\Movies',
    [int]$Season = 0,
    [int]$EpisodeStart = 0,
    [int]$EpisodeEnd = 0
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0
Import-Module '.\src\Andacious.Ripstation.psd1' -Force

function RipTitle([string]$titleId, [int]$Episode)
{
    $title = $titleInfo[$titleId]

    if (!$MediaName)
    {
        $name = $title['2'] ?? $disk['2']
        $MediaName = Get-TitleFileName $name
    }
    
    $mkvPath = "$IntermediatePath\$($title['27'])"
    
    Write-Host "Ripping title $titleId - '$MediaName' to: $mkvPath" -ForegroundColor Cyan
    
    if (Test-Path $mkvPath)
    {
        Write-Warning "Deleting existing file: $mkvPath"
        Remove-Item $mkvPath -Force
    }
    
    Backup-Title -TitleId $titleId -OutputPath $IntermediatePath -Progress
    
    Write-Host "MakeMKV execution succeeded; wrote MKV to: $mkvPath" -ForegroundColor Green
    
    $m4vPath = Get-MediaFilePath $OutputPath $MediaName $Season $Episode
    Write-Host "Encoding title $titleId - '$MediaName' to: $m4vPath"
    
    if (Test-Path $m4vPath)
    {
        Write-Warning "Deleting existing file: $m4vPath"
        Remove-Item $m4vPath -Force
    }

    $m4vDirectory = Split-Path $m4vPath -Parent
    New-Item $m4vDirectory -ItemType Directory -Force | Out-Null
    
    Convert-Video -InputFile $mkvPath -OutputFile $m4vPath
    
    ## Remove intermediate MKV file if Handbrake succeeds
    Remove-Item $mkvPath -Force
    
    Write-Host "HandBrake execution succeeded; wrote MP4 to: $m4vPath" -ForegroundColor Green
}

$scanningMessage = "Scanning disk $DiskNumber for titles..."
Write-Host $scanningMessage -ForegroundColor Cyan

$disk, $titleInfo = Get-DiskAndTitleInfo -DiskNumber $DiskNumber -Progress

Write-Host "Found $($titleInfo.Count) titles on disk $DiskNumber - $($disk['2']):" -ForegroundColor Cyan

foreach ($kvp in $titleInfo.GetEnumerator())
{
    Write-Host "Title ID $($kvp.Key): $($kvp.Value['27']) - $($kvp.Value['9']) - $($kvp.Value['10']) - $($kvp.Value['8']) chapters"
}

if ($titleInfo.Count -eq 1)
{
    $titles = ,[string]$titleInfo.Keys[0]
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
    throw "Episode<->title count mismatch"
}

for ($i = 0; $i -lt $titles.Count; $i++)
{
    $titleId = $titles[$i]
    $episode = $episodeNumbers[$i]
    RipTitle $titleId $episode
}

