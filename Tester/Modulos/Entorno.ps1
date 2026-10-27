# Entorno.ps1 - deteccion de herramientas reales instaladas.
# Devuelve hashtable con rutas verificadas (o vacio si falta). Sin inventar rutas.

function Detectar-Entorno {
    param([string]$Proyecto, [string]$Log)

    $e = @{
        UnityVersionProyecto = ''
        UnityExe             = ''
        Git                  = ''
        Python               = ''
        Dotnet               = ''
        Adb                  = ''
        Ffmpeg               = ''
        OpenCode             = ''
        UnityAbierta         = $false
        Notas                = @()
    }

    # Version del proyecto
    $pv = Join-Path $Proyecto 'ProjectSettings\ProjectVersion.txt'
    if (Test-Path -LiteralPath $pv) {
        $linea = Get-Content -LiteralPath $pv | Where-Object { $_ -match 'm_EditorVersion' } | Select-Object -First 1
        if ($linea -match 'm_EditorVersion\s*(.+)') { $e.UnityVersionProyecto = $Matches[1].Trim() }
    }

    # Unity instalado
    $hubBase = 'C:\Program Files\Unity\Hub\Editor'
    if ($e.UnityVersionProyecto -and (Test-Path $hubBase)) {
        $candidato = Join-Path $hubBase "$($e.UnityVersionProyecto)\Editor\Unity.exe"
        if (Test-Path -LiteralPath $candidato) { $e.UnityExe = $candidato }
    }
    if (-not $e.UnityExe -and (Test-Path $hubBase)) {
        $otra = Get-ChildItem $hubBase -Directory -ErrorAction SilentlyContinue |
                ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } |
                Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if ($otra) {
            $e.UnityExe = $otra
            $e.Notas += "Unity exacto no hallado; usa $otra (version distinta)."
        }
    }

    # Herramientas en PATH
    foreach ($h in @('git','python','dotnet','ffmpeg')) {
        $cmd = Get-Command $h -ErrorAction SilentlyContinue
        if ($cmd) { $e[$h] = $cmd.Source }
    }
    $e.Git    = (Get-Command git    -ErrorAction SilentlyContinue).Source
    $e.Python = (Get-Command python -ErrorAction SilentlyContinue).Source
    $e.Dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    $e.Ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source

    # ADB dentro del Android SDK de Unity
    if ($e.UnityExe) {
        $adbCand = Join-Path (Split-Path (Split-Path $e.UnityExe -Parent) -Parent) 'Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
        if (Test-Path -LiteralPath $adbCand) { $e.Adb = $adbCand }
    }
    if (-not $e.Adb) {
        $e.Adb = (Get-Command adb -ErrorAction SilentlyContinue).Source
    }

    # OpenCode CLI
    $oc = Get-Command opencode -ErrorAction SilentlyContinue
    if ($oc) {
        $ver = & cmd /c "opencode --version 2>&1"
        if ($LASTEXITCODE -eq 0 -and $ver -match 'opencode\s+v?') {
            $e.OpenCode = 'cmd'
            $e.Notas += "OpenCode CLI detectado ($ver) - se invoca via cmd.exe."
        } else {
            $e.Notas += "OpenCode encontrado pero --version fallo."
        }
    }

    # Unity abierto EN este proyecto?
    $procs = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
    foreach ($p in $procs) {
        if ($p.CommandLine -match 'Unity\.exe' -and $p.CommandLine -match [regex]::Escape($Proyecto)) {
            if ($p.CommandLine -notmatch 'AssetImportWorker') {
                $e.UnityAbierta = $true
                $e.Notas += "Unity Editor esta ABIERTO con este proyecto (PID $($p.ProcessId)). Las fases batch se saltaran."
            }
        }
    }

    return $e
}