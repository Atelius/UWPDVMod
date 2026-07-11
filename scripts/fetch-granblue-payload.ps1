# Populates payload/granblue-relink/ with the files the Granblue profile installs.
# These get embedded into UWPDVMod.Core as resources so the published exe is a single file.
#   GBFRelinkFix.asi  - from the mod's xmake build output (sibling repo)
#   GBFRelinkFix.ini  - default settings from the mod repo root
#   winmm.dll         - Ultimate ASI Loader, copied from the game install if present,
#                       otherwise downloaded from the ThirteenAG release.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$payload = Join-Path $root "payload\granblue-relink"
New-Item -ItemType Directory -Force $payload | Out-Null

# The GBFRelinkFix mod repo lives under the gbflinkUW workspace root as UWMod\GBFRelinkFix.
# Walk up from this solution's root looking for it, since the solution's own nesting
# depth (gbflinkUW\UWPDVMod vs gbflinkUW\UWPDVMod\UWPDVMod) has moved before.
$modRepo = $null
$probe = $root
for ($i = 0; $i -lt 4; $i++) {
    $candidate = Join-Path $probe "UWMod\GBFRelinkFix"
    if (Test-Path $candidate) { $modRepo = $candidate; break }
    $probe = Split-Path $probe -Parent
}
if (-not $modRepo) { throw "Could not find the UWMod\GBFRelinkFix mod repo above $root" }

$asi = Join-Path $modRepo "build\windows\x64\release\GBFRelinkFix.asi"
if (-not (Test-Path $asi)) { throw "Build the mod first (xmake in $modRepo): $asi not found" }
Copy-Item $asi (Join-Path $payload "GBFRelinkFix.asi") -Force
Write-Host "payload: GBFRelinkFix.asi  <- $asi"

Copy-Item (Join-Path $modRepo "GBFRelinkFix.ini") (Join-Path $payload "GBFRelinkFix.ini") -Force
Write-Host "payload: GBFRelinkFix.ini  <- mod repo root"

$winmmTarget = Join-Path $payload "winmm.dll"
if (Test-Path $winmmTarget) {
    Write-Host "payload: winmm.dll already present, keeping it"
} else {
    $fromGame = "C:\Program Files\Steam\steamapps\common\Granblue Fantasy Relink\winmm.dll"
    if (Test-Path $fromGame) {
        Copy-Item $fromGame $winmmTarget
        Write-Host "payload: winmm.dll  <- existing game install"
    } else {
        $url = "https://github.com/ThirteenAG/Ultimate-ASI-Loader/releases/latest/download/Ultimate-ASI-Loader-x64.zip"
        $zip = Join-Path $env:TEMP "ual-x64.zip"
        Write-Host "payload: downloading Ultimate ASI Loader..."
        Invoke-WebRequest $url -OutFile $zip
        $tmp = Join-Path $env:TEMP "ual-extract"
        Expand-Archive $zip $tmp -Force
        Copy-Item (Get-ChildItem $tmp -Filter *.dll -Recurse | Select-Object -First 1).FullName $winmmTarget
        Remove-Item $zip, $tmp -Recurse -Force
        Write-Host "payload: winmm.dll  <- Ultimate ASI Loader release"
    }
}
Write-Host "payload ready: $payload"
