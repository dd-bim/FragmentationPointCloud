param(
  [string]$BundleFolder = ".\Green3DScan.bundle"
)

function Test-Admin {
  $current = [Security.Principal.WindowsIdentity]::GetCurrent()
  (New-Object Security.Principal.WindowsPrincipal($current)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$target = if (Test-Admin) {
  Join-Path $env:ProgramData "Autodesk\ApplicationPlugins"
} else {
  Join-Path $env:AppData "Autodesk\ApplicationPlugins"
  Write-Host "Keine Admin-Rechte: Installation pro Benutzer."
}

if (!(Test-Path $BundleFolder)) { throw "BundleFolder '$BundleFolder' nicht gefunden." }

New-Item -ItemType Directory -Path $target -Force | Out-Null
$dest = Join-Path $target "Green3DScan.bundle"

if (Test-Path $dest) {
  Write-Host "Altes Bundle wird ersetzt..."
  Remove-Item $dest -Recurse -Force
}

Copy-Item $BundleFolder $dest -Recurse
Write-Host "Installiert nach: $dest"
Write-Host "Fertig. Revit neu starten."