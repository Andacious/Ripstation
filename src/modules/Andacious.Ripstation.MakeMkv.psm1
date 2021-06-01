$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Get-DiskAndTitleInfo
{
    [CmdletBinding()]
    param
    (
        [switch]$Progress,
        [string]$DiskNumber = '0',
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

    $disk = $diskInfo[$DiskNumber]

    return $disk, $titleInfo
}

function Backup-Title
{
    [CmdletBinding()]
    param
    (
        [string]$TitleId,
        [string]$OutputPath,
        [switch]$Progress,
        [string]$DiskNumber = '0',
        [string]$MakeMkvExe = "${env:ProgramFiles(x86)}\MakeMKV\makemkvcon64.exe"
    )

    $rippingMessage = "Ripping title $TitleId to: $OutputPath"
    $lastRipStatus = ''

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

    if (!$?)
    {
        throw "MakeMKV execution failed; result code: $LASTEXITCODE"
    }

    Write-Information "MakeMKV execution succeeded; wrote MKV to: $OutputPath"
}