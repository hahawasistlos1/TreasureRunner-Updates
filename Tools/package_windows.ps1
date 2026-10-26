param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolRoot = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $projectRoot 'Builds/Windows-Werkzeuge' }
$toolSourceRoot = Join-Path $projectRoot 'Tools'
$sourceFile = Join-Path $toolSourceRoot 'WindowsLauncher.cs'
$iconFile = Join-Path $toolSourceRoot 'TreasureRunner.ico'
New-Item -ItemType Directory -Force $toolRoot | Out-Null
$compiler = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach ($toolName in @('TreasureRunner-Levelbau','TreasureRunner-Verwaltung')) {
    $outputFile = Join-Path $toolRoot ($toolName + '.exe')
    & $compiler '/nologo' '/target:winexe' '/reference:System.Windows.Forms.dll' "/win32icon:$iconFile" "/out:$outputFile" $sourceFile
    if ($LASTEXITCODE -ne 0) { throw 'Starter konnte nicht gebaut werden.' }
}
New-Item -ItemType Directory -Force "$toolRoot/Lizenzen" | Out-Null
foreach ($pack in @('Skeletons','Dungeon','Nature','Particles','Platformer')) {
    $licenseFile = Get-ChildItem -LiteralPath "$projectRoot/Assets/Resources/OpenArt/$pack" -Filter '*icense.txt' | Select-Object -First 1
    Copy-Item -LiteralPath $licenseFile.FullName -Destination "$toolRoot/Lizenzen/$pack.txt"
}
Copy-Item -LiteralPath "$projectRoot/Assets/Resources/OpenArt/manifest.json" -Destination "$toolRoot/Lizenzen/Asset-Quellen.json"
Copy-Item -LiteralPath "$projectRoot/Documentation/Credits.md" -Destination "$toolRoot/Lizenzen/Credits.md"
Copy-Item -LiteralPath "$projectRoot/Documentation/Windows-Werkzeuge-Anleitung.md" -Destination "$toolRoot/Bitte-zuerst-lesen.md"
Write-Output "Werkzeuge vorbereitet: $toolRoot"
foreach ($fontLicense in @('Cinzel-License.txt','Lato-License.txt')) { Copy-Item -LiteralPath "$projectRoot/Assets/Resources/Fonts/$fontLicense" -Destination "$toolRoot/Lizenzen/$fontLicense" }
