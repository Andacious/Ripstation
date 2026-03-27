$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Convert-Video
{
    <#
    .SYNOPSIS
    Converts a video file using HandBrake.

    .DESCRIPTION
    Uses HandBrake CLI to convert a video file (typically MKV) to M4V format
    using a specified preset configuration.

    .PARAMETER InputFile
    The path to the input video file to convert.

    .PARAMETER OutputFile
    The path where the converted video file will be saved.

    .PARAMETER PresetName
    The name of the HandBrake preset to use (default is 'Plex').

    .PARAMETER PresetFile
    The path to the JSON file containing HandBrake presets.

    .PARAMETER HandBrakeExe
    The path to the HandBrake CLI executable.

    .EXAMPLE
    Convert-Video -InputFile 'C:\Temp\video.mkv' -OutputFile 'C:\Media\video.m4v'

    .EXAMPLE
    Convert-Video -InputFile 'input.mkv' -OutputFile 'output.m4v' -PresetName 'Custom' -PresetFile 'C:\presets.json'
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [ValidateScript({ Test-Path $_ })]
        [string]$InputFile,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$OutputFile,

        [ValidateNotNullOrEmpty()]
        [string]$PresetName = 'Plex',

        [ValidateNotNullOrEmpty()]
        [ValidateScript({ Test-Path $_ })]
        [string]$PresetFile = "$PSScriptRoot\..\..\presets\Plex.json",

        [ValidateNotNullOrEmpty()]
        [ValidateScript({ Test-Path $_ })]
        [string]$HandBrakeExe = "$env:ProgramFiles\HandBrakeCLI\HandBrakeCLI.exe"
    )

    if ($PSCmdlet.ShouldProcess($OutputFile, "Convert video from $InputFile"))
    {
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
}