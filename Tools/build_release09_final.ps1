param([string]$From='TreasureRunnerRelease09Tests.Run')
$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$unityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$steps=@(
 @{method='TreasureRunnerRelease09Tests.Run';target='Android';quit=$false},
 @{method='TreasureRunnerIdentityTests.Run';target='Android';quit=$false},
 @{method='TreasureRunnerRelease09.Android';target='Android';quit=$true},
 @{method='TreasureRunnerRelease09.WindowsEditor';target='Win64';quit=$true},
 @{method='TreasureRunnerRelease09.WindowsAdmin';target='Win64';quit=$true}
)
$started=$false
foreach($step in $steps){
 if($step.method -eq $From){$started=$true}
 if(-not $started){continue}
 $name=$step.method.Replace('.','-')
 @{step=$step.method;status='running'} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release09/BuildProgress.json"
 $buildArguments=@('-batchmode','-buildTarget',$step.target,'-projectPath',('"'+$gameRoot+'"'),'-executeMethod',$step.method,'-logFile',('"'+$gameRoot+'/Logs/Release09/Final-'+$name+'.log"'))
 if($step.quit){$buildArguments+='-quit'}
 $buildProcess=Start-Process -FilePath $unityEditor -ArgumentList $buildArguments -WindowStyle Hidden -Wait -PassThru
 if($buildProcess.ExitCode -ne 0){@{step=$step.method;status='failed';exitCode=$buildProcess.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release09/BuildProgress.json";exit 1}
}
@{status='passed';steps=$steps.method} | ConvertTo-Json | Set-Content -LiteralPath "$gameRoot/Logs/Release09/BuildProgress.json"
