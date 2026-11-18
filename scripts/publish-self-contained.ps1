param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "bin/$Configuration/net9.0-windows/$Runtime/publish"

Push-Location $repoRoot
try {
    dotnet publish .\ChangeAudioSource.csproj -c $Configuration -r $Runtime --self-contained true -o $publishDir "-p:Version=$Version"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}
