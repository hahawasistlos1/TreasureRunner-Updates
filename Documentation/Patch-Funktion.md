# Kleine Spielpatches ohne erneute APK-Installation

Aktualisiert 26.10.2026: Patches laufen automatisch im gemeinsamen Update-Popup mit Kennzeichnung „PATCH“, Fortschrittsbalken und Änderungsnotizen. Kein separater Patch-Bereich und keine manuelle Rücknahme mehr. Ein normales APK-Update zeigt keine zusätzliche Patch-Kennzeichnung. Details und aktuelle Nachweise: `Documentation/Automatische-Patchupdates.md`. Dieser Umbau ist lokal und noch nicht in der veröffentlichten APK enthalten.

Ergänzung 26.10.2026: Die lokale Validierung unterstützt neben sword/axe/bow/staff auch spear/hammer/crossbow/froststaff (maximal acht Waffenwerte). Keine öffentliche Patchdatei angelegt. Neue Modelle/Fähigkeitslogik müssen zuerst mit einer ausdrücklich beauftragten APK verteilt werden; ein Datenpatch kann danach die zulässigen Waffenwerte korrigieren. Details: `Documentation/Waffen-Faehigkeiten-Fortschritt.md`.

## Ziel und Grenzen

Nutzer möchte kleinere Korrekturen verteilen, ohne jedes Mal eine komplette APK erstellen zu müssen. Implementiert ist ein Datenpatch-Kanal zusätzlich zum bisherigen APK-Updater. Einmal muss eine neue, ausdrücklich freigegebene APK mit dem Patch-Lader auf die Geräte kommen. Die bereits veröffentlichte 0.7.0 kennt diesen Lader noch nicht. Aktuell bleibt alles lokal; keine APK, Versionsänderung, GitHub-Datei oder Veröffentlichung wurde erstellt/geändert.

Vor Änderungen gesichert: Backups/DataPatches/BeforePatchSystem.zip.

Die erste Ausbaustufe unterstützt Kampagnenkorrekturen an:

- Grundschaden und Angriffspause pro vorhandener Waffe. Bestehende Waffen-Upgrades rechnen weiter auf den korrigierten Grundwerten.
- Gegner-Leben, Tempo, Wahrnehmungsradius und maßvoller Skalierung der Trefferflächen, getrennt nach Welt und Grundtyp (normal, monster, shooter, boss). Die Unterkante bleibt beim Ändern der Höhe am bisherigen Ort.
- Breite, Höhe, nachfolgender Sprunglücke und Raumtyp einzelner vorhandener Kampagnenabschnitte. Nachfolgende X-Positionen werden neu berechnet. Anzahl der Level/Abschnitte, IDs und Freischaltungen bleiben erhalten. Alle angrenzenden Sprünge müssen mit der unveränderten Grundsprungphysik erreichbar bleiben.

Community- und Editor-Level behalten ihre standardisierte Grundbalance; deren Inhalte können bereits über den vorhandenen Katalog aktualisiert werden. Neue Gegnerprogramme, neue Spielmechaniken, C#-Fehlerbehebungen, native Android-Funktionen, Unity-Änderungen und derzeit fest eingebaute Grafiken/Musik benötigen weiter eine neue APK. Es wird kein heruntergeladenes Programm ausgeführt. Größere nachladbare Grafikpakete wären ein separater Ausbau (z.B. Addressables), nicht Teil dieser ersten Datenpatch-Funktion.

Unity erläutert die Trennung zwischen Inhalts- und Programmänderungen: https://docs.unity.cn/Packages/com.unity.addressables%401.19/manual/ContentUpdateWorkflow.html

## Ablauf im Spiel

- Beim Start wird der zuletzt geprüfte, zur exakten App-Version passende Patch aus dem privaten Datenspeicher geladen. Offline bleibt dieser Stand spielbar.
- Im Hauptmenü prüft der Client automatisch in Abständen von fünf Minuten den eigenen GitHub-Kanal. Neue Daten werden automatisch heruntergeladen und nur ohne laufendes Level aktiviert.
- Beginnt während des Downloads ein Level, wartet die Aktivierung bis zur Rückkehr ins Hauptmenü. Auch eine Pause erlaubt keine Änderung laufender Spielregeln.
- Das gemeinsame Update-Popup zeigt bei einem Patch „PATCH“, App-/Patchnummer, Titel, Änderungen und laufenden Downloadfortschritt. Herunterladen, Prüfen und Anwenden benötigen keine Bestätigung. „Schließen“ blendet nur das Fenster aus; der Download läuft weiter.
- Es gibt keinen eigenen Patch-Bereich, keinen Zurücknehmen-Button und keine öffentliche Rücknahmefunktion im Spielerprogramm. Bereits installierte Daten bleiben erhalten; nur höhere gültige Patchnummern ersetzen sie. Fehlerhafte Daten werden weiterhin automatisch abgefangen.
- APK-Download und manuelle Android-Installation bleiben im bestehenden GameUpdateClient erhalten. Die Patch-Funktion führt keine APK-Installation aus.

## Daten und Speicherung

PatchManifest schema 1: exakte appVersion, aufsteigende revision, title (max. 80 Zeichen), changes (max. 2.000 Zeichen), payloadFile (nur patch-NNNN.json), payloadBytes (max. 256 KiB), sha256. Ankündigung maximal 16 KiB. Keine frei wählbaren Downloadadressen oder Dateipfade aus dem Netz.

