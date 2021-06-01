$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
Set-StrictMode -Version 3.0

function Backup-DiskMedia
{
    [CmdletBinding()]
    param
    (
        [string]$titleId,
        [int]$Episode,
        [string]$MediaName,
        [string]$IntermediatePath,
        [string]$OutputPath
    )

    $title = $titleInfo[$titleId]

    if (!$MediaName)
    {
        $name = $title['2'] ?? $disk['2']
        $MediaName = Get-TitleFileName $name
    }
    
    $mkvPath = "$IntermediatePath\$($title['27'])"
    Write-Information "Ripping title $titleId - '$MediaName' to: $mkvPath"
    
    if (Test-Path $mkvPath)
    {
        Write-Warning "Deleting existing file: $mkvPath"
        Remove-Item $mkvPath -Force
    }
    
    Backup-Title -TitleId $titleId -OutputPath $IntermediatePath -Progress
    
    $m4vPath = Get-MediaFilePath $OutputPath $MediaName $Season $Episode
    Write-Information "Encoding title $titleId - '$MediaName' to: $m4vPath"
    
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