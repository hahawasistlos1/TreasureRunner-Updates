$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$unityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$steps=@(
 @{method='TreasureRunnerRelease10Tests.Run';target='Android';quit=$false},
 @{method='TreasureRunnerEditorCommunityTests.Run';target='Android';quit=$false},
 @{method='TreasureRunnerLiveEditorTests.Run';target='Android';quit=$false},
 @{method='TreasureRunnerRelease10.Android';target='Android';quit=$true},
 @{method='TreasureRunnerRelease10.WindowsEditor';target='Win64';quit=$true},
 @{method='TreasureRunnerRelease10.WindowsAdmin';target='Win64';quit=$true}
)
foreach($step in $steps){
 @{step=$step.method;status='running'} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release10/BuildProgress.json"
 $arguments=@('-batchmode','-buildTarget',$step.target,'-projectPath',('"'+$gameRoot+'"'),'-executeMethod',$step.method,'-logFile',('"'+$gameRoot+'/Logs/Release10/'+$step.method.Replace('.','-')+'.log"'))
 if($step.quit){$arguments+='-quit'}
 $process=Start-Process -FilePath $unityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit();$process.Refresh()
 if($process.ExitCode -ne 0){@{step=$step.method;status='failed';exitCode=$process.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release10/BuildProgress.json";throw "Release failed: $($step.method)"}
 Write-Output "Passed: $($step.method)"
}
& "$gameRoot/Tools/package_windows10.ps1"
& "$gameRoot/Tools/package_windows10.ps1" -Admin
& "$gameRoot/Tools/test_windows10.ps1"
@{status='passed';version='0.10.0';code=11;published=$false} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release10/BuildProgress.json"
