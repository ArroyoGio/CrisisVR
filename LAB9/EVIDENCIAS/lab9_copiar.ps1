$ErrorActionPreference = 'Stop'
$src = 'C:\Users\USER\CrisisVR'
$dst = 'C:\Users\USER\AppData\Local\Temp\opencode\CrisisVR_LAB9'

if (Test-Path $dst) {
    Write-Output 'Destino ya existe; se reutiliza.'
} else {
    New-Item -ItemType Directory -Path $dst | Out-Null
}

foreach ($p in @('Assets','Packages','ProjectSettings')) {
    Write-Output ('Copiando ' + $p + ' ...')
    $r = robocopy (Join-Path $src $p) (Join-Path $dst $p) /E /NFL /NDL /NJH /NJS /R:1 /W:1 /MT:8
    Write-Output ('  robocopy exit=' + $LASTEXITCODE)
}

Write-Output ''
Write-Output ('Assets copiados: ' + (Get-ChildItem (Join-Path $dst 'Assets') -Recurse -File -ErrorAction SilentlyContinue).Count)
Write-Output ('ProjectVersion: ' + (Test-Path (Join-Path $dst 'ProjectSettings\ProjectVersion.txt')))
