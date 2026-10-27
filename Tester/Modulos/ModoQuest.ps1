# ModoQuest.ps1 - modo separado para Meta Quest 3 fisico.
# Solo detecta/recoge; NUNCA instala en un visor no conectado.

function Ejecutar-ModoQuest {
    param([string]$Proyecto, [hashtable]$Entorno)

    # Q-01 ADB disponible
    if (-not $Entorno.Adb) {
        Add-Resultado 'Q-01' 'ADB disponible' 'BLOQUEADO' 'No se encontro adb.exe (dentro del Android SDK de Unity ni en PATH)'
        return
    }
    Add-Resultado 'Q-01' 'ADB disponible' 'IMPLEMENTADO' $Entorno.Adb

    # Q-02 Dispositivo conectado (solo lectura)
    $devices = & $Entorno.Adb devices 2>&1
    Write-Log ("EJECUTAR: `"$($Entorno.Adb)`" devices -> " + ($devices -join ' / '))
    $conectados = @($devices | Where-Object { $_ -match '^\S+\s+device$' })
    if ($conectados.Count -eq 0) {
        Add-Resultado 'Q-02' 'Quest 3 conectada (adb devices)' 'BLOQUEADO' 'Sin dispositivo en modo depuracion. Conecta el Quest 3, acepta la depuracion USB y relanza con -ModoQuest'
        return
    }
    Add-Resultado 'Q-02' 'Quest 3 conectada (adb devices)' 'EJECUTADO' ($conectados -join ', ')

    # Q-03 Build APK: existe compilacion Android previa?
    $apk = Get-ChildItem (Join-Path $Proyecto 'Builds') -Filter *.apk -Recurse -ErrorAction SilentlyContinue |
           Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $apk) {
        Add-Resultado 'Q-03' 'APK compilado presente' 'BLOQUEADO' 'No hay ningun .apk en Builds\. Compila para Android desde el Editor (Build Profiles) antes de instalar'
        return
    }
    Add-Resultado 'Q-03' 'APK compilado presente' 'IMPLEMENTADO' $apk.FullName

    # Q-04 Instalacion + lanzamiento (requiere APK + dispositivo)
    $pkg = 'com.DefaultCompany.CrisisVR'  # VERIFICAR: nombre de paquete real en ProjectSettings > Player
    $inst = & $Entorno.Adb install -r "`"$($apk.FullName)`"" 2>&1
    Write-Log ("EJECUTAR: adb install -r -> " + ($inst -join ' / '))
    if (($inst -join ' ') -match 'Success') {
        Add-Resultado 'Q-04' 'Instalacion en Quest' 'APROBADO' "Instalado: $($apk.Name)"
        # Q-05 lanzar app + recoger logcat breve
        & $Entorno.Adb shell monkey -p $pkg -c android.intent.category.LAUNCHER 1 2>&1 | Out-Null
        Start-Sleep -Seconds 8
        $logcat = Join-Path $Proyecto 'TestResults' 'quest_logcat.txt'
        & $Entorno.Adb logcat -d -t 300 2>&1 | Out-File -LiteralPath $logcat -Encoding UTF8
        Add-Resultado 'Q-05' 'Logcat recogido tras lanzar' 'EJECUTADO' "Ultimas ~300 lineas en $logcat" $logcat
    } else {
        Add-Resultado 'Q-04' 'Instalacion en Quest' 'FALLIDO' (($inst -join ' ') -replace '\s+',' ')
    }
}
