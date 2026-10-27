$ErrorActionPreference = 'Stop'
$dest = 'C:\Users\USER\CrisisVR\LAB9\EVIDENCIAS'
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }

# Evidencia 1: resultado XML real de las 8 pruebas
Copy-Item 'C:\Users\USER\CrisisVR\TestResults\Lab9\tests_editmode.xml' -Destination (Join-Path $dest 'tests_editmode.xml') -Force

# Evidencia 2: log completo de la ejecucion (incluye la compilacion del script EditorBuildSetup)
Copy-Item 'C:\Users\USER\CrisisVR\TestResults\Lab9\editmode.log' -Destination (Join-Path $dest 'editmode.log') -Force

# Evidencia 3: scripts con los que se ejecuto y se verifico
foreach ($f in @('lab9_copiar.ps1','lab9_lanzar.ps1','lab9_xml.ps1','lab9_m3.ps1','lab9_compiled.ps1')) {
    $src = Join-Path 'C:\Users\USER\CrisisVR\TestResults' $f
    if (Test-Path $src) { Copy-Item $src -Destination (Join-Path $dest $f) -Force }
}

Write-Output '--- LAB9\EVIDENCIAS ---'
Get-ChildItem $dest | ForEach-Object {
    $kb = '' + [math]::Round($_.Length/1KB,1)
    Write-Output ('  ' + $_.Name.PadRight(24) + $kb.PadLeft(9) + ' KB   ' + $_.LastWriteTime)
}

Write-Output ''
Write-Output '--- lineas clave del log (pruebas y build) ---'
$log = Join-Path $dest 'editmode.log'
Select-String -Path $log -Pattern 'CrisisVR: build actualizado|Test run completed|Exiting batchmode successfully|OnPostprocessAllAssets.*Assembly-CSharp' -ErrorAction SilentlyContinue |
    Select-Object -First 6 | ForEach-Object { Write-Output ('  ' + $_.Line.Trim()) }
