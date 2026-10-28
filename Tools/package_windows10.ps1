param([switch]$Admin)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$toolRoot=Join-Path $projectRoot $(if($Admin){'Builds/Private-0.10.0/Verwaltung'}else{'Builds/Publish-0.10.0/Windows-Editor'})
$toolName=if($Admin){'TreasureRunner-Verwaltung'}else{'TreasureRunner-Levelbau'}
if(-not (Test-Path -LiteralPath (Join-Path $toolRoot 'Runtime/TreasureRunnerTools.exe'))){throw 'Runtime zuerst bauen.'}
$launcherSource=Join-Path $PSScriptRoot 'WindowsLauncher.cs'
$launcherIcon=Join-Path $PSScriptRoot 'TreasureRunner-v2.ico'
$launcherOutput=Join-Path $toolRoot ($toolName+'.exe')
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' '/nologo' '/target:winexe' '/reference:System.Windows.Forms.dll' "/win32icon:$launcherIcon" "/out:$launcherOutput" $launcherSource
if($LASTEXITCODE -ne 0){throw 'Starter-Bau fehlgeschlagen.'}
New-Item -ItemType Directory -Force "$toolRoot/Lizenzen" | Out-Null
foreach($pack in @('Skeletons','Dungeon','Nature','Particles','Platformer')){
 $licenseFile=Get-ChildItem -LiteralPath "$projectRoot/Assets/Resources/OpenArt/$pack" -Filter '*icense.txt' | Select-Object -First 1
 Copy-Item -LiteralPath $licenseFile.FullName -Destination "$toolRoot/Lizenzen/$pack.txt"
}
foreach($font in @('Cinzel','Lato')){Copy-Item -LiteralPath "$projectRoot/Assets/Resources/Fonts/$font-License.txt" -Destination "$toolRoot/Lizenzen/$font-License.txt"}
Copy-Item -LiteralPath "$projectRoot/Assets/Resources/OpenArt/manifest.json" -Destination "$toolRoot/Lizenzen/Asset-Quellen.json"
New-Item -ItemType Directory -Force "$toolRoot/Lizenzen/Assetbibliothek" | Out-Null
Get-ChildItem -LiteralPath "$projectRoot/Assets/Resources/AssetLibrary/Licenses" -Filter '*.txt' | Copy-Item -Destination "$toolRoot/Lizenzen/Assetbibliothek"
Copy-Item -LiteralPath "$projectRoot/Assets/Resources/AssetLibrary/Catalog.json" -Destination "$toolRoot/Lizenzen/Assetbibliothek/Katalog.json"
Copy-Item -LiteralPath "$projectRoot/Documentation/Credits.md" -Destination "$toolRoot/Lizenzen/Credits.md"
Copy-Item -LiteralPath "$projectRoot/Documentation/Windows-Welteneditor-0.10.md" -Destination "$toolRoot/Bitte-zuerst-lesen.md"
@{version='0.10.0';kind=$(if($Admin){'private-owner-moderation'}else{'public-world-editor'});moderationProtocol=4} | ConvertTo-Json | Set-Content -LiteralPath "$toolRoot/Ausgabe.json"
Write-Output "Vorbereitet: $toolRoot"
