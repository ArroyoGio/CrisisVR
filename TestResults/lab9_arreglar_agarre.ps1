$ErrorActionPreference = 'Stop'
$proj = 'C:\Users\USER\CrisisVR'
$out  = 'C:\Users\USER\CrisisVR\TestResults\Lab9'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out | Out-Null }
$log = Join-Path $out 'arreglo_agarre.log'
$argumentos = @('-batchmode','-nographics','-accept-apiupdate','-projectPath',$proj,'-executeMethod','CrisisVR.Editor.TutorialSimulatorSetup.ArreglarAgarreBatch','-logFile',$log)
$p = Start-Process -FilePath $unity -ArgumentList $argumentos -PassThru -WindowStyle Hidden
Set-Content -Path (Join-Path $out 'unity_pid_fix.txt') -Value $p.Id
Write-Output ('Unity arreglo lanzado pid=' + $p.Id)
Write-Output ('log=' + $log)
