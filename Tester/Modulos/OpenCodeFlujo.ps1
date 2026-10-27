# OpenCodeFlujo.ps1 - integra OpenCode para corregir fallos detectados.
# Lanza opencode run con limites de intentos, captura cambios de archivos
# y deja evidencias para revisión manual. Opción `-ConOpenCode` obligatoria.

function Invoke-OpenCodeFlujo {
    param([string]$Proyecto, [string]$RunDir, [array]$Fallos, [int]$MaxIntentos = 3,
          [hashtable]$Entorno)

    $evidenceDir = Join-Path $RunDir 'Evidence'
    if (-not (Test-Path $evidenceDir)) { New-Item -ItemType Directory -Force -Path $evidenceDir } | Out-Null

    # Preparar evidencia de los fallos actuales
    $resumenFallas = $Fallos | ForEach-Object { "$($_.Id): $($_.Nombre) - $($_.Detalle)" } -join "`n"
    $evidenceFile = Join-Path $evidenceDir 'incidencia_actual.txt'
    $resumenFallas | Out-File -LiteralPath $evidenceFile -Encoding UTF8

    $fallosRestantes = @($Fallos)
    $intentoOk = $false

    for ($i = 1; $i -le $MaxIntentos; $i++) {
        Write-Log ("--- Intento de OpenCode #$i / $MaxIntentos ---")
        # Construir prompt para OpenCode: logs resumidos + resumen de fallos
        $promptBreve = "Soy OpenCode en Crisis VR. Necesito corregir los siguientes errores de prueba detectados.
Resumen de fallos actuales:
$resumenFallas

Instrucciones: analiza los logs del proyecto (consultables en el directorio TestResults\<run>\Logs\), sugiere cambios minimalistas en el codigo fuente o la configuracion, no elimina archivos, no cambia pruebas para ocultar fallos, no desactive funcionalidades sin autorizacion. Devuelve SOLO el codigo o configuracion sugerida. Responde con formato JSON: { cambios: [ { archivo, cambios } ], observaciones: texto } . Si no hallas solucion, devuelve { cambios: [], observaciones: \"no se pudo corregir\" }."

        # Invocar via cmd (el .ps1 no ejecuta opencode por tema de ExecutionPolicy)
        $argFile = Join-Path $evidenceDir "prompt_intento_$i.txt"
        $promptBreve | Out-File -LiteralPath $argFile -Encoding UTF8

        # cmd /c opencode run --auto --title crisis-vr-tester --file $argFile "responde OK"
        # Timeout 120 seg.
        $procArgs = '/c', 'opencode', 'run', '--auto', '--title', 'crisis-vr-tester', '--file', $argFile, 'responde OK'
        $p = Start-Process -FilePath 'cmd.exe' -ArgumentList $procArgs -PassThru -NoNewWindow
        # Esperar con timeout aproximado
        $esperado = 120000
        $esperadoOk = $p.WaitForExit($esperado)
        if (-not $esperadoOk) { $p.Kill() }
        $salida = $p.StandardOutput.ReadToEnd()
        $err  = $p.StandardError.ReadToEnd()
        $salidaCompleta = $salida + $err

        Write-Log ("Output OpenCode intento $i: $($salidaCompleta.Substring(0,200))")

        # Intentar parsear JSON devuelto
        $jsonResult = $null
        try { $jsonResult = $salidaCompleta | ConvertFrom-Json -ErrorAction Stop } catch { }
        if ($jsonResult) {
            $cambios = $jsonResult.cambios -or @()
            $obs = $jsonResult.observaciones -or ''
            # Guardar propuestas
            $propFile = Join-Path $evidenceDir "opencode_propuesta_$i.txt"
            $cambios | Out-File -LiteralPath $propFile -Encoding UTF8
            $obs | Out-File -LiteralPath ($propFile + '.obs') -Encoding UTF8

            # Aplicar cambios detectados (solo si parecen seguros: solo lectura, no tocan test ni assets críticos)
            # Por ahora solo anota y pasa al siguiente intento o marca ok si cero cambios
            if ($cambios.Count -eq 0 -or $cambios -match 'no se pudo corregir') {
                $fallosRestantes = $fallosRestantes | Where-Object { $_.Id -notin @($cambios) }
                if ($fallosRestantes.Count -eq 0) { $intentoOk = $true }
                Write-Log ("Intento $i: OpenCode no hallo cambio; fallos restantes: $($fallosRestantes.Count)")
                break
            } else {
                # Marcar intento como aplicado (placeholder: no autoprocesamos cambios reales para evitar romper)
                $obs | Out-File -LiteralPath ($propFile + '.aplicado') -Encoding UTF8
                Write-Log ("Intento $i: OpenCode sugirió cambios; sin embargo se omitio su aplicacion automatica por seguridad.")
                $fallosRestantes = @($Fallos)
                Write-Log "    -> Se dejaron los cambios al usuario (OpenCode) para revisar manualmente."
                break
            }
        } else {
            Write-Log ("Intento $i: OpenCode no retorno JSON valido. Salida: $($salidaCompleta.Substring(0,150))")
            $fallosRestantes = $fallosRestantes
        }

        # Pequeña pausa entre intentos
        Start-Sleep -Seconds 5
    }

    # Informe final de OpenCode
    $reporte = Join-Path $evidenceDir 'reporte_openCode.txt'
    "$i intentos realizados. Fallos resueltos: $($intentoOk -and $fallosRestantes.Count -eq 0); Fallos restantes: $($fallosRestantes.Count)" | Out-File -LiteralPath $reporte -Encoding UTF8
    $fallosRestantes | ForEach-Object { "$($_.Id) $($_.Nombre): $($_.Detalle)" } | Out-File -LiteralPath $reporte -Encoding UTF8 -Append

    if ($intentoOk) {
        Add-Resultado 'OC-01' 'Integracion OpenCode' 'APROBADO' "Correccion asistida exitosa en $MaxIntentos intentos"
    } else {
        Add-Resultado 'OC-01' 'Integracion OpenCode' 'BLOQUEADO' "No se corrigieron todos los fallos en $MaxIntentos intentos; revisar manualmente $evidenceFile"
    }
}