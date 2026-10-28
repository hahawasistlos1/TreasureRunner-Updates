$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$unity='C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$logRoot=Join-Path $projectRoot 'Logs/EditorWorkspace-R2'
$steps=@(
 @{method='TreasureRunnerEditorCommunityTests.Run';quit=$false},
 @{method='TreasureRunnerAssetLibraryTests.AndroidScripts';quit=$true},
 @{method='TreasureRunnerAssetLibraryDevBuild.Editor';quit=$true},
 @{method='TreasureRunnerAssetLibraryDevBuild.Admin';quit=$true}
)
foreach($step in $steps){
 @{step=$step.method;status='running'} | ConvertTo-Json | Set-Content -LiteralPath "$logRoot/Progress.json"
 $arguments=@('-batchmode','-buildTarget','Win64','-projectPath',('"'+$projectRoot+'"'),'-executeMethod',$step.method,'-logFile',('"'+$logRoot+'/'+$step.method.Replace('.','-')+'.log"'))
 if($step.quit){$arguments+='-quit'}
 $process=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit();$process.Refresh()
 if($process.ExitCode -ne 0){throw "Local check/build failed: $($step.method)"}
 Write-Output "Passed: $($step.method)"
}
& "$projectRoot/Tools/AssetExpansion/package_dev.ps1"
& "$projectRoot/Tools/AssetExpansion/package_dev.ps1" -Admin
& "$projectRoot/Tools/AssetExpansion/test_dev.ps1"
@{status='passed';apkBuilt=$false;published=$false;versionChanged=$false} | ConvertTo-Json | Set-Content -LiteralPath "$logRoot/Progress.json"
