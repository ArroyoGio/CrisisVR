$gdd = 'C:\Users\USER\CrisisVR\docs\GDD.txt'
$lineas = Get-Content $gdd
foreach ($r in @(@(542,718), @(719,924), @(925,975))) {
    Write-Output ('===== ' + $r[0] + '-' + $r[1] + ' =====')
    $i = $r[0]
    while ($i -le $r[1]) {
        $l = $lineas[$i-1]
        if ($l -match 'Variable|Opción|Opcin|Reintento|Adaptabilidad|Equipo|Tiempo de espera|tiempo de espera|Avanza|Paso|T[1-9]\b|COMENZAR|Registrar|Se registra') {
            Write-Output ($i.ToString() + ': ' + $l)
        }
        $i++
    }
}
