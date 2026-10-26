# Google-Anmeldung mit eigenem Nickname – eingerichtet am 26.10.2026

Im Profil gibt es jetzt eine vorbereitete Google-Anmeldung. Nach erfolgreicher Anmeldung ohne vorhandenes Spielerprofil erscheint eine leere Nickname-Eingabe. Ein gültiger selbst gewählter Nickname wird separat gespeichert und als Spielername für die vorhandenen lokalen Menüs und Einladungen verwendet. Google-Profilname, E-Mail und Profilbild werden nicht als Spielername übernommen oder an Unity weitergereicht. Abmelden erhält den lokalen Kampagnenstand.

Die Android-Anmeldung ist jetzt im Besitzer-Projekt `treasurerunner-2689b` eingerichtet und in APK 0.9.0 aktiviert. Google-Anbieter und passende Signaturfingerabdrücke sind hinterlegt; OAuth-Zielgruppe extern, Status „In Produktion“. Firestore `(default)` liegt in `eur3`, im kostenlosen Spark-Tarif ohne aktiviertes Abrechnungskonto oder kostenpflichtige Backups. Die getesteten Feldregeln wurden veröffentlicht und aus der aktiven Regelversion rückgelesen. Anonyme REST-Abfrage: HTTP 403.

Auf dem A55 wurde die bestehende App ohne Datenlöschung aktualisiert. Das echte angemeldete Profil mit selbst gewähltem Nickname wurde vor und nach vollständigem App-Neustart geprüft. Die erste Kontoauswahl und das Speichern des Nicknames wurden nicht automatisiert oder aufgezeichnet; das vorhandene Nutzerkonto wurde weder abgemeldet noch verändert. Eigene Leveldateien sind bytegenau erhalten. Konto-/Gerätewechsel und eine vollständige Offline-Fehlermatrix sind damit nicht behauptet.

## Datenfluss

Android Credential Manager öffnet den Google-Systemdialog. Dessen eigene Kontoauswahl kann Google-Kontodaten anzeigen. Im Spiel und im separaten Spielerprofil wird nur der gewählte Nickname angezeigt. Firebase Authentication prüft den Google-Anmeldenachweis serverseitig und verwaltet die Anmeldung. Der Authentifizierungsdienst kann die üblichen Google-Kontodaten innerhalb seiner Kontoverwaltung verarbeiten; diese sind nicht Bestandteil der separaten Spielerprofile und werden von unserem Code weder ausgelesen noch angezeigt.

Unity erhält ausschließlich eine interne UID, den gewählten Nickname und feste Status-/Fehlercodes. Weder Tokens noch rohe Provider-Fehler werden in Spielständen oder eigenen Logs abgelegt. Alte, doppelte oder unaufgeforderte Rückmeldungen werden ignoriert. Abbruch, Zeitüberschreitung und Abmelden löschen den lokalen Anmeldestatus.

Firestore `players/{uid}` enthält ausschließlich `schema: 1` und `nickname`. Die Regeln erlauben nur dem angemeldeten Besitzer, dieses Dokument anzulegen, zu ändern oder zu löschen. Angemeldete Spieler können einen bekannten Spieler-Nickname abfragen; Verzeichnislisten sind gesperrt. Andere Datenpfade sind gesperrt. Profilabrufe verwenden den Server statt eines persistenten Gerätecaches. Vorhandene Spielstände, Freunde und Ranglisten werden nicht automatisch in die Cloud kopiert.

Nicknames: 3–20 Zeichen, lateinische Buchstaben einschließlich deutscher Umlaute/ß, Zahlen, Leerzeichen, Punkt, Unterstrich und Bindestrich; mindestens ein Buchstabe/eine Ziffer, keine äußeren Leerzeichen. E-Mail-Adressen, Markup und Steuerzeichen werden abgelehnt. Die Regeln gelten auch für die neue Eingabe eines lokalen Gast-Nicknames. Keine Eindeutigkeitsgarantie für Nicknames; die interne UID unterscheidet Konten.

## Dateien und Prüfgrenzen

- Unity-Ablauf: `Assets/Scripts/Social/GoogleIdentity.cs`, `NicknamePolicy.cs`, `Assets/Scripts/UI/GoogleAccountPanel.cs`, `MenuProfilePages.cs`.
- Native Android-Bibliothek: `Assets/Plugins/Android/TreasureIdentity.androidlib`. Credential Manager 1.6.0, Google ID 1.1.1, Firebase BoM 34.18.0. Manuelle Initialisierung einer benannten Firebase-App, kein Google-Services-Gradle-Plugin und kein Analytics-SDK ergänzt.
- AndroidX-Einstellung wird nur bei einem später separat autorisierten Android-Bau in dessen generiertem Gradle-Projekt aktiviert; der Editor-Hook startet keinen Bau.
- Cloud-Regeln und lokale Regeltests: `Cloud/Identity`. Besitzer-Einrichtung siehe dortige `README.md`.
- `Logs/GoogleIdentity/Checks.txt`: 45 bestandene Unity-Prüfungen für Nicknames, nicht konfigurierte Anmeldung, Statuswechsel, Rückmeldungszuordnung, Anbieterfeld-Abschirmung, UI und Schutz vorhandener Daten. Die angemeldete Vorschau `Nickname-Konto.png` verwendet eine ausdrücklich simulierte Rückmeldung, kein echtes Google-Konto.
- `NativeCompileFinal.log`: Java-Releasequellen mit tatsächlichen Android-/Firebase-Abhängigkeiten erfolgreich kompiliert. Historischer Einzeltest vor dem späteren erfolgreichen APK-Bau.
- `AndroidScripts.txt` und `AndroidCompileEditor.log`: gemeinsamer Android-C#-Stand erfolgreich ausschließlich zu Test-DLLs kompiliert.
- `RulesChecks.txt` und `RulesEmulatorFinal.log`: 33 Zugriffsprüfungen mit dem offiziellen lokalen Firestore-Emulator 1.22.0 und Java 21 bestanden. Ausschließlich Demo-Projekt `demo-treasurerunner`, Loopback-Verbindung und simulierte Konten; damals keine Cloud-Regeln veröffentlicht; die spätere Live-Bereitstellung ist oben dokumentiert. Fremde Schreib-/Löschzugriffe, Gastzugriffe, zusätzliche Provider-Felder, ungültige Nicknames, Verzeichnislisten und fremde Datenpfade werden abgelehnt. Der Emulator wurde anschließend beendet. Die vorherigen Installationsprotokolle enthalten Paketmanager-Wiederholungen und zunächst nicht freigegebene optionale Installationsskripte; die abschließende direkte CLI-Ausführung war erfolgreich.

Finale Nachweise: `Logs/Release09/GoogleSetupVerification.json`, `AndroidVerification.json`, `Logs/Device09/Verification.json` und die beiden Profil-Screenshots. APK und beide Windows-Ausgaben erfolgreich gebaut; diese Anleitung bestätigt noch keinen veröffentlichten Release. Geräteübergreifende Spielstandssynchronisierung ist nicht enthalten.

Offizielle Grundlagen: [Google-Anmeldung mit Firebase auf Android](https://firebase.google.com/docs/auth/android/google-signin), [Credential Manager](https://developer.android.com/identity/sign-in/credential-manager-siwg-implementation), [Firestore-Feldregeln](https://firebase.google.com/docs/firestore/security/rules-fields), [lokale Regeltests](https://firebase.google.com/docs/rules/unit-tests).
