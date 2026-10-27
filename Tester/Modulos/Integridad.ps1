# Integridad.ps1 - comprobaciones iniciales del proyecto SIN invocar Unity.
# Usa Add-Resultado y Write-Log definidos en Coordinador.ps1 (scope global).

function Probar-Integridad {
    param([string]$Proyecto, [hashtable]$Entorno)

    # P-01 Version del proyecto
    if ($Entorno.UnityVersionProyecto) {
        Add-Resultado 'P-01' 'Version de Unity del proyecto' 'IMPLEMENTADO' $Entorno.UnityVersionProyecto
    } else {
        Add-Resultado 'P-01' 'Version de Unity del proyecto' 'FALLIDO' 'No se pudo leer ProjectSettings\ProjectVersion.txt'
    }

    # P-02 Unity instalado coincide
    if ($Entorno.UnityExe) {
        Add-Resultado 'P-02' 'Unity ejecutable disponible' 'IMPLEMENTADO' $Entorno.UnityExe
    } else {
        Add-Resultado 'P-02' 'Unity ejecutable disponible' 'BLOQUEADO' "No se hallo Unity.exe para version $($Entorno.UnityVersionProyecto). Instala esa version desde Unity Hub."
    }

    # P-03 Escenas reales del juego (las que existen hoy)
    $escenasEsperadas = @('Tutorial.unity','Act1.unity','SampleScene.unity','BasicScene.unity')
    foreach ($esc in $escenasEsperadas) {
        $ruta = Join-Path $Proyecto "Assets\Scenes\$esc"
        if (Test-Path -LiteralPath $ruta) {
            Add-Resultado ("P-03." + $esc) "Escena $esc existe" 'IMPLEMENTADO' "Assets/Scenes/$esc"
        } else {
            Add-Resultado ("P-03." + $esc) "Escena $esc existe" 'PENDIENTE' "No encontrada en Assets/Scenes/$esc"
        }
    }

    # P-04 Escenas en Build Settings (solo SampleScene habilitada segun inspeccion previa)
    $ebs = Join-Path $Proyecto 'ProjectSettings\EditorBuildSettings.asset'
    $escenasBuild = @()
    if (Test-Path -LiteralPath $ebs) {
        $escenasBuild = Get-Content -LiteralPath $ebs | Where-Object { $_ -match 'path:\s*(.+\.unity)' } | ForEach-Object { $_ -replace '.*path:\s*','' }
    }
    Add-Resultado 'P-04' 'Escenas en Build Settings' 'IMPLEMENTADO' ("{0} escena(s): {1}" -f $escenasBuild.Count, ($escenasBuild -join ', '))

    # P-05 Scripts de juego reales bajo Assets/_Project
    $scripts = Get-ChildItem (Join-Path $Proyecto 'Assets\_Project\Scripts') -Recurse -Filter *.cs -ErrorAction SilentlyContinue
    if ($scripts -and $scripts.Count -gt 0) {
        Add-Resultado 'P-05' 'Scripts del proyecto (Assets/_Project/Scripts)' 'IMPLEMENTADO' "$($scripts.Count) scripts .cs"
    } else {
        Add-Resultado 'P-05' 'Scripts del proyecto (Assets/_Project/Scripts)' 'PENDIENTE' 'No hay scripts .cs en Assets/_Project/Scripts'
    }

    # P-06 Compilacion: error de compilacion previo en Logs del proyecto
    $logDir = Join-Path $Proyecto 'Logs'
    $erroresCS = @()
    if (Test-Path -LiteralPath $logDir) {
        $logs = Get-ChildItem $logDir -Filter *.log -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 3
        foreach ($lg in $logs) {
            $hits = Select-String -LiteralPath $lg.FullName -Pattern 'error CS\d+' -ErrorAction SilentlyContinue | Select-Object -Last 5
            $erroresCS += $hits
        }
    }
    if ($erroresCS.Count -gt 0) {
        $muestra = ($erroresCS | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique -First 5) -join ' | '
        Add-Resultado 'P-06' 'Errores de compilacion CS en logs recientes' 'FALLIDO' $muestra "Logs\ (3 logs recientes)"
    } else {
        Add-Resultado 'P-06' 'Errores de compilacion CS en logs recientes' 'EJECUTADO' 'Sin "error CS" en los 3 logs mas recientes de Logs\ (no sustituye a una compilacion batch)'
    }

    # P-07 Paquetes XR presentes en manifest.json
    $manifest = Join-Path $Proyecto 'Packages\manifest.json'
    if (Test-Path -LiteralPath $manifest) {
        $txt = Get-Content -LiteralPath $manifest -Raw
        $faltan = @()
        foreach ($pkg in @('com.unity.xr.openxr','com.unity.xr.meta-openxr','com.unity.inputsystem')) {
            if ($txt -notmatch [regex]::Escape($pkg)) { $faltan += $pkg }
        }
        if ($faltan.Count -eq 0) {
            Add-Resultado 'P-07' 'Paquetes XR obligatorios en manifest' 'IMPLEMENTADO' 'openxr, meta-openxr e inputsystem presentes'
        } else {
            Add-Resultado 'P-07' 'Paquetes XR obligatorios en manifest' 'BLOQUEADO' ("Faltan: {0}. Agregalos via Package Manager (tarea de la desarrolladora)." -f ($faltan -join ', '))
        }
    }

    # P-08 Unity abierto impide fases batch
    if ($Entorno.UnityAbierta) {
        Add-Resultado 'P-08' 'Proyecto libre para pruebas batch' 'BLOQUEADO' 'Unity Editor abierto con este proyecto; cierralo para compilar/tests por CLI'
    } else {
        Add-Resultado 'P-08' 'Proyecto libre para pruebas batch' 'EJECUTADO' 'Ningun Unity.exe ocupando el proyecto'
    }

    # Notas del entorno (informativas)
    foreach ($n in $Entorno.Notas) { Write-Log "NOTA ENTORNO: $n" }
}
