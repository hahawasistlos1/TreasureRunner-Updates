# Bewegliche Plattformen – lokale Korrektur vom 26.10.2026

Bewegliche Plattformen haben jetzt einen festen kinematischen Rigidbody mit einem nicht auslösenden BoxCollider. Die bisher dort eingesetzte Einweg-Kollision wurde aus Kampagne und beiden Editor-Varianten entfernt. Dadurch blockieren sie auch seitlich und von unten. Eine zusätzliche Sicherung verhindert, dass eine alte Kombination aus `MovingRide` und `OneWayLedge` ihre Kollision wieder abschaltet. Die stationäre obere Geheimweg-Plattform bleibt absichtlich von unten durchspringbar.

Die Spielfigur fährt durch die Geschwindigkeit ihrer tatsächlich berührten Stützplattform mit. Sie wird nicht mehr pro Schritt versetzt. Bodenkontakt berücksichtigt die relative Vertikalgeschwindigkeit; damit bleibt er auch beim Aufwärtsfahren und beim Richtungswechsel erhalten. Abspringen löst den Kontakt, Pause hält Plattform und Figur an.

Betroffen: `Assets/PlayerMovement.cs`, `Assets/Scripts/Levels/AdventureMechanics.cs`, `Assets/Scripts/Levels/LevelFactory.cs`. Originale liegen unter `Backups/PlatformCollision`.

## Nachweis

`Logs/PlatformCollision/Checks.txt`: 31 bestandene Prüfungen im Unity-Editor. Enthalten sind die tatsächlichen beiden Editor-Aufzüge, seitliche Blockierung, Unterseitenkontakt, Mitfahren über wiederholte horizontale/vertikale/diagonale Bewegungen, Richtungswechsel, Pause und Abspringen sowie die bestehenden Prüfungen für Schatzblock, Druckschalter, Gegner und stationären Geheimweg. `Logs/PlatformCollision/ControlRegression.txt`: 26 bestehende Prüfungen für Joystick und Bewegungspfeile bestanden. Gemeinsame Android-C#-Skripte separat zu DLLs kompiliert; siehe `Logs/GoogleIdentity/AndroidScripts.txt`.

Der gelbe bewegliche Asset-Block ist mit korrigiert. Ein goldener Schatzblock blockiert im Test seitliches Laufen bereits korrekt und zahlt nach einem Kopfstoß genau einmal aus. Welche weiteren „gelben Steine“ der Nutzer meint, ist noch offen: Dekokisten, Sprungfedern und Druckschalter haben unterschiedliche Aufgaben. Diese wurden nicht pauschal in feste Hindernisse umgewandelt. Die dazu gestellte Rückfrage bleibt offen.

Ein früher Testentwurf verwendete versehentlich eine Format-2-Bauteil-ID in einer Format-1-Testwelt; die Testwelt wurde vor dem eigentlichen Lauf korrigiert und validiert. Der erste Bericht ist als `Fixture-first.txt` erhalten.

Realer Spielstand und Backup sowie veröffentlichte APK und Archive unverändert geprüft. Kein Handytest dieses neuen Codes, keine APK gebaut, Versionsstand weiterhin 0.8.0 / Android-Code 9, nichts veröffentlicht.
