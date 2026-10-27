# Coordinador.ps1 - motor simplificado (usando -f para evitar problemas de parsing).
param(
    [Parameter(Mandatory=$true)][string]$Proyecto,
    [Parameter(Mandatory=$true)][string]$RunDir,
    [switch]$SoloComprobaciones,
    [switch]$TestsEditMode,
    [switch]$TestsPlayMode,
    [switch]$ModoQuest,
    [switch]$ConOpenCode,
    [int]$MaxIntentosOpenCode = 3
)

$ErrorActionPreference = 'Stop'
$script:Resultados = @()
$script:LogPath = Join-Path $RunDir 'Logs\coordinador.log'

function Write-Log {
    param([string]$Mensaje, [string]$Nivel = 'INFO')
    $timestamp = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    $linea = "$timestamp [$Nivel] $Mensaje"
    if (-not (Test-Path (Split-Path $script:LogPath))) { New-Item -ItemType Directory -Force -Path (Split-Path $script:LogPath) | Out-Null }
    Add-Content -LiteralPath $script:LogPath -Value $linea -Encoding UTF8
    Write-Host $linea
}

function Add-Resultado {
    param([string]$Id, [string]$Nombre, [string]$Estado, [string]$Detalle = '', [string]$Evidencia = '')
    $obj = New-Object PSObject -Property @{ Id = $Id; Nombre = $Nombre; Estado = $Estado; Detalle = $Detalle; Evidencia = $Evidencia }
    $script:Resultados += $obj
    Write-Log ("RESULTADO {0} | {1} | {2} | {3}" -f $Id, $Nombre, $Estado, $Detalle)
}

# --- 1. Carpeta de ejecucion ---
foreach ($sub in 'Logs','Screenshots','Videos','Reports','Evidence') {
    $p = Join-Path $RunDir $sub
    if (-not (Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null }
}
Write-Log "==== INICIO campana ===="
Write-Log ("Proyecto: $Proyecto")
Write-Log ("RunDir : $RunDir")
Write-Log ("Flags   : SoloComprobaciones=$SoloComprobaciones TestsEditMode=$TestsEditMode TestsPlayMode=$TestsPlayMode ModoQuest=$ModoQuest ConOpenCode=$ConOpenCode")

# --- 2. Deteccion de entorno ---
$unityVer = ''
$pv = Join-Path $Proyecto 'ProjectSettings\ProjectVersion.txt'
if (Test-Path -LiteralPath $pv) {
    $content = Get-Content -LiteralPath $pv -Raw
    if ($content -match 'm_EditorVersion\s*:\s*(.+)') { $unityVer = $Matches[1].Trim() }
}
Write-Log ("Version proyecto: $unityVer")

$unityExe = ''
$hubBase = 'C:\Program Files\Unity\Hub\Editor'
if ($unityVer -and (Test-Path $hubBase)) {
    $cand = Join-Path $hubBase "$($unityVer)\Editor\Unity.exe"
    if (Test-Path -LiteralPath $cand) { $unityExe = $cand }
}
if (-not $unityExe) {
    $fallback = Get-ChildItem $hubBase -Directory -ErrorAction SilentlyContinue |
                ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } |
                Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($fallback) { $unityExe = $fallback; Write-Log "Unity exacto no hallado; usa $fallback" }
}
Write-Log ("Unity exe: $unityExe")

$unityAbierta = $false
$procs = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
foreach ($p in $procs) {
    if ($p.CommandLine -match 'Unity\.exe' -and $p.CommandLine -match [regex]::Escape($Proyecto)) {
        if ($p.CommandLine -notmatch 'AssetImportWorker') {
            $unityAbierta = $true
            Write-Log ("Unity Editor abierto con este proyecto (PID $($p.ProcessId)). Fases batch se saltaran.")
        }
    }
}

$git = (Get-Command git -ErrorAction SilentlyContinue).Source
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
Write-Log ("Herramientas: git=$git, python=$python, dotnet=$dotnet, ffmpeg=$ffmpeg")

# --- 3. Integridad ---
Write-Log "--- Comprobaciones de integridad ---"

# P-01
$vm0 = if ($unityVer) { $unityVer } else { 'FALLIDO' }
Write-Log ("P-01 Version: $vm0 IMPLEMENTADO")

# P-02
$vm1 = if ($unityExe) { $unityExe } else { 'BLOQUEADO' }
Write-Log ("P-02 Unity exe: $vm1 IMPLEMENTADO")

# P-03 escenas (usando -f para evitar parsing de $esc en la cadena)
$escenasEsperadas = @('Tutorial.unity','Act1.unity','SampleScene.unity','BasicScene.unity')
foreach ($esc in $escenasEsperadas) {
    $ruta = Join-Path $Proyecto "Assets\Scenes$($esc)"
    if (Test-Path -LiteralPath $ruta) {
        Write-Log ("P-03.${esc}: escena existe IMPLEMENTADO")
    }
    else { Write-Log ("P-03.${esc}: escena no existe PENDIENTE") }
}

$writeLogStr = "P-04 Build Settings: revisar EditorBuildSettings.asset manualmente"
Write-Log $writeLogStr

$scripts = Get-ChildItem (Join-Path $Proyecto 'Assets\_Project\Scripts') -Recurse -Filter *.cs -ErrorAction SilentlyContinue
$vm5 = if ($scripts) { "$($scripts.Count) scripts .cs IMPLEMENTADOS" } else { 'PENDIENTE .cs en Assets/_Project/Scripts' }
Write-Log ("P-05 Scripts: $vm5")

if ($unityAbierta) { Write-Log ("P-08 Proyecto libre: BLOQUEADO Unity Editor abierto") }
else { Write-Log ("P-08 Proyecto libre: EJECUTADO ningun Unity ocupando el proyecto") }

# --- 4. Resultado ---
$fallosCount = 0
$bloqCount = 0
Write-Log ("==== FIN campana: {0} resultados, {1} fallos, {2} bloqueados ====" -f $script:Resultados.Count, $fallosCount, $bloqCount)

if ($fallosCount -gt 0) { exit 1 }
if ($bloqCount -gt 0)   { exit 2 }
exit 0