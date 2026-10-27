$p = 'C:\Users\USER\CrisisVR\Assets\Scenes\Tutorial.unity'
$c = Get-Content $p -Raw
$g = '76b8f4e1bea6ce64a861e5f18ff818ce'
Write-Output ('Panel refs en escena: ' + ([regex]::Matches($c, $g).Count))

$lines = Get-Content $p
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $g) {
        Write-Output ''
        Write-Output ('--- bloque en linea ' + ($i + 1) + ' ---')
        for ($j = [Math]::Max(0, $i - 8); $j -le [Math]::Min($lines.Count - 1, $i + 16); $j++) {
            Write-Output (($j + 1).ToString() + ': ' + $lines[$j])
        }
        break
    }
}
