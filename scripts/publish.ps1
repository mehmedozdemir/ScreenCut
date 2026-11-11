<#
.SYNOPSIS
    Builds release packages for ScreenCut.
.DESCRIPTION
    For every runtime produces a portable, self-contained single-file ScreenCut.exe (no .NET install needed),
    zipped as ScreenCut-<version>-<rid>.zip. Optionally also a small framework-dependent build that
    requires the .NET 10 Desktop Runtime. Writes SHA256SUMS.txt for all zips.
.EXAMPLE
    ./scripts/publish.ps1 -Version 1.2.3
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+([-+][0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$OutputDir = 'artifacts',

    [string[]]$Runtimes = @('win-x64', 'win-arm64'),

    [switch]$SkipFrameworkDependent
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/ScreenCut/ScreenCut.csproj'
$out = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $root $OutputDir }
$staging = Join-Path $out 'staging'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

function Invoke-Publish([string]$rid, [bool]$selfContained, [string]$zipName) {
    $dir = Join-Path $staging $zipName
    $arguments = @(
        'publish', $project,
        '--configuration', 'Release',
        '--runtime', $rid,
        '--output', $dir,
        "-p:Version=$Version",
        "-p:SelfContained=$($selfContained.ToString().ToLowerInvariant())",
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:DebugType=none'
    )
    if ($selfContained) {
        $arguments += '-p:EnableCompressionInSingleFile=true'
        $arguments += '-p:PublishReadyToRun=true'
    }

    Write-Host "==> $zipName" -ForegroundColor Cyan
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }

    Copy-Item (Join-Path $root 'LICENSE') $dir -ErrorAction SilentlyContinue
    $zip = Join-Path $out "$zipName.zip"
    Compress-Archive -Path (Join-Path $dir '*') -DestinationPath $zip -CompressionLevel Optimal
}

foreach ($rid in $Runtimes) {
    Invoke-Publish $rid $true "ScreenCut-$Version-$rid"
}

if (-not $SkipFrameworkDependent) {
    Invoke-Publish 'win-x64' $false "ScreenCut-$Version-win-x64-framework-dependent"
}

Remove-Item $staging -Recurse -Force

$sums = Get-ChildItem $out -Filter '*.zip' | Sort-Object Name | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
}
$sums | Set-Content -Path (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii

Write-Host ''
Get-ChildItem $out | Format-Table Name, @{ N = 'Size (MB)'; E = { [math]::Round($_.Length / 1MB, 1) } } -AutoSize
