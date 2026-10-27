# UnityBatch.ps1 - invoca Unity en batchmode: compilacion y Unity Test Framework.
# Requiere: Entorno.UnityExe y proyecto sin Editor abierto.
# Estados reales: no se declara APROBADO sin leer exit code + testResults XML.

function Invoke-UnityProceso {
    param([string]$Exe, [string[]]$Args, [string]$LogPath, [int]$TimeoutMin = 30)
    Write-Log ("EJECUTAR: `"$Exe`" " + ($Args -join ' '))
    $p = Start-Process -FilePath $Exe -ArgumentList $Args -PassThru -NoNewWindow -RedirectStandardOutput $LogPath -RedirectStandardError "$LogPath.err"
    $ok = $p.WaitForExit($TimeoutMin * 60000)
    if (-not $ok) {
        try { $p.Kill() } catch {}
        return [pscustomobject]@{ ExitCode = $null; Timeout = $true }
    }
    return [pscustomobject]@{ ExitCode = $p.ExitCode; Timeout = $false }
}

function Ejecutar-UnityBatch {
    param([string]$Proyecto, [hashtable]$Entorno, [switch]$EditMode, [switch]$PlayMode)

    if (-not $Entorno.UnityExe) {
        Add-Resultado 'U-01' 'Compilacion batch de Unity' 'BLOQUEADO' 'No hay Unity.exe disponible'
        return
    }
    if ($Entorno.UnityAbierta) {
        Add-Resultado 'U-01' 'Compilacion batch de Unity' 'BLOQUEADO' 'Unity Editor abierto con el proyecto; cierra el Editor y relanza'
        return
    }

    $logDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'TestResults'
    # logs dentro del RunDir real: el coordinador pasa RunDir via global
    $runDirGlobal = $null
    # RunDir se infiere del log del coordinador
    $runDirGlobal = Split-Path (Join-Path $PSScriptRoot '..\..') -ErrorAction SilentlyContinue

    # --- U-01 compilacion: -quit -batchmode sale con 0 si compila ---
    $compLog = Join-Path $env:TEMP 'crisisvr_compile.log'
    if (Test-Path $compLog) { Remove-Item $compLog -Force }
    $r = Invoke-UnityProceso -Exe $Entorno.UnityExe -LogPath $compLog -TimeoutMin 30 `
        -Args @('-batchmode','-quit','-nographics','-projectPath',"`"$Proyecto`"",'-logFile',"`"$compLog`"")
    if ($r.Timeout) {
        Add-Resultado 'U-01' 'Compilacion batch de Unity' 'FALLIDO' "Timeout 30 min. Log: $compLog"
    } elseif ($r.ExitCode -eq 0) {
        Add-Resultado 'U-01' 'Compilacion batch de Unity' 'APROBADO' "Exit code 0. Log: $compLog"
    } else {
        $errs = @()
        if (Test-Path $compLog) { $errs = Select-String -LiteralPath $compLog -Pattern 'error CS\d+|Scripts have compiler errors' | Select-Object -First 5 }
        $detalle = if ($errs.Count) { ($errs | ForEach-Object { $_.Line.Trim() }) -join ' | ' } else { "Exit code $($r.ExitCode) sin 'error CS' claro" }
        Add-Resultado 'U-01' 'Compilacion batch de Unity' 'FALLIDO' $detalle "Log: $compLog"
    }

    # --- U-02/U-03 Unity Test Framework ---
    if ($EditMode -or $PlayMode) {
        # Las pruebas deben existir primero; detectar carpeta de tests
        $testAsm = Get-ChildItem (Join-Path $Proyecto 'Assets') -Recurse -Filter *.asmdef -ErrorAction SilentlyContinue |
                   Where-Object { $_.Name -match 'Tests?' } | Select-Object -First 1
        if (-not $testAsm) {
            Add-Resultado 'U-02' 'Unity Test Framework (asmdef de tests)' 'PENDIENTE' 'No hay ningun .asmdef de tests bajo Assets; anade pruebas UTF primero'
        } else {
            foreach ($modo in @(@($EditMode,'EditMode','U-02'), @($PlayMode,'PlayMode','U-03'))) {
                if (-not $modo[0]) { continue }
                $nombre = $modo[1]; $id = $modo[2]
                $resXml = Join-Path $env:TEMP ("crisisvr_tests_{0}.xml" -f $nombre.ToLower())
                $tLog   = Join-Path $env:TEMP ("crisisvr_tests_{0}.log" -f $nombre.ToLower())
                $r2 = Invoke-UnityProceso -Exe $Entorno.UnityExe -LogPath $tLog -TimeoutMin 45 `
                    -Args @('-batchmode','-nographics','-projectPath',"`"$Proyecto`"",
                            '-runTests','-testPlatform',$nombre,'-testResults',"`"$resXml`"",'-logFile',"`"$tLog`"")
                if ($r2.Timeout) {
                    Add-Resultado $id "Unity Test Framework $nombre" 'FALLIDO' "Timeout 45 min. Log: $tLog"
                    continue
                }
                if (Test-Path $resXml) {
                    try {
                        [xml]$xml = Get-Content -LiteralPath $resXml -Raw
                        $total   = [int]$xml.'test-run'.total
                        $passed  = [int]$xml.'test-run'.passed
                        $failed  = [int]$xml.'test-run'.failed
                        $skipped = [int]$xml.'test-run'.skipped
                        $estado = if ($failed -gt 0) { 'FALLIDO' } elseif ($passed -gt 0) { 'APROBADO' } else { 'EJECUTADO' }
                        Add-Resultado $id "Unity Test Framework $nombre" $estado `
                            ("{0} tests: {1} aprobados, {2} fallidos, {3} omitidos (exit {4})" -f $total,$passed,$failed,$skipped,$r2.ExitCode) $resXml
                    } catch {
                        Add-Resultado $id "Unity Test Framework $nombre" 'FALLIDO' "XML ilegible: $resXml"
                    }
                } else {
                    # UTF devuelve exit 3 cuando encuentra errores de compilacion, 2 con fallos
                    $estado = if ($r2.ExitCode -eq 2) { 'FALLIDO' } else { 'BLOQUEADO' }
                    Add-Resultado $id "Unity Test Framework $nombre" $estado "Sin testResults XML; exit code $($r2.ExitCode). Log: $tLog"
                }
            }
        }
    }
}
