$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path $PSScriptRoot -Parent
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe'
$buildArguments = @('-batchmode','-buildTarget','Win64','-projectPath',('"'+$gameRoot+'"'),'-executeMethod','TreasureRunnerRelease08.Windows','-quit','-logFile',('"'+$gameRoot+'/Logs/WindowsToolsBuildUnity.log"'))
$windowsBuildProcess = Start-Process -FilePath $unityEditor -ArgumentList $buildArguments -WindowStyle Hidden -Wait -PassThru
if ($windowsBuildProcess.ExitCode -ne 0) { throw 'Windows-Bau fehlgeschlagen. Siehe Logs/WindowsToolsBuildUnity.log.' }
& (Join-Path $gameRoot 'Tools/package_windows.ps1') -OutputDirectory (Join-Path $gameRoot 'Builds/Publish-0.8.0/Windows-Werkzeuge')