DataPatch schema 1: dieselbe appVersion/revision; Listen weapons, enemies und levels. Einträge müssen eindeutig und innerhalb fester Zahlenbereiche liegen. Unbekannte Typen, ungültige Werte, nicht endliche Zahlen und unpassende Sprunggeometrie werden abgelehnt.

Jeder veröffentlichte Patch ist ein **vollständiger kumulativer Satz aller gewünschten Abweichungen vom APK-Grundstand**, kein Patch, der ältere Downloads voraussetzt. Nicht enthaltene Werte nutzen den APK-Grundstand. Dadurch können neue Spieler direkt die neueste kleine Datei laden.

Eine atomar ersetzte state.json enthält aktuellen Patch, eine technische Wiederherstellungskopie und wegen beschädigter Cachepakete gesperrte Nummern. Vor dem Commit werden Schema, Dateigröße, SHA-256 und Spielwerte geprüft. Ein fehlgeschlagener Schreibvorgang lässt den vorherigen Stand aktiv. Beschädigte Daten fallen automatisch auf den geprüften Vorgänger bzw. den Grundstand zurück; Spieler können das nicht manuell auslösen. Ein Sicherungsjournal deckt beschädigte Hauptdateien ab. Spielstand, Geld, Käufe und eigene Level liegen weiterhin getrennt davon; keine Kampagnenmigration und kein Reset wurden ergänzt.

## GitHub-Kanal und lokales Werkzeug

Vorgesehener Kanal im autorisierten Repository, Branch TennoFarm:

- patches/<App-Version>/manifest.json
- patches/<App-Version>/patch-0001.json usw. (Dateinamen nach Veröffentlichung unveränderlich, jede neue Fassung mit höherer Nummer)

Der Client liest ausschließlich die festen HTTPS-Adressen unter raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/TennoFarm/patches/<App-Version>/ und akzeptiert keine HTTP-Weiterleitungen. Jede App-Version behält einen eigenen Kanal; so können auch ältere, bereits patchfähige APKs weiterhin passende Korrekturen erhalten. Das bestehende UpdateChannel-Asset schaltet den Kanal ein/aus. Ein HTTP-Loopback-Haken existiert nur für Unity-Editor-Prüfungen und wird nicht in den Player kompiliert.

Vorbereitung: Patches/Draft.json und Patches/Notes.json bearbeiten. In Unity: TreasureRunner → Spielpatch → Entwurf prüfen und exportieren. Export nach Builds/DataPatch/<App-Version>/: manifest.json und patch-NNNN.json. Der Export prüft die Daten und erzeugt Größe/Prüfsumme; er baut keine APK und lädt nichts hoch.

Die mitgelieferten Entwurfsdateien sind ausdrücklich lokale Testbeispiele für den aktuellen Entwicklungsstand 0.7.0. Nach einer später ausdrücklich freigegebenen neuen APK muss appVersion im Entwurf deren Versionsnummer entsprechen. Erst geprüfte echte Korrekturen exportieren. Eine Veröffentlichung erfolgt nur auf gesonderte Nutzeranweisung; Paket zuerst, Ankündigung zuletzt veröffentlichen, damit Clients nie eine noch fehlende Nutzdatei angeboten bekommen.

## Prüfung

Abgeschlossen am 24.10.2026:

- 148 gezielte Patch-Prüfungen bestanden, Logs/DataPatchTests.txt: Schema, Versionskanäle, Prüfsumme/Dateigröße, Empfangsgrenze, ungültige Werte, 80 erreichbare Kampagnenrouten, tatsächliche Waffen-/Gegner-/Geometrieanwendung, atomarer Cache, beschädigte Journale, Offline-Neustart, Schreibfehler, Rücknahme und Revisionssperre.
- Echte HTTP-Downloads von einem isolierten Loopback-Testserver geprüft: erfolgreiche Aktivierung, manipulierte Nutzdatei, Download während laufendem Level mit verzögerter Aktivierung, Popup, tatsächlicher Rücknahme-Button, wiederholte Prüfung und Offline-Fall. Kein öffentlicher Server verändert.
- Lokaler Export samt Prüfsumme erfolgreich, Builds/DataPatch/0.7.0/manifest.json und patch-0001.json. Das ist ein ausdrücklich beschriftetes Testbeispiel ohne veröffentlichte Korrekturen.
- 547 Spiel-/Baukastenprüfungen nach Patch- und Figurenänderungen erneut bestanden, Logs/FantasyMenuRegression.txt (Unity-Lauf: Logs/PatchBaseRegressionUnity.log).
- 26 Steuerungsprüfungen erneut bestanden, Logs/ControlChoiceTests.txt (Unity-Lauf: Logs/PatchControlRegressionUnity.log).
- 120 Figurenprüfungen bestanden, Logs/CharacterChoiceTests.txt. Zusammen 841 erfolgreiche Prüfungen im aktuellen Abschlussstand.

Spieler-Prefs und deren Backup sowie die drei veröffentlichten 0.7-Dateien unverändert. Popup und Statusseite tatsächlich in Unity gerendert und visuell kontrolliert, Bilder unter Builds/PatchPreview. Kein Handytest, keine APK, keine Versionsänderung, kein Upload und keine Veröffentlichung.
