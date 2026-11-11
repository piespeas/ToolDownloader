[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'The .NET SDK is required: https://dotnet.microsoft.com/download'
    }

    dotnet publish .\AcousticVoidDownloader.csproj `
        -c Release `
        -o $Output `
        -p:DebugType=None `
        -p:DebugSymbols=false

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $exe = Join-Path $Output 'AcousticVoidDownloader.exe'
    $legacy = Join-Path $Output 'ToolsDownloader.exe'
    if (Test-Path $legacy) {
        Remove-Item $legacy -Force
    }

    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -Path (Join-Path $Output 'SHA256SUMS.txt') `
        -Value "$hash  AcousticVoidDownloader.exe" `
        -Encoding ascii

    Write-Host ''
    Write-Host "Built:  $exe" -ForegroundColor Green
    Write-Host ("Size:   {0:n0} MB" -f ((Get-Item $exe).Length / 1MB)) -ForegroundColor DarkGray
    Write-Host "SHA256: $hash" -ForegroundColor DarkGray
}
finally {
    Pop-Location
}
