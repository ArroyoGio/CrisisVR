$gdd = 'C:\Users\USER\CrisisVR\docs\GDD.txt'
Write-Output ('tamano: ' + (Get-Item $gdd).Length + ' lineas: ' + (Get-Content $gdd).Count)
Write-Output ''
Write-Output '--- Encabezados ---'
Select-String -Path $gdd -Pattern '^(ACTO|Acto|##|###|[0-9]+\.[0-9]+ )' | Select-Object -First 80 | ForEach-Object { Write-Output ($_.LineNumber.ToString() + ': ' + $_.Line) }
