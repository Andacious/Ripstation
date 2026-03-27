$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Get-DiskAndTitleInfo
{
    <#
    .SYNOPSIS
    Scans a disk and retrieves information about all available titles.

    .DESCRIPTION
    Uses MakeMKV to scan a disk and return detailed information about the disk
    and all available titles including duration, size, chapters, and filenames.

    .PARAMETER Progress
    Display progress information during the scan operation.

    .PARAMETER DiskNumber
    The disk number to scan (default is '0').

    .PARAMETER MakeMkvExe
    The path to the MakeMKV executable.

    .EXAMPLE
    $disk, $titles = Get-DiskAndTitleInfo -DiskNumber '0' -Progress

    .OUTPUTS
    System.Object[]
    Returns an array containing a Disk object and an array of Title objects.
    #>
    [CmdletBinding()]
    [OutputType([System.Object[]])]
    param
    (
        [switch]$Progress,

        [ValidateNotNullOrEmpty()]
        [string]$DiskNumber = '0',

        [ValidateNotNullOrEmpty()]
        [ValidateScript({ Test-Path $_ })]
        [string]$MakeMkvExe = "${env:ProgramFiles(x86)}\MakeMKV\makemkvcon64.exe"
    )

    $diskInfo = [ordered]@{}
    $titleInfo = [ordered]@{}
    $scanningMessage = "Scanning disk $DiskNumber for titles..."

    $lastScanStatus = ''
    & $MakeMkvExe --robot --cache=1024 --messages=-stdout --progress=-same --minlength=600 info disc:$DiskNumber | ForEach-Object `
    {
        $line = [string]$_

        Write-Debug $line

        if ($line.StartsWith('CINFO') -and $line -match '^CINFO:(?<Code>[0-9]+),(?<Flags>[0-9]+),"(?<Value>.+)"$')
        {
            if (!$diskInfo[$DiskNumber])
            {
                $diskInfo[$DiskNumber] = @{}
            }

            $diskInfo[$DiskNumber][$Matches.Code] = $Matches.Value
        }
        elseif ($line.StartsWith('TINFO') -and $line -match '^TINFO:(?<Id>[0-9]+),(?<Code>[0-9]+),(?<Flags>[0-9]+),"(?<Value>.+)"$')
        {
            if (!$titleInfo[$Matches.Id])
            {
                $titleInfo[$Matches.Id] = @{}
            }

            $titleInfo[$Matches.Id][$Matches.Code] = $Matches.Value
        }
        elseif ($Progress -and $line.StartsWith('PRGV') -and $line -match '^PRGV:(?<Current>[0-9]+),(?<Total>[0-9]+),(?<Max>[0-9]+)$')
        {
            $percent = ([int]$Matches.Total / [int]$Matches.Max) * 100
            Write-Progress -Activity $scanningMessage -Status $lastScanStatus -PercentComplete $percent
        }
        elseif ($Progress -and $line.StartsWith('PRG') -and $line -match '^PRG[CT]:(?<Code>[0-9]+),(?<Id>[0-9]+),"(?<Name>.+)"$')
        {
            $lastScanStatus = $Matches.Name
        }
    }

    if ($Progress)
    {
        # Clear the progress bar
        Write-Progress -Activity $scanningMessage -Completed
    }

    $titles = @()

    foreach ($kvp in $titleInfo.GetEnumerator())
    {
        $title = [Title]::new($kvp.Value)
        $title.Id = $kvp.Key
        $titles += $title
    }

    $disk = [Disk]::new($diskInfo[$DiskNumber])
    $disk.Id = $DiskNumber

    return $disk, $titles
}

function Backup-Title
{
    <#
    .SYNOPSIS
    Rips a specific title from a disk to an MKV file.

    .DESCRIPTION
    Uses MakeMKV to rip a specific title from a disk to the specified output path.
    The title must have been previously scanned using Get-DiskAndTitleInfo.

    .PARAMETER TitleId
    The ID of the title to rip.

    .PARAMETER OutputPath
    The directory path where the MKV file will be saved.

    .PARAMETER Progress
    Display progress information during the ripping operation.

    .PARAMETER DiskNumber
    The disk number to rip from (default is '0').

    .PARAMETER MakeMkvExe
    The path to the MakeMKV executable.

    .EXAMPLE
    Backup-Title -TitleId '0' -OutputPath 'C:\Temp\MKV' -Progress
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$TitleId,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$OutputPath,

        [switch]$Progress,

        [ValidateNotNullOrEmpty()]
        [string]$DiskNumber = '0',

        [ValidateNotNullOrEmpty()]
        [ValidateScript({ Test-Path $_ })]
        [string]$MakeMkvExe = "${env:ProgramFiles(x86)}\MakeMKV\makemkvcon64.exe"
    )

    $rippingMessage = "Ripping title $TitleId to: $OutputPath"
    $lastRipStatus = ''

    if ($PSCmdlet.ShouldProcess("Title $TitleId from disk $DiskNumber", 'Rip to MKV'))
    {
        & $MakeMkvExe --robot --noscan --cache=1024 --messages=-stdout --progress=-same --minlength=600 mkv disc:$DiskNumber $TitleId $OutputPath | ForEach-Object `
        {
            $line = [string]$_

            Write-Debug $line

            if ($Progress -and $line.StartsWith('PRGV') -and $line -match '^PRGV:(?<Current>[0-9]+),(?<Total>[0-9]+),(?<Max>[0-9]+)$')
            {
                $percent = ([int]$Matches.Total / [int]$Matches.Max) * 100
                Write-Progress -Activity $rippingMessage -Status $lastRipStatus -PercentComplete $percent
            }
            elseif ($Progress -and $line.StartsWith('PRG') -and $line -match '^PRG[CT]:(?<Code>[0-9]+),(?<Id>[0-9]+),"(?<Name>.+)"$')
            {
                $lastRipStatus = $Matches.Name
            }
        }

        if ($Progress)
        {
            # Clear the progress bar
            Write-Progress -Activity $rippingMessage -Completed
        }

        if ($LASTEXITCODE -ne 0)
        {
            throw "MakeMKV execution failed; result code: $LASTEXITCODE"
        }

        Write-Information "MakeMKV execution succeeded; wrote MKV to: $OutputPath"
    }
}