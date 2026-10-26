$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$toolFolder=Join-Path $gameRoot 'Builds/Publish-0.8.0/Windows-Werkzeuge'
$runtimeExe=Join-Path $toolFolder 'Runtime/TreasureRunnerTools.exe'
New-Item -ItemType Directory -Force (Join-Path $toolFolder 'Pruefung') | Out-Null
foreach($toolMode in @('editor','admin')){
 $startedAt=Get-Date
 $runArgs='-batchmode --level-'+$toolMode+' --tools-smoke -logFile "'+$toolFolder+'/Pruefung/'+$toolMode+'-Unity.log"'
 @{mode=$toolMode;status='running'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $gameRoot 'Logs/Windows08SmokeProgress.json')
 $runtimeCheck=Start-Process -FilePath $runtimeExe -ArgumentList $runArgs -WorkingDirectory $toolFolder -WindowStyle Hidden -PassThru
 $runtimeCheck.WaitForExit()
 $reportPath=Join-Path $toolFolder ('Pruefung/'+$toolMode+'.txt')
 if(-not (Test-Path -LiteralPath $reportPath) -or $runtimeCheck.ExitCode -ne 0){@{mode=$toolMode;status='failed';exitCode=$runtimeCheck.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $gameRoot 'Logs/Windows08SmokeProgress.json');exit 1}
 $report=Get-Item -LiteralPath $reportPath
 Get-Content -LiteralPath $reportPath
 if($report.LastWriteTime -lt $startedAt -or -not (Select-String -LiteralPath $reportPath -SimpleMatch 'SMOKE PASSED' -Quiet)){exit 1}
}
@{status='passed'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $gameRoot 'Logs/Windows08SmokeProgress.json')
