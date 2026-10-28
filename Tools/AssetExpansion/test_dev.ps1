$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$developmentRoot=Join-Path $projectRoot 'Builds/AssetExpansion-Dev'
foreach($case in @(@{Folder='Welteneditor';Mode='editor';Report='editor'},@{Folder='Welteneditor';Mode='admin';Report='blocked-admin'},@{Folder='Verwaltung';Mode='admin';Report='admin'})){
 $toolRoot=Join-Path $developmentRoot $case.Folder
 $checkRoot=Join-Path $toolRoot 'Pruefung'
 New-Item -ItemType Directory -Force $checkRoot | Out-Null
 if($case.Folder -eq 'Verwaltung'){
  Copy-Item -LiteralPath "$projectRoot/Builds/Private-0.9.0/Verwaltung/Pruefung/Nutzer-Level.trl" -Destination "$checkRoot/Nutzer-Level.trl"
 }
 $started=Get-Date
 $arguments='-batchmode --level-'+$case.Mode+' --tools-smoke -logFile "'+$checkRoot+'/'+$case.Report+'-Unity.log"'
 $runtimeJob=Start-Process -FilePath "$toolRoot/Runtime/TreasureRunnerTools.exe" -ArgumentList $arguments -WorkingDirectory $toolRoot -WindowStyle Hidden -PassThru
 $runtimeJob.WaitForExit();$runtimeJob.Refresh()
 $report=Join-Path $checkRoot ($case.Report+'.txt')
 if($runtimeJob.ExitCode -ne 0 -or -not(Test-Path -LiteralPath $report)){throw "Windows-Prüfung fehlgeschlagen: $($case.Report)"}
 if((Get-Item -LiteralPath $report).LastWriteTime -lt $started -or -not(Select-String -LiteralPath $report -SimpleMatch 'SMOKE PASSED' -Quiet)){throw "Kein aktueller Erfolg: $($case.Report)"}
 Get-Content -LiteralPath $report
}
