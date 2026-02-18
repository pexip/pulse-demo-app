# Generate Markdown documentation from Pexip.Pulse NuGet package
$pulseVersion = "1.0.16785"
$pulseDll = "$env:USERPROFILE\.nuget\packages\pexip.pulse\$pulseVersion\lib\net8.0\Pexip.Pulse.dll"
$outputDir = ".\docs"

Write-Host "Generating documentation for Pexip.Pulse v$pulseVersion..." -ForegroundColor Cyan

# Restore local tools first
Write-Host "Restoring local tools..." -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore local tools."
    exit 1
}

if (-not (Test-Path $pulseDll)) {
    Write-Error "Pexip.Pulse.dll not found at: $pulseDll"
    Write-Host "Make sure the package is restored and the version is correct." -ForegroundColor Yellow
    exit 1
}

dotnet xmldocmd $pulseDll $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Documentation generated successfully in $outputDir" -ForegroundColor Green
} else {
    Write-Error "Failed to generate documentation."
    exit 1
}
