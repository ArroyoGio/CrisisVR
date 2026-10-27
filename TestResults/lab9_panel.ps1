$p = 'C:\Users\USER\CrisisVR\Assets\_Project\Prefabs\Tutorial\TutorialRoot.prefab'
$l = Get-Content $p
Write-Output '=== Font sizes en el prefab ==='
$l | Select-String -Pattern 'm_fontSize:' | ForEach-Object { $_.Line.Trim() } | Group-Object | Sort-Object Count -Descending | ForEach-Object { $_.Name + '  x' + $_.Count }

Write-Output ''
Write-Output '=== Bloque TutorialPanel ==='
for ($i = 0; $i -lt $l.Count; $i++) {
    if ($l[$i] -match '76b8f4e1bea6ce64a861e5f18ff818ce') {
        for ($j = [Math]::Max(0, $i - 10); $j -le [Math]::Min($l.Count - 1, $i + 30); $j++) {
            Write-Output (($j + 1).ToString() + ': ' + $l[$j])
        }
        break
    }
}
