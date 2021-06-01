$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Convert-Video
{
    [CmdletBinding()]
    param
    (
        [string]$InputFile,
        [string]$OutputFile,
        [string]$PresetName = 'Plex',
        [string]$PresetFile = "$PSScriptRoot\..\..\presets\Plex.json",
        [string]$HandBrakeExe = "$env:ProgramFiles\HandBrakeCLI\HandBrakeCLI.exe"
    )

    & $HandBrakeExe --preset-import-file $PresetFile -i $InputFile -o $OutputFile --preset $PresetName --json | ForEach-Object `
    {
        $line = [string]$_
        Write-Debug $line
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "HandBrake execution failed; result code: $LASTEXITCODE"
    }
    elseif (-not (Test-Path $OutputFile))
    {
        throw "HandBrake execution failed; output file was not created: $OutputFile"
    }

    Write-Information "HandBrake execution succeeded; wrote MP4 to: $OutputFile" 
}