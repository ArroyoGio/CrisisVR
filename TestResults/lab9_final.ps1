$ErrorActionPreference = 'Stop'
$copia = Join-Path $env:TEMP 'opencode\CrisisVR_LAB9'
if (Test-Path $copia) {
    Remove-Item $copia -Recurse -Force -ErrorAction SilentlyContinue
    Write-Output ('copia temporal eliminada: ' + $copia + ' -> ' + (Test-Path $copia))
} else { Write-Output 'copia temporal ya no existia' }

Write-Output ''
Write-Output '=== LAB9 (entrega) ==='
Get-ChildItem 'C:\Users\USER\CrisisVR\LAB9' | ForEach-Object {
    if ($_.PSIsContainer) {
        $n = (Get-ChildItem $_.FullName -Recurse -File).Count
        Write-Output ('  [carpeta] ' + $_.Name + '  (' + $n + ' archivos)')
    } else {
        Write-Output ('           ' + $_.Name)
    }
}
Write-Output ''
Write-Output '=== respaldo dentro de RESPALDO ==='
Get-ChildItem 'C:\Users\USER\CrisisVR\LAB9\RESPALDO' | ForEach-Object { Write-Output ('  ' + $_.Name) }
Write-Output ''
Write-Output '=== codigo tocado ==='
foreach ($f in @(
    'C:\Users\USER\CrisisVR\Assets\_Project\Scripts\Dialogue\TutorialPanel.cs',
    'C:\Users\USER\CrisisVR\Assets\_Project\Tests\EditMode\TutorialFunctionalTests.cs',
    'C:\Users\USER\CrisisVR\Assets\_Project\Scripts\Build\Editor\EditorBuildSetup.cs'
)) {
    $i = Get-Item $f
    Write-Output ('  ' + $i.Name.PadRight(30) + [string]$i.Length + ' bytes  ' + $i.LastWriteTime)
}
