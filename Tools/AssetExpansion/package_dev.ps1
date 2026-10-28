param([switch]$Admin)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$toolRoot=Join-Path $projectRoot $(if($Admin){'Builds/AssetExpansion-Dev/Verwaltung'}else{'Builds/AssetExpansion-Dev/Welteneditor'})
$toolName=if($Admin){'TreasureRunner-Verwaltung-Entwicklung'}else{'TreasureRunner-Levelbau-Entwicklung'}
if(-not(Test-Path -LiteralPath "$toolRoot/Runtime/TreasureRunnerTools.exe")){throw 'Entwicklungsruntime fehlt.'}
$launcherSource=(Resolve-Path -LiteralPath (Join-Path $projectRoot 'Tools/WindowsLauncher.cs')).ProviderPath
$launcherIcon=(Resolve-Path -LiteralPath (Join-Path $projectRoot 'Tools/TreasureRunner-v2.ico')).ProviderPath
$launcherOutput=Join-Path $toolRoot ($toolName+'.exe')
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' '/nologo' '/target:winexe' '/reference:System.Windows.Forms.dll' "/win32icon:$launcherIcon" "/out:$launcherOutput" $launcherSource
if($LASTEXITCODE -ne 0){throw 'Entwicklungsstarter fehlgeschlagen.'}
New-Item -ItemType Directory -Force "$toolRoot/Lizenzen/Assetbibliothek" | Out-Null
Get-ChildItem -LiteralPath "$projectRoot/Assets/Resources/AssetLibrary/Licenses" -Filter '*.txt' | Copy-Item -Destination "$toolRoot/Lizenzen/Assetbibliothek"
foreach($pack in @('Skeletons','Dungeon','Nature','Particles','Platformer')){
 $licenseFile=Get-ChildItem -LiteralPath "$projectRoot/Assets/Resources/OpenArt/$pack" -Filter '*icense.txt' | Select-Object -First 1
 Copy-Item -LiteralPath $licenseFile.FullName -Destination "$toolRoot/Lizenzen/$pack.txt"
}
foreach($font in @('Cinzel','Lato')){Copy-Item -LiteralPath "$projectRoot/Assets/Resources/Fonts/$font-License.txt" -Destination "$toolRoot/Lizenzen/$font-License.txt"}
Copy-Item -LiteralPath "$projectRoot/Assets/Resources/AssetLibrary/catalog.json" -Destination "$toolRoot/Lizenzen/Assetbibliothek/Katalog.json"
@'
# TreasureRunner – lokale Entwicklungsausgabe

Diese Ausgabe öffnet eine leere Vollbild-Bauwelt: oben links stehen das Zahnrad für alle Bauteile und daneben das Funktionsmenü. Die schaltbare Simulation lässt Gegner und Fallen laufen, während du weiterbauen kannst. Zwei Finger verschieben/zoomen die Ansicht; am PC verschiebt auch die rechte oder mittlere Maustaste. Am Bildschirmrand scrollt die Ansicht beim Bauen weiter. Unter Funktionen → Ansicht → Level einrichten findest du bis zu 24 Abschnitte, 32 Felder Höhe und 2400 Bauteile. Eigene Bauplätze werden ausdrücklich geladen/gespeichert. Simulation und „Neu starten“ verändern keine gespeicherten Bauteilpositionen; für den vollständigen Ablauf gibt es „Testspielen“.

Sie ist noch nicht veröffentlicht und bleibt getrennt von den bisherigen Windows-Werkzeugen. Level mit neuen Bauteilen oder mehr als sechs Abschnitten benötigen eine spätere passende Spielausgabe; die veröffentlichte APK 0.9.0 unterstützt diese Erweiterungen noch nicht. Die Einreichung im aktuellen Onlinekatalog ist dafür gesperrt. Bestehende Level werden nicht automatisch verlängert oder überschrieben.

Die vollständigen Urheber- und Lizenzhinweise stehen im Ordner Lizenzen und innerhalb der Anwendung unter Credits. Originalarchive und eigene Level bleiben erhalten.
'@ | Set-Content -LiteralPath "$toolRoot/Bitte-zuerst-lesen.md" -Encoding utf8
Write-Output "Lokales Entwicklungspaket vorbereitet: $toolRoot"
