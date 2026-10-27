$ErrorActionPreference = 'Stop'
$root = 'C:\Users\USER\CrisisVR'

Write-Output '=== BUILDANDROID.CS BYTES ==='
$p = Join-Path $root 'Assets\_Project\Scripts\Build\Editor\BuildAndroid.cs'
$b = [System.IO.File]::ReadAllBytes($p)
Write-Output ('Bytes: ' + $b.Length)
$t = [System.Text.Encoding]::UTF8.GetString($b)
$lines = $t -split "`n"
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '\\' -or $lines[$i] -match '\?\?') {
        Write-Output (($i + 1).ToString() + ': ' + $lines[$i])
    }
}

Write-Output ''
Write-Output '=== PACKAGES-LOCK (ugui/textmesh/yarn) ==='
$pl = Join-Path $root 'Packages\packages-lock.json'
if (Test-Path $pl) {
    $j = Get-Content $pl -Raw | ConvertFrom-Json
    foreach ($k in $j.dependencies.PSObject.Properties.Name) {
        if ($k -match 'ugui|textmesh|yarn|timeline') {
            Write-Output ($k + ' = ' + $j.dependencies.$k.version)
        }
    }
} else { Write-Output 'NO packages-lock.json' }

Write-Output ''
Write-Output '=== TUTORIALROOT PREFAB: COMPONENTES ==='
$prefab = Join-Path $root 'Assets\_Project\Prefabs\Tutorial\TutorialRoot.prefab'
if (Test-Path $prefab) {
    $plines = Get-Content $prefab
    foreach ($ln in $plines) {
        if ($ln -match 'm_Name: (TutorialFlow|TutorialControllerCue|TutorialPanel|TutorialInputRouter|InstructionPanel|ControllerCue|AimDemonstration|GrabDemonstration)') {
            Write-Output $ln.Trim()
        }
    }
    Write-Output ('Lines: ' + $plines.Count)
} else { Write-Output 'NO TutorialRoot.prefab' }

Write-Output ''
Write-Output '=== TUTORIAL SCENE: REFERENCIAS A TutorialFlow ==='
$scene = Join-Path $root 'Assets\Scenes\Tutorial.unity'
$scontent = Get-Content $scene -Raw
Write-Output ('TutorialFlow GUID refs: ' + ([regex]::Matches($scontent, 'TutorialFlow').Count))
Write-Output ('Script GUID 1a2b... count: ' + ([regex]::Matches($scontent, 'Tutorial').Count))
Write-Output ('Scene lines: ' + (Get-Content $scene).Count)

Write-Output ''
Write-Output '=== SCRIPTS .META (GUID -> file) ==='
$scriptsDir = Join-Path $root 'Assets\_Project\Scripts'
Get-ChildItem $scriptsDir -Recurse -Filter '*.cs.meta' | ForEach-Object {
    $g = (Select-String -Path $_.FullName -Pattern 'guid: ([a-f0-9]{32})').Matches[0].Groups[1].Value
    Write-Output ($_.BaseName + ' = ' + $g)
}

Write-Output ''
Write-Output '=== SCRIPTS REFERENCIADOS EN TUTORIAL SCENE (GUIDs) ==='
$guids = [regex]::Matches($scontent, 'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})') | ForEach-Object { $_.Groups[1].Value }
$guids | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
