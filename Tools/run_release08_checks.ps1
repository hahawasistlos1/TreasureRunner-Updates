$ErrorActionPreference='Stop'
$releaseRoot=Split-Path $PSScriptRoot -Parent
$releaseEditor='C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$releaseSteps=@('TreasureRunnerNextDevTests.Run','TreasureRunnerGridTests.Run','TreasureRunnerPatchTests.Run','TreasureRunnerCharacterTests.Run','TreasureRunnerControlChoiceTests.Run','TreasureRunnerInviteTests.Run','TreasureRunnerReferenceMenuTests.Run')
foreach($releaseStep in $releaseSteps){
 $releaseLog=Join-Path $releaseRoot ('Logs/Release08-'+$releaseStep.Split('.')[0]+'.log')
 @{step=$releaseStep;status='running'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseRoot 'Logs/Release08ChecksProgress.json')
 $releaseArguments=@('-batchmode','-projectPath',('"'+$releaseRoot+'"'),'-executeMethod',$releaseStep,'-logFile',('"'+$releaseLog+'"'))
 $releaseProcess=Start-Process -FilePath $releaseEditor -ArgumentList $releaseArguments -WindowStyle Hidden -PassThru
 $releaseProcess.WaitForExit()
 if($releaseProcess.ExitCode -ne 0){@{step=$releaseStep;status='failed';exitCode=$releaseProcess.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseRoot 'Logs/Release08ChecksProgress.json');exit 1}
}
@{status='passed';steps=$releaseSteps} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseRoot 'Logs/Release08ChecksProgress.json')
