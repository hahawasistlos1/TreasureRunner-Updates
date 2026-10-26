# TreasureRunner – Nickname-Konten

Eingerichtet am 26.10.2026 im Besitzer-Projekt `treasurerunner-2689b`: Android-Client aktiv, Google-Anbieter aktiv, OAuth extern/in Produktion. Firestore `(default)` in `eur3`, kostenloser Spark-Tarif, keine Abrechnung oder kostenpflichtigen Backups aktiviert. `firestore.rules` veröffentlicht und aktive Version gegen diese Datei geprüft. Anonymer Zugriff wird abgewiesen. Echtes angemeldetes Nickname-Profil auf A55 vor/nach App-Neustart geprüft; kein Cloud-Spielstand. Keine Zugangsdaten oder privaten Schlüssel in dieses Verzeichnis legen.

Die folgende Liste beschreibt die Einrichtungsschritte für eine eigene Installation; im Besitzer-Projekt sind sie bereits ausgeführt.

## Einrichtung im Projekt des Besitzers

1. Ein vorhandenes eigenes Firebase-/Google-Cloud-Projekt auswählen oder ein neues eigenes Projekt einrichten. Keine kostenpflichtigen Dienste für diesen lokalen Stand aktivieren.
2. Android-App für das bereits bestehende Paket `com.treasurerunner.game` registrieren. Den SHA-1- und SHA-256-Fingerabdruck des tatsächlich verwendeten Release-Signaturzertifikats hinterlegen; ein anderes Zertifikat verhindert die Anmeldung. Die vorhandene APK kann mit Android `apksigner verify --print-certs` rein lesend geprüft werden. Nicht den privaten Keystore weitergeben.
3. Firebase Authentication mit Google als Anmeldeanbieter einrichten. Die erforderlichen OAuth-Einstellungen und den zur App gehörenden Web-Client mit passendem Zustimmungs-/Teststatus einrichten. Der Client benötigt die Web-OAuth-Client-ID, nicht die Android-Client-ID.
4. Cloud Firestore im gewünschten Projekt einrichten. Die Datei `firestore.rules` prüfen und für genau dieses Projekt bereitstellen. Keine offenen Testmodus-Regeln verwenden. Bestehende Regeln anderer Anwendungen nicht überschreiben.
5. Die öffentlichen Android-Clientwerte in `Assets/Resources/IdentityConfig.json` eintragen: `firebaseProjectId`, `firebaseAppId`, `firebaseApiKey`, `googleWebClientId`; erst dann `enabled: true`. Die Firebase-App-ID ist nicht der Android-Paketname. API-Schlüsselbeschränkungen müssen die Android-App und benötigten Firebase-Dienste zulassen. Service-Account-Schlüssel, OAuth-Client-Secret, Google-Passwort und Tokens werden dafür nicht benötigt und gehören nicht in das Spiel.
6. Erst bei gesonderter Freigabe eine neue APK bauen. Anschließend mit einem echten Android-Konto Anmeldung, erste Nicknamewahl, Wiederanmeldung, Abmelden, Netzwerkfehler und Konto-/Gerätewechsel prüfen. Vorher ist kein realer Login bestätigt.

Im Google-Systemdialog können Google-Kontodaten erscheinen. Das Spiel liest und zeigt nur UID/Nickname aus dem separaten Spielerprofil. Firebase Auth bleibt der externe Kontodienst; dessen normale Provider-Kontodaten sind keine öffentlichen TreasureRunner-Spielerprofile. Der lokale Spielstand wird durch diese Funktion nicht synchronisiert. Nicknames sind nicht global eindeutig.

Die vorhandene öffentliche 0.8-APK ist laut `apksigner` mit dem bisherigen Android-Debug-Zertifikat signiert. Für Tests mit genau dieser Signatur: SHA-1 `F6:4D:94:20:5F:33:F0:2A:2B:4F:92:41:9E:59:C6:B4:E6:D1:40:D3`, SHA-256 `00:D6:5B:99:DF:39:CD:8D:0A:FB:F3:90:E3:65:C0:8B:4C:9B:CE:E8:85:63:69:A6:7B:C6:CD:D4:D0:6B:17:AC`. Bei einem späteren Wechsel der Signatur müssen auch die hinterlegten Fingerabdrücke angepasst werden. Es wurde hier keine Signatur geändert.

## Nur lokale Regeltests

Node 24, pnpm und eine zum aktuellen Firebase-Emulator passende Java-Laufzeit werden benötigt. Nach `pnpm install` startet `pnpm test` ausschließlich den Firestore-Emulator für `demo-treasurerunner` auf `127.0.0.1:8187`. Es gibt kein Deploy-Skript. Die Testdatei verweigert jeden anderen Projekt-/Hostwert und verwendet simulierte UIDs, keine echten Google-Konten.

`rules.test.mjs` prüft Besitzerrechte, geschützte Fremdprofile, fehlende Anmeldung, erlaubte Nicknames, verbotene zusätzliche Provider-Felder und gesperrte Listen/andere Datenpfade. Der Bericht wird in `Logs/GoogleIdentity/RulesChecks.txt` abgelegt. Ein lokaler Regeltest bestätigt keinen realen Google-Login und keine Cloud-Bereitstellung.

Am 26.10.2026 mit Node 24.22.1, Firebase CLI 15.34.1, Firestore-Emulator 1.22.0 und Java 21.0.8 tatsächlich bestanden: 33 Prüfungen. Nach der lokalen Paketinstallation wurde `node node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --project demo-treasurerunner --only firestore "node rules.test.mjs"` ausgeführt. `FIREBASE_EMULATORS_PATH` zeigte auf den Projekt-Unterordner `Logs/GoogleIdentity/EmulatorCache`. Die optionalen Installationsskripte der Pakete sind in `pnpm-workspace.yaml` deaktiviert; für diese Regeltests wurden keine nativen Paket-Builds benötigt.

Quellen: [Firebase Google Auth](https://firebase.google.com/docs/auth/android/google-signin), [Firestore-Regeln](https://firebase.google.com/docs/firestore/security/rules-fields), [Regeltests](https://firebase.google.com/docs/rules/unit-tests).
