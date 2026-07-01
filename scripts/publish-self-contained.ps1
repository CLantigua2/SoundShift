param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "bin/$Configuration/net9.0-windows/$Runtime/publish"

Push-Location $repoRoot
try {
    dotnet publish .\ChangeAudioSource.csproj -c $Configuration -r $Runtime --self-contained true -o $publishDir
}
finally {
    Pop-Location
}
