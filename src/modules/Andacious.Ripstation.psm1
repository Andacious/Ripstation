$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Backup-DiskMedia
{
    <#
    .SYNOPSIS
    Rips and encodes a title from a disk to an output file.

    .DESCRIPTION
    Backs up a specific title from a disk using MakeMKV to an intermediate MKV file,
    then encodes it to M4V format using HandBrake, and removes the intermediate file.

    .PARAMETER Title
    The Title object containing information about the title to rip.

    .PARAMETER Disk
    The Disk object containing information about the source disk.

    .PARAMETER Season
    The season number for TV show episodes (used in output filename).

    .PARAMETER Episode
    The episode number for TV show episodes (used in output filename).

    .PARAMETER MediaName
    The name for the media file. If not specified, uses the title or disk name.

    .PARAMETER IntermediatePath
    The path where intermediate MKV files will be stored during ripping.

    .PARAMETER OutputPath
    The final output path for the encoded M4V files.

    .EXAMPLE
    Backup-DiskMedia -Title $title -Disk $disk -Episode 1 -MediaName "MyShow" -IntermediatePath "C:\Temp" -OutputPath "C:\Media"

    .EXAMPLE
    Backup-DiskMedia -Title $title -Disk $disk -Season 1 -Episode 3 -MediaName "Series" -IntermediatePath "S:\MKV" -OutputPath "S:\Plex\Media\TV"
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param
    (
        [Parameter(Mandatory)]
        [Title]$Title,

        [Parameter(Mandatory)]
        [Disk]$Disk,

        [int]$Season,

        [int]$Episode,

        [ValidateNotNullOrEmpty()]
        [string]$MediaName,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$IntermediatePath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$OutputPath
    )

    if (!$MediaName)
    {
        $name = $Title.Name ?? $Disk.Name
        $MediaName = Get-TitleFileName $name
    }

    $mkvPath = "$IntermediatePath\$($Title.FileName)"
    Write-Information "Ripping title $($Title.Id) - '$MediaName' to: $mkvPath"

    if (Test-Path $mkvPath)
    {
        if ($PSCmdlet.ShouldProcess($mkvPath, 'Delete existing file'))
        {
            Write-Warning "Deleting existing file: $mkvPath"
            Remove-Item $mkvPath -Force
        }
    }

    if ($PSCmdlet.ShouldProcess("Title $($Title.Id)", 'Rip to MKV'))
    {
        Backup-Title -TitleId $Title.Id -OutputPath $IntermediatePath -Progress
    }

    $m4vPath = Get-MediaFilePath $OutputPath $MediaName $Season $Episode
    Write-Information "Encoding title $($Title.Id) - '$MediaName' to: $m4vPath"

    if (Test-Path $m4vPath)
    {
        if ($PSCmdlet.ShouldProcess($m4vPath, 'Delete existing file'))
        {
            Write-Warning "Deleting existing file: $m4vPath"
            Remove-Item $m4vPath -Force
        }
    }

    $m4vDirectory = Split-Path $m4vPath -Parent
    if (-not (Test-Path $m4vDirectory))
    {
        New-Item $m4vDirectory -ItemType Directory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($m4vPath, 'Convert to M4V'))
    {
        Convert-Video -InputFile $mkvPath -OutputFile $m4vPath
    }

    ## Remove intermediate MKV file if Handbrake succeeds
    if ((Test-Path $mkvPath) -and $PSCmdlet.ShouldProcess($mkvPath, 'Delete intermediate MKV'))
    {
        Remove-Item $mkvPath -Force
    }
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