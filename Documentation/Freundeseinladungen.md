# Freundeseinladungen – lokaler Spielstand und öffentliche Webseite

Ergänzung 26.10.2026: Die öffentliche Seite bietet jetzt einen direkten Latest-APK-Download und auf Android den Knopf „Spiel öffnen oder APK herunterladen“ mit Download-Fallback. Einladungsdaten bleiben für das erneute Öffnen nach der Installation erhalten. 34 Webprüfungen und öffentliche Inhalte geprüft; keine neue APK/Spielversion. Details: `Documentation/Einladung-APK-Download.md`. Die folgenden ursprünglichen Versions-/Testangaben beschreiben den früheren Entwicklungsstand.

Stand: 24.10.2026. Der Nutzer hat ausdrücklich die GitHub-Einladungsseite freigegeben. Diese Freigabe umfasst keine APK, neue Spielversion, Spielveröffentlichung oder öffentlichen Datenpatch. Spielversion weiterhin 0.7.0 / Android-Code 8.

## Öffentliche Seite

- Adresse: https://hahawasistlos1.github.io/treasurerunner/invite/
- Eigenes Hosting-Repository: https://github.com/hahawasistlos1/hahawasistlos1.github.io, Branch TennoFarm, GitHub Pages aus dem Wurzelverzeichnis, HTTPS aktiv.
- Das zusätzliche Repository ist notwendig, damit die Android-Zuordnungsdatei unter `https://hahawasistlos1.github.io/.well-known/assetlinks.json` an der Domainwurzel liegt. Eine reine Projekt-Unterseite im Updates-Repository würde dafür nicht genügen.
- Die Webseite bietet „Im Spiel öffnen“, Link kopieren und die offizielle neueste Android-Veröffentlichung aus dem bestehenden Updates-Repository. Installation bleibt eine Nutzeraktion. Nach einer Installation denselben Link erneut öffnen; kein automatisches Übertragen durch einen App-Store-Installationsdienst.
- Deutlicher Hinweis: Die veröffentlichte 0.7.0 unterstützt die Einladungen noch nicht. Die neue Funktion muss erst mit einer ausdrücklich beauftragten zukünftigen APK ausgeliefert werden. Keine weitere Versionsnummer vorweggenommen.
- Quellen unter `Web/InviteSite/`. Nur diese eigens erstellten Webdateien wurden veröffentlicht; keine Unity-Projektdateien, Nutzerkontakte oder privaten Schlüssel hochgeladen.
- GitHub-Connector meldete 403 „Resource not accessible by integration“. Die freigegebenen Dateien wurden über die angemeldete GitHub-Oberfläche gespeichert. Das war eine fehlende Schreibberechtigung der Integration, keine Ablehnung durch die automatische Freigabeprüfung.

## Spiel

Im Freunde-Menü kann der Spieler seinen Link teilen oder kopieren. Android öffnet die normale Systemauswahl; der Spieler wählt selbst App, Empfänger und Versand. Windows/Editor kopieren den Link. Das Programm sendet keine Nachricht selbst.

Jeder lokale Spielstand erhält eine zufällige Kontakt-ID. Der Link trägt Protokollversion, ID und bereinigten Profilnamen im URL-Fragment hinter `#`. Die Webseite liest diese Angaben im Browser, sendet sie nicht an eine eigene Datenbank und lädt keine Analyse- oder Werbeskripte. GitHub Pages verarbeitet normale Seitenabrufe nach seinen eigenen Datenschutzregeln. Der geteilte Link enthält den Namen sichtbar für den Empfänger.

HTTPS-App-Link und Fallback `treasurerunner://invite#…` sind in derselben bereits verwendeten Unity-GameActivity eingetragen. HTTPS ist auf den Einladungspfad beschränkt. Öffentliche Android-Zuordnung verwendet Paket `com.treasurerunner.game` und den exakt aus der vorhandenen geprüften APK übernommenen öffentlichen Zertifikatabdruck. Keine Änderungen an Signaturschlüssel, Update-Provider oder Installationsreceiver.

Kalter Start nutzt `Application.absoluteURL`, eine laufende App `Application.deepLinkActivated`. Einladungen warten beim Spielen, in der Pause oder im Editor bis zum Hauptmenü. Das Home-Menü öffnet dann den Spielerbereich. Maximal fünf wartende Einladungen verhindern eine unbegrenzt wachsende Warteschlange. Vor dem Hinzufügen bestätigt oder verwirft der Empfänger den Kontakt.

Die Kontaktliste bleibt **nur auf dem jeweiligen Gerät** gespeichert. Es gibt weiterhin keine verbundene Authentifizierung, bestätigte Online-Identität, gegenseitige Server-Freundschaft, Online-Status oder Koop-Einladung. Ein zweiter Spieler kann den Link seinerseits zurückteilen, damit beide Geräte einen Kontakt speichern. Dies wird in Spiel und Webseite ausdrücklich erklärt.

Speicherung über optionale `friendId` und `friends` im vorhandenen Fortschrittsformat; alte Spielstände ohne diese Felder bleiben lesbar. Doppelte und eigene Kontakte werden abgewiesen, ein vorhandener Kontaktname wird durch einen fremden Link nicht überschrieben. Maximal 200 Kontakte, direkt entfernbar. Ungültige optionale Einträge werden bereinigt, Kampagnenstand und eigene Level bleiben erhalten. Gesperrte zukünftige Spielstände erlauben kein Hinzufügen/Entfernen.

Parser akzeptiert nur die eigene HTTPS-Adresse oder das eigene App-Schema, Protokoll 1, eine gültige Kontakt-ID und begrenzte reine Namen. Fremde Hosts/Pfade, Benutzerinformationen im URL, Queries, ungültige Kodierung, doppelte/unbekannte Felder, Steuerzeichen und überlange Links werden verworfen. Keine Ausführung von Linkinhalten.

## Prüfung

- 56 Spielprüfungen: URL-Erzeugung/Validierung, deutsche Namen, Speicherung/Altstände, Duplikate/Selbsteinladungen, Grenzen, echte Menüknöpfe, Zustimmung/Ablehnung/Entfernen, Warteschlange im Editor und Level, Szenenwechsel und Home-Menü, unveränderte reale Spielstände und Versionswerte. `Logs/InviteTests.txt`.
- 27 Webprüfungen: vollständiger Link, Name, Systemöffnungsziel, Kopierfunktion und manueller Fallback bei verweigerter Zwischenablage, ungültige Links, reine Textdarstellung und zutreffender 0.7-Hinweis. `Logs/InviteWebsiteTests.txt`.
- 547 bestehende Spiel-/Editorprüfungen bestanden. `Logs/InviteRegression.txt`.
- Native Android-Teilenmethode gegen das vorhandene Android-SDK kompiliert, ohne APK-Bau. Manifestdeklarationen mit der vorhandenen Unity-GameActivity verglichen.
- Alle drei öffentlichen Dateien mit HTTP 200, korrektem Inhaltstyp und exakt denselben Bytes wie die geprüften lokalen Dateien abgerufen. Zertifikat und Paketzuordnung geprüft. `Logs/InviteSiteDeployment.json`.
- Öffentliche Seite mit Testkontakt im Browser dargestellt, Name, Öffnungs-/Downloadziele und Erfolgsanzeige des Kopierknopfs kontrolliert. Die Zwischenablage des Browserwerkzeugs liefert unabhängig davon einen leeren Inhalt; die exakte Ausgabe der Webseite ist zusätzlich im isolierten Webtest geprüft. Keine Nachricht verschickt.
- Bilder unter `Builds/InvitePreview/`, einschließlich echter Spieloberfläche und veröffentlichter Webseite. Kein Handytest und kein End-to-End-Test der Android-App-Link-Verifikation auf einem installierten neuen Build, da weder APK-Bau noch Handytest beauftragt sind.
- Alle drei veröffentlichten 0.7.0-Dateien behalten ihre ursprünglichen SHA-256-Werte. Keine APK, Windows-Programme, Version oder Spielveröffentlichung erneuert.

Vor einer später ausdrücklich freigegebenen Spielveröffentlichung den 0.7-Hinweis auf der Webseite nach dem tatsächlichen Release anpassen und Android-App-Links auf einem Gerät prüfen.
