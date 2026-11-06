$ErrorActionPreference = 'Stop'
Add-Type -Path 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
$ruta = 'C:\Users\USER\CrisisVR\Library\ScriptAssemblies\Assembly-CSharp.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ruta)
Write-Output ('Ensamblado: ' + (Split-Path $ruta -Leaf) + '  modificado: ' + (Get-Item $ruta).LastWriteTime)
foreach ($tn in @('CrisisVR.TutorialPanel')) {
    $t = $asm.MainModule.Types | Where-Object { $_.FullName -eq $tn }
    if (-not $t) { Write-Output ('TIPO NO ENCONTRADO: ' + $tn); continue }
    Write-Output ('--- ' + $tn + ' ---')
    foreach ($m in $t.Methods) {
        if ($m.Name -match 'FitInstruction|EnsureProgress|SetProgress|Show') {
            $ps = ($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ', '
            Write-Output ('  ' + $m.Name + '(' + $ps + ')  visibilidad=' + $m.IsPublic)
        }
    }
    foreach ($f in $t.Fields) { if ($f.Name -eq 'progress') { Write-Output ('  campo progress : ' + $f.FieldType.Name) } }
}
