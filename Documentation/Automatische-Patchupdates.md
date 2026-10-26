# Automatische Patches im gemeinsamen Update-Fenster

Stand: 26.10.2026, lokaler Entwicklungsstand nach Nutzerauftrag. Keine APK gebaut, keine Version erhöht, kein Release oder öffentlicher Datenpatch angelegt. Die veröffentlichte/installierte 0.8.0 enthält diesen Bedienungsumbau noch nicht.

## Bedienung

- Neue passende Patches werden automatisch geprüft, heruntergeladen, auf Größe/Prüfsumme und Spielwerte geprüft und angewendet. Es gibt keine Download-/Anwenden-Bestätigung und keinen Zurücknehmen-Button.
- Das bestehende Update-Popup zeigt **PATCH · App-Version / Patchnummer**, Titel, „Was wurde gepatcht?“, die konkreten Änderungen, einen Fortschrittsbalken und den aktuellen Status. Nach erfolgreichem Abschluss steht dort, dass der Patch automatisch angewendet wurde.
- Der einzige Patch-Button heißt **Schließen**. Er blendet das Fenster aus. Download und Anwendung laufen weiter; das absichtlich geschlossene Fenster wird bei Abschluss nicht erneut geöffnet. Derselbe allgemeine Update-Status-Button kann die Notizen später erneut öffnen.
- Der separate Bereich „Kleine Spielpatches“, der zusätzliche Prüfbutton und die Rücknahmeaktion sind entfernt. Unter Updates und Einstellungen bleibt der allgemeine Updatezugang.
- Ein **normales APK-Update** zeigt die normale Versionsnummer und dessen Veröffentlichungstext, ohne zusätzliche Patch-Überschrift, Patchnotizen oder Patchsektion. Der automatische APK-Download und die manuelle Android-Installation bleiben erhalten. Wenn beides gleichzeitig vorliegt, hat das normale APK-Angebot Vorrang im einzigen Popup; der Patchdownload kann automatisch weiterlaufen.
- Während eines laufenden oder pausierten Levels werden Patchregeln nicht geändert. Läuft ein Download beim Levelstart bereits, wird er abgeschlossen/geprüft und anschließend automatisch bei Rückkehr ins Hauptmenü aktiviert. Dafür wird kein Spielabbruch erzwungen.

Automatische Prüfung beim Hauptmenüstart und im Abstand von fünf Minuten; nach Rückkehr in die App wird nach etwa zwei Sekunden erneut geprüft. Ohne Netz bleibt ein bereits geprüfter Patch erhalten. Eine beschädigte oder unpassende Datei ersetzt den aktiven Stand nicht.

## Umsetzung

`GameUpdateClient` baut das gemeinsame Fenster und trennt dessen APK-/Patchinhalt. `PatchClient` meldet Downloadfortschritt, geprüfte Änderungsnotizen und Anwendungsstatus an dieses Fenster; sein eigenes zweites Popup entfällt. Download- und Änderungsnotizen kommen ausschließlich aus dem bestehenden festen, zur App-Version passenden Projektkanal. Kein freier Remote-Programmcode.

`PatchStatusPanel.cs` und dessen Aufruf sind entfernt. Die manuellen `Rollback`-Methoden in `PatchRuntime` und `PatchStore` sowie `AfterRollback` sind ebenfalls entfernt. Eine technisch beschädigte Cachedatei darf weiterhin automatisch durch eine geprüfte Wiederherstellungskopie ersetzt werden; das ist keine auswählbare Spieleraktion. Gleich alte oder ältere Patchpakete können einen aktiven Patch nicht ersetzen. Spielstand, Käufe und eigene Level werden nicht zurückgesetzt.

Editor-RAM-Tests rufen keine echte GitHub-APK-Prüfung auf. Sie verwenden einen isolierten Loopbackserver für Patchdownloads, einschließlich verzögerter/chunkweiser Antworten. Die normale APK-Anzeige wird mit einem ausdrücklich simulierten Releaseobjekt geprüft; es wird keine echte neue APK heruntergeladen oder installiert. Die Vorschau „Normales-Update.png“ zeigt daher eine erfundene Testversion v0.9.0, keine Veröffentlichung.

## Prüfung

`Logs/AutomaticPatchFlow/Checks.txt`: 26 neue Prüfungen bestanden. Einschließlich automatischer Prüfung nach App-Rückkehr ohne Tastendruck, echtem stückweisen HTTP-Download mit Zwischenfortschritt, exakten Änderungsnotizen vor Aktivierung, nur einem Schließen-Button, Fortsetzung nach dem Schließen, Anwendung ohne Bestätigung, Wiederöffnen über den gemeinsamen Status, Persistenz, keinem Rücknahme-API, entferntem Patchbereich, normalem APK-Fenster ohne Patchbeschriftung, APK-Vorrang bei gleichzeitigen Angeboten, Aktivierung erst im Hauptmenü, Prüfsummenfehler und Offline-Erhalt.

Ergänzend wird die bestehende Datenpatchregression mit aktualisierten Erwartungen geprüft (`PatchRegression.txt`), außerdem die bestehende Update-/Benachrichtigungsregression. Android-Spielerskripte werden separat zu DLLs kompiliert, ohne APK-Bau. Endergebnisse und Dateihashes: `Logs/AutomaticPatchFlow/Verification.json`.

Endstand: **26 neue Ablaufprüfungen + 147 Datenpatchprüfungen + 32 Updateprüfungen = 205 bestanden**. Android-C#-Kompilierung ebenfalls erfolgreich; Bericht `AndroidScriptCompilation.txt`. Die bisherige Prüfung der eigenen Patchstatus-Karte entfällt zusammen mit dieser entfernten Oberfläche; die verbleibenden UI-Prüfungen sichern nun das Fehlen der Rücknahmeaktion und des separaten Bereichs ab. Datensicherheit, Schadensfälle und die 80 erreichbaren Kampagnenrouten bleiben geprüft.

PNG-Vorschauen in `Logs/AutomaticPatchFlow/` sind echte Unity-Editor-Renderings, keine Handyaufnahmen. Gesichert wurde der vorherige Stand in `Backups/AutomaticPatchFlow/`. Echte Spieler-Prefs/Backup und veröffentlichte APK-/Windows-/Quellarchive werden in den Prüfungen unverändert nachgewiesen. Ein Gerätetest dieser neuen Oberfläche erfordert eine später ausdrücklich beauftragte neue APK.
