# TreasureRunner

Ein buntes Android-Jump-and-Run mit acht Welten, 80 Kampagnenleveln, Waffen, Upgrades und eigenem Levelbaukasten.

## Aktuell: 0.8.0

- [Android-APK herunterladen](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.8.0/TreasureRunner.apk)
- [Windows-Levelbau und Verwaltung herunterladen](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.8.0/TreasureRunner-Windows-Werkzeuge.zip)
- [Eigene Quellen 0.8.0](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.8.0/TreasureRunner-Source-0.8.0.zip)
- [Alle Änderungen und Downloads](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/tag/v0.8.0)

Neues Fantasy-Menü nach der eigenen Bildvorlage, entsprechend gestaltete Windows-Werkzeuge, auswählbare Bewegungspfeile oder Joystick, vier Figuren, bessere Rasterblöcke und Undo/Redo, weitere Gegnerbegegnungen, Heilherzen/Schutzschilde, lokale Freundeseinladungen per Link und vorbereitete kleine Datenpatches. Bestehende Spielstände und eigene Level bleiben erhalten; kein neuer Kampagnen-Reset.

Android ARM64, mindestens Android 8. Die vorhandene App zum Aktualisieren behalten und die APK darüber installieren. Das Spiel prüft GitHub-Updates, zeigt Änderungen und Downloadfortschritt; die Installation wird selbst in Android bestätigt. Die APK-Signatur bleibt unverändert. Windows-Paket vollständig einschließlich Runtime entpacken, dann Levelbau oder Verwaltung starten; keine Unity-Installation nötig.

1.347 Prüfungen für Spiel, Editor und die fertigen Windows-Werkzeuge bestanden. Weiterhin ohne Handytest. Die Quellen sind ein Auszug mit eigenen Menügrafiken und lizenzierten Schriften, kein vollständiges Unity-Projekt; fremde Originalmodelle und Signierschlüssel sind nicht enthalten. Credits und Lizenzen liegen im Spiel und Windows-Paket.

## Eigene und Community-Level

Im Editor bauen und speichern, dann genau diese Fassung bis zum Ziel testen. „Freigeben“ öffnet ein GitHub-Einreichungsformular. Einreichen bedeutet noch keine öffentliche Freigabe. Level-Codes und Issue-Links können bereits direkt importiert und gespielt werden.

Das Windows-Verwaltungsprogramm lädt Einreichungen und freigegebene Level, bietet einen 3D-Prüflauf und kann nach bewusster Bestätigung veröffentlichen oder eine Freigabe später zurücknehmen. Schreibzugang benötigt ein eigenes auf dieses Repository begrenztes GitHub-Token; es bleibt nur während der Sitzung im Arbeitsspeicher. Kein Token gehört in ein Level oder eine Einreichung.

Klassische Level nutzen `community/catalog.json`, Asset-Level `community/catalog-v2.json` und zusätzliche 0.8-Mechaniken `community/catalog-v3.json`. Ein noch nicht vorhandener zusätzlicher Katalog gilt als leer. Die neue Spielversion führt alle Quellen zusammen; ältere APKs laden keine unbekannten Bauteile. Keine automatische APK-Erstellung beim Freigeben von Leveln.

## Freunde und Einladungen

Ab 0.8.0 gibt es im Freunde-Menü einen teilbaren Einladungslink. Die [Einladungsseite](https://hahawasistlos1.github.io/treasurerunner/invite/) öffnet eine unterstützte installierte App oder führt zum offiziellen Download. Der Empfänger bestätigt das Hinzufügen. Kontakte bleiben zunächst lokal; Online-Status, gegenseitige Server-Freundschaften und Koop benötigen weiterhin einen Onlinedienst.

## Kleine Spielpatches

Die 0.8.0 enthält einen geprüften Datenpatch-Lader mit Änderungsanzeige, Offline-Cache und Rücknahme. Unterstützte Werte und Levelabschnitte können später als kleine versionierte Datenpakete korrigiert werden. Neuer Programmcode oder neue Assets benötigen weiterhin eine APK. Derzeit ist kein zusätzlicher Datenpatch veröffentlicht.
