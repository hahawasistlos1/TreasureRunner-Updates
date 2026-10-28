# TreasureRunner

Android-Jump-and-Run mit acht Welten, 80 Kampagnenleveln, acht Waffen, Figurenfähigkeiten und gemeinsamem Welteneditor.

## Version 0.10.0

- [Android-APK](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.10.0/TreasureRunner.apk)
- [Windows-Welteneditor](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.10.0/TreasureRunner-Windows-Editor.zip)
- [Änderungen und Prüfgrenzen](Documentation/Release-0.10.0.md)
- [Windows-Anleitung](Documentation/Windows-Welteneditor-0.10.md)
- [Eigener Spielcode](Assets/Scripts)

Neuer Vollbildeditor mit Zahnrad für die gemeinsame Assetpalette und getrenntem Funktionsmenü. Leere Welt mit bis zu 384 × 32 Feldern und 2400 Bauteilen; Bauen und Löschen bei laufender Gegner-/Fallensimulation, verschiebbare Ansicht und Randscrollen. Eigene Bauplätze werden ausdrücklich gespeichert und geladen. Testspielen ist freiwillig, Einreichen führt zur Betreiberentscheidung. [Assetbibliothek und Quellen](Documentation/Assetbibliothek-0.10.md).

Die separate Verwaltung bleibt beim Betreiber und gehört nicht zum öffentlichen Windows-Download. APK und aktuelle Verwaltung verwenden ausschließlich community/0.10.0/catalog.json mit Protokoll4 und passender Spielversion. Alte Werkzeuge schreiben ihre bisherigen Kataloge. Eigene ältere Level bleiben importierbar; kein neuer Spielstandsreset.

Geheimtüren verschwinden nach Schatzfund und Verlassen für den Versuch. Jägerbogen bei Richtungswechseln und Upgrades korrigiert. Freigegebene Community-Level bieten Sterne und optionale Kommentare unter eigenem Nickname. Google-Anmeldung ist eingerichtet; keine Google-E-Mail oder Provider-Profilnamen im Spielprofil. Firebase Spark/Europa eur3; keine Cloud-Spielstandssynchronisierung. Der neue Katalog startet leer, daher noch kein regulärer öffentlicher Bewertungsschreibtest.

## Quellen und Lizenzen

Eigener C#-/Java-/Shader-Code, Editor- und Buildwerkzeuge, eigene Menügrafiken und lizenzierte Schriften stehen als Git-Dateien bereit. Kein zusätzlicher Source-ZIP-Anhang. Dieser Quellcode-Auszug ist kein vollständig startfertiges Unity-Projekt: fremde Originalmodelle, ursprüngliche Szenen und benötigte Unity-Pakete sind rechtmäßig zu ergänzen. Keine privaten Signierschlüssel, Tokens, Spielstände oder Build-Caches. Für eigenen Code wird keine zusätzliche Open-Source-Lizenz eingeräumt. [Credits](Documentation/Credits.md).

Unity6000.3.17f1/URP, Android ARM64 ab API26, Windows x64. Build-Einstiege TreasureRunnerRelease10.Android, .WindowsEditor, .WindowsAdmin. Verwaltungscode ist einsehbar; öffentliche Freigaben werden durch die GitHub-Schreibrechte des Betreibers geschützt.

## Einladungen und Updates

Die [Einladungsseite](https://hahawasistlos1.github.io/treasurerunner/invite/) öffnet die App oder bietet den offiziellen Latest-APK-Download. Nach Installation dieselbe Einladung erneut öffnen. Kleine Datenpatches verändern nur unterstützte Daten; neuer Code und neue Assets benötigen eine APK. 0.10.0 ist ein normales APK-Update, kein zusätzlicher Datenpatch. Download über die bestehende Updatefunktion, Android-Installation manuell; Hintergrundprüfungen unterliegen Android-Zeitplänen und Berechtigungen.
