$ErrorActionPreference = 'Stop'
$proj = 'C:\Users\USER\AppData\Local\Temp\opencode\CrisisVR_LAB9'
$out  = 'C:\Users\USER\CrisisVR\TestResults\Lab9'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out | Out-Null }
$log = Join-Path $out 'editmode.log'
$xml = Join-Path $out 'tests_editmode.xml'
if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $xml) { Remove-Item $xml -Force }
$argumentos = @('-batchmode','-nographics','-accept-apiupdate','-projectPath',$proj,'-runTests','-testPlatform','EditMode','-testResults',$xml,'-logFile',$log)
$p = Start-Process -FilePath $unity -ArgumentList $argumentos -PassThru -WindowStyle Hidden
Set-Content -Path (Join-Path $out 'unity_pid.txt') -Value $p.Id
Write-Output ('Unity lanzado pid=' + $p.Id)
Write-Output ('log=' + $log)
