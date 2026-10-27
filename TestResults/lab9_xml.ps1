$ErrorActionPreference = 'Stop'
$xmlPath = 'C:\Users\USER\CrisisVR\TestResults\Lab9\tests_editmode.xml'
[xml]$x = Get-Content $xmlPath
$r = $x.'test-run'
Write-Output ('total=' + $r.total + ' aprobadas=' + $r.passed + ' fallidas=' + $r.failed + ' omitidas=' + $r.skipped + ' tiempo=' + $r.time + 's')
Write-Output ('fecha=' + $r.start + '  platform=' + $r.platform + '  nunit=' + $r['nunit-version'])
Write-Output ''
Write-Output '--- detalle por prueba ---'
foreach ($t in $r.SelectNodes('//test-case')) {
    Write-Output ('  ' + $t.result.PadRight(8) + ' ' + $t.name + ' (' + $t.time + 's)')
}
