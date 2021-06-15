$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Backup-DiskMedia
{
    [CmdletBinding()]
    param
    (
        [Title]$Title,
        [Disk]$Disk,
        [int]$Episode,
        [string]$MediaName,
        [string]$IntermediatePath,
        [string]$OutputPath
    )

    if (!$MediaName)
    {
        $name = $title.Name ?? $disk.Name
        $MediaName = Get-TitleFileName $name
    }
    
    $mkvPath = "$IntermediatePath\$($title.FileName)"
    Write-Information "Ripping title $($title.Id) - '$MediaName' to: $mkvPath"
    
    if (Test-Path $mkvPath)
    {
        Write-Warning "Deleting existing file: $mkvPath"
        Remove-Item $mkvPath -Force
    }
    
    Backup-Title -TitleId $title.Id -OutputPath $IntermediatePath -Progress
    
    $m4vPath = Get-MediaFilePath $OutputPath $MediaName $Season $Episode
    Write-Information "Encoding title $($title.Id) - '$MediaName' to: $m4vPath"
    
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
}

class Disk
{
    [int]$Id
    [string]$Type
    [string]$Name
    [string]$AlternateName

    Disk ([Hashtable] $RawDiskInfo)
    {
        if (!$RawDiskInfo)
        {
            throw 'Parameter RawDiskInfo required'
        }

        $this.Id = [int]$RawDiskInfo['33']
        $this.Type = $RawDiskInfo['1']
        $this.Name = $RawDiskInfo['2']
        $this.AlternateName = $RawDiskInfo['30']
    }
}

class Title 
{
    [int]$Id
    [string]$Name
    [string]$FileName
    [int]$Chapters
    [timespan]$Duration
    [long]$SizeInBytes

    Title ([Hashtable] $RawTitleInfo)
    {
        if (!$RawTitleInfo)
        {
            throw 'Parameter RawDiskInfo required'
        }

        $this.Id = [int]$RawTitleInfo['33']
        $this.Name = $RawTitleInfo['2']
        $this.FileName = $RawTitleInfo['27']
        $this.Chapters = [int]$RawTitleInfo['8']
        $this.Duration = $RawTitleInfo['9'] ?? [timespan]::Zero
        $this.SizeInBytes = [long]$RawTitleInfo['11']
    }
}