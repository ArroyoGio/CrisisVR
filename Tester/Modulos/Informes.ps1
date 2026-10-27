# Informes.ps1 - genera el reporte final en Markdown y CSV por cada ejecucion.

function Generar-Informes {
    param([string]$Proyecto, [string]$RunDir, [system.collections.arraylist]$Resultados,
          [hashtable]$Entorno)

    $mdPath = Join-Path $RunDir 'Reports\Informe.md'
    $csvPath = Join-Path $RunDir 'Resumen.csv'

    # CSV header y filas
    $csvLineas = @()
    $csvLineas += 'ID,Nombre,Estado,Detalle,Evidencia'
    foreach ($r in $Resultados) {
        $ev = $r.Evidencia
        if ($ev -and $ev -match 'TestResults\\') { $ev = 'TR\' + $ev -replace 'TestResults\\', '' }
        else { if ($ev.Length -gt 40) { $ev = $ev.Substring(0,37) + '...' } }
        $csvLinea = "$($r.Id),$($r.Nombre),$($r.Estado),$($r.Detalle),$ev"
        $csvLineas += $csvLinea
    }
    $csvLineas | Out-File -LiteralPath $csvPath -Encoding UTF8

    # Determinar cadenas simples sin ternary
    $unidad = 'N/A'
    if ($Entorno.UnityExe) { $unidad = Split-Path $Entorno.UnityExe -Leaf }
    $openCodeOk = 'NO'
    if ($Entorno.OpenCode) { $openCodeOk = 'SÍ (CLI via cmd)' }
    $questOk = 'NO'
    if ($Entorno.Adb) { $questOk = 'SÍ (ADB)' }
    $editorAbierto = 'NO'
    if ($Entorno.UnityAbierta) { $editorAbierto = 'SÍ' }

    $fallosCount = 0
    $bloqCount = 0
    $pendCount = 0
    foreach ($r in $Resultados) {
        if ($r.Estado -eq 'FALLIDO') { $fallosCount++ }
        if ($r.Estado -eq 'BLOQUEADO') { $bloqCount++ }
        if ($r.Estado -eq 'PENDIENTE') { $pendCount++ }
    }

    # Reporte Markdown - construir linea por linea
    $lineasMD = @()
    $lineasMD += "# Informe de Pruebas - Crisis VR"
    $lineasMD += "**Fecha:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $lineasMD += "**Proyecto:** $Proyecto"
    $versUnidad = $unidad
    $lineasMD += "**Versión Unity:** $versUnidad"
    $plataforma = 'PC (Simulador XR)'
    if ($editorAbierto -eq 'SÍ') { $plataforma += ' (Editor abierto)' }
    $lineasMD += "**Plataforma:** $plataforma"
    $lineasMD += "**Herramientas detectadas:**"
    $lineasMD += "- Unity: $unidad"
    $lineasMD += "- Git: $(if ($Entorno.Git) { 'Instalado' } else { 'No encontrado' })"
    $lineasMD += "- Python: $(if ($Entorno.Python) { 'Instalado' } else { 'No encontrado' })"
    $lineasMD += "- ADB/Quest: $questOk"
    $lineasMD += "- ffmpeg (video): $(if ($Entorno.Ffmpeg) { 'Instalado' } else { 'No instalado - video PENDIENTE (ver README)' })"
    $lineasMD += "- OpenCode CLI: $openCodeOk"

    $lineasMD += ""
    $lineasMD += "## Resumen de resultados"
    $lineasMD += "| ID | Nombre | Estado | Detalle |"
    $lineasMD += "|----|--------|--------|---------|"

    $noPendientes = $Resultados | Where-Object { $_.Estado -ne 'PENDIENTE' }
    foreach ($r in $noPendientes) {
        $det = $r.Detalle -replace '"', ''
        $ev = $r.Evidencia -replace '"', ''
        $lineasMD += ("| `{0}` | `{1}` | `{2}` | `{3}` |" -f $r.Id,$r.Nombre,$r.Estado,$det) + "`n"
    }

    $lineasMD += ""
    $pendientes = $Resultados | Where-Object { $_.Estado -eq 'PENDIENTE' }
    if ($pendientes.Count -gt 0) {
        $lineasMD += "`n## Pendientes por verificar o implementar`n"
        foreach ($r in $pendientes) {
            $lineasMD += ("- **{0}** - {1}`n" -f $r.Nombre,$r.Detalle) + "`n"
        }
    } else {
        $lineasMD += "`n## Pendientes por verificar o implementar`nningún pendiente.`n"
    }

    $lineasMD += "`n## Evidencias disponibles`n"
    $dirs = @('Logs','Screenshots','Videos','Reports','Evidence')
    foreach ($d in $dirs) {
        $p = Join-Path $RunDir $d
        if (Test-Path $p) { $lineasMD += ("- Carpeta: `$p` (contiene registros de la ejecucion)`n" ) }
    }

    $lineasMD += "`n## Problemas que requieren intervención humana`n"
    $humanos = @()
    foreach ($r in $Resultados) {
        if ($r.Estado -eq 'BLOQUEADO') { $humanos += "$($r.Id) $($r.Nombre): $($r.Detalle)" }
        if ($r.Estado -eq 'FALLIDO') { $humanos += "$($r.Id) $($r.Nombre): $($r.Detalle)" }
    }
    if ($humanos.Count -gt 0) {
        $lineasMD += ($humanos -join "`n") + "`n"
    } else {
        $lineasMD += 'Ningún problema crítico requiere intervención humana.`n'
    }

    $lineasMD += "`n## Próximos pasos recomendados`n"
    $pasos = @()
    $pasos += "- Ejecutar pruebas con Unity Test Framework (requiere cerrar Editor Unity)."
    $pasos += "- Instalar ffmpeg si se desean capturas de vídeo (`winget install ffmpeg`)."
    $pasos += "- Conectar Quest 3 y usar -ModoQuest para instalacion y logcat."
    $pasos += "- Si es necesario, OpenCode manual con revision de cambios antes de re-ejecutar pruebas."
    $pasos += "- Corregir fallos detectados y repetir campana para validar regresiones."
    $lineasMD += ($pasos -join "`n") + "`n`

    $lineasMD += "## Resumen CSV`n"
    $lineasMD += "Ver archivo `Resumen.csv` para datos estructurados.`n"

    $mdText = $lineasMD -join "`n"
    $mdText | Out-File -LiteralPath $mdPath -Encoding UTF8
    Write-Log ("Informes generados: $mdPath y $csvPath")
}