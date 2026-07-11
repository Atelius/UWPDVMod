# Builds the single self-contained UWPDVMod.exe.
# Refreshes the embedded Granblue payload first so the exe ships the latest mod build.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

& (Join-Path $root "scripts\fetch-granblue-payload.ps1")

$out = Join-Path $root "publish"
dotnet publish (Join-Path $root "src\UWPDVMod.App") `
    -c Release -r win-x64 --self-contained `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=none /p:DebugSymbols=false `
    -o $out

Write-Host ""
Write-Host "Single-file exe: $(Join-Path $out 'UWPDVMod.exe')"
