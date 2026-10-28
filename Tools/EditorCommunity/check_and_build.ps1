param([switch]$FinalOnly)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$unity='C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$steps=@(
 @{method='TreasureRunnerEditorCommunityTests.Run';quit=$false},
 @{method='TreasureRunnerLiveEditorTests.Run';quit=$false},
 @{method='TreasureRunnerPlatformCollisionTests.Run';quit=$false},
 @{method='TreasureRunnerAssetLibraryTests.AndroidScripts';quit=$true},
 @{method='TreasureRunnerAssetLibraryDevBuild.Editor';quit=$true},
 @{method='TreasureRunnerAssetLibraryDevBuild.Admin';quit=$true}
)
if($FinalOnly){$steps=@($steps | Where-Object {$_.method -notin @('TreasureRunnerLiveEditorTests.Run','TreasureRunnerPlatformCollisionTests.Run')})}
foreach($step in $steps){
 $label=$step.method.Replace('.','-')
 @{step=$step.method;status='running'} | ConvertTo-Json | Set-Content -LiteralPath "$projectRoot/Logs/EditorCommunity/Progress.json"
 $arguments=@('-batchmode','-buildTarget','Win64','-projectPath',('"'+$projectRoot+'"'),'-executeMethod',$step.method,'-logFile',('"'+$projectRoot+'/Logs/EditorCommunity/'+$label+'.log"'))
 if($step.quit){$arguments+='-quit'}
 $process=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.WaitForExit();$process.Refresh()
 if($process.ExitCode -ne 0){throw "Local check/build failed: $($step.method)"}
 Write-Output "Passed: $($step.method)"
}
& "$projectRoot/Tools/AssetExpansion/package_dev.ps1"
& "$projectRoot/Tools/AssetExpansion/package_dev.ps1" -Admin
& "$projectRoot/Tools/AssetExpansion/test_dev.ps1"
@{status='passed';apkBuilt=$false;published=$false;versionChanged=$false} | ConvertTo-Json | Set-Content -LiteralPath "$projectRoot/Logs/EditorCommunity/Progress.json"
