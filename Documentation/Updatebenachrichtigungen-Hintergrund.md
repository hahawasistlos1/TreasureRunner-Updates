# Updatebenachrichtigungen bei geschlossenem Spiel

Lokaler Entwicklungsstand vom **26.10.2026**. Auf Nutzerauftrag eingebaut und lokal geprüft. **Noch nicht in der installierten/veröffentlichten APK 0.8.0 enthalten.** Keine APK erstellt oder installiert, keine Version erhöht und nichts veröffentlicht. Die Funktion benötigt die nächste ausdrücklich beauftragte APK.

## Bedienung

Unter **Einstellungen** und **Updates** gibt es „Updates bei geschlossenem Spiel“. Der Spieler aktiviert die Funktion dort und erlaubt auf Android ab Version 13 die Benachrichtigungen. Standard ist ausgeschaltet; eine Ablehnung wird respektiert. Bei dauerhaft gesperrter Freigabe führt eine Schaltfläche in die Android-Benachrichtigungseinstellungen. „Ausschalten“ beendet geplante Prüfungen und entfernt die Update-Meldung.

Nach Aktivierung prüft Android die neueste stabile GitHub-Veröffentlichung ungefähr alle sechs Stunden mit Netzwerkverbindung und ausreichend Akku. Eine erste Prüfung wird zusätzlich frühestens nach 30 Sekunden eingeplant. Jede neue Version wird nur einmal gemeldet, mit Versionsnummer und ausklappbaren Änderungshinweisen. Antippen startet das Spiel und prüft die aktuelle Veröffentlichung frisch, statt alte Metadaten aus einem früheren Dialog zu übernehmen. Der bestehende Dialog übernimmt Download, Fortschrittsanzeige und Prüfsumme. Die Installation bestätigt weiterhin der Spieler.

Ist beim Antippen noch ein Level geöffnet, darf die ausdrücklich angeforderte Updateprüfung dort laufen. Der vorhandene Dialog pausiert den laufenden Versuch und kann ihn beim Schließen fortsetzen. Normale automatische Vordergrundprüfungen bleiben auf das Hauptmenü beschränkt. Freundeseinladungen und Update-Links werden getrennt behandelt.

## Android-Grenzen

Dies ist eine regelmäßige Hintergrundprüfung über GitHub, kein zeitgenauer Pushdienst. Android darf sie bei Doze, Hintergrundbeschränkungen, fehlendem Netzwerk oder niedrigem Akku verzögern. Nach „Stopp erzwingen“ muss die App wieder durch den Nutzer geöffnet werden. Für gesperrte Benachrichtigungen kann das Spiel keine Systemmeldung erzwingen. Es fordert keine Ausnahme vom Energiesparen und keine exakten Alarme an.

Offizielle Grundlagen:

- [Android: Laufzeitfreigabe für Benachrichtigungen](https://developer.android.com/develop/ui/compose/notifications/notification-permission)
- [Android: regelmäßige/persistierte Jobs und Netzwerkbedingungen](https://developer.android.com/reference/android/app/job/JobInfo.Builder)
- [Android: Verhalten nach „Force Stop“](https://developer.android.com/about/versions/15/behavior-changes-all#stopped-state)
- [Unity 6.3: Laufzeitberechtigungen](https://docs.unity3d.com/6000.3/Documentation/Manual/android-RequestingPermissions.html)

## Umsetzung

- Native `UpdateNotificationJob` mit Android `JobScheduler`, periodischem und einmaligem Auftrag. Persistierung über Neustarts; erneute Planung nach App-Update. Keine zusätzliche Cloudregistrierung, kein Firebase-SDK und keine Unity-Spielszene im Hintergrund nötig.
- Vier Berechtigungen: vorhandenes `INTERNET`, zusätzlich `ACCESS_NETWORK_STATE` (für Netzwerkjobs ab Android 14 erforderlich), `POST_NOTIFICATIONS` und `RECEIVE_BOOT_COMPLETED` für persistierte Jobs. Kein Vordergrunddienst und kein Dauerprozess. JobService mit `BIND_JOB_SERVICE` geschützt.
- Fester öffentlicher Endpunkt `https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates/releases/latest`. Keine Zugangsdaten oder Spielerkennungen übertragen.
- Nur vollständige stabile APK-Releases mit neuerer numerischer Version, korrektem Dateinamen, eigener HTTPS-Downloadadresse, begrenzter Dateigröße und SHA-256-Digest werden gemeldet. Entwürfe, Vorabversionen, aktuelle/alte und bereits gesehene Versionen werden übersprungen.
- HTTP ohne Weiterleitungen, Verbindungs-/Lesefristen, maximal 512 KiB Metadaten, begrenzter Meldungstext, abbrechbarer Worker. Der Hintergrundauftrag lädt ausschließlich Metadaten und keine APK.
- Separater nativer Einstellungsbereich; keine Migration oder Rücksetzung des Spielfortschritts. Synchronisierter Ausschalt-/Meldungspfad verhindert eine Meldung nach abgeschlossenem Ausschalten.
- Eigene weiße Kristallgrafik als Android-Benachrichtigungssymbol. Unveränderlicher PendingIntent mit direktem Ziel zur Spielaktivität und `treasurerunner://updates`.
- `BackgroundUpdateNotifications` verbindet Unity mit der nativen API und fragt Androids Freigabe erst auf Betätigung an. Derselbe `BackgroundUpdatePanel` ist auf Einstellungen und Update-Seite eingebunden; auf Windows gibt es eine Plattforminformation und deaktivierte Android-Aktionen.
- Vorhandener APK-Installationsreceiver und dessen Cachebereinigung erhalten. Nach Installation wird zusätzlich die Hintergrundplanung erneuert. Kleine Datenpatches werden durch diesen Auftrag nicht separat gemeldet.

## Prüfung

**79 gezielte Prüfungen bestanden:** 47 JVM-Policyprüfungen und 32 Unity-/Konfigurations-/Menüprüfungen. Java-Quellen erfolgreich mit dem Android-36-SDK und Java-8-Sprachstand kompiliert. Android-Ressource mit `aapt2` kompiliert. Zusätzlich die echten Android-C#-Spielerskripte erfolgreich über `PlayerBuildInterface.CompilePlayerScripts` kompiliert, einschließlich JNI- und Permission-Zweigen. Dabei entstehen DLL-Prüfdateien, keine APK.

Geprüft wurden unter anderem Versionssortierung, Wiederholungsunterdrückung, manipulierte/fremde URLs und Digests, Draft-/Prerelease-Ausschluss, erforderliche Manifestberechtigungen, geschützter JobService, vorhandener APK-Provider, Editor-/Windows-Fallback, beide Menüeinträge und Trennung von Sozial-/Update-Links. Vorhandener Fortschritt und Sicherung nach Test unverändert. App-Version 0.8.0 / Android-Code 9 unverändert.

Nachweise unter `Logs/BackgroundUpdates/`:

- `PolicyChecks.txt`, `UnityChecks.txt`
- `AndroidScriptCompilation.txt`, `UnityFinal.log`
- `notification-resources.zip` (nur Ressourcenprüfung)
- `Updates.png` (echte Unity-Editorvorschau, kein Handybild)
- `Verification.json` (Abschluss und unveränderte veröffentlichte Dateien)

Der initiale Teststart wurde durch den bereits geöffneten TreasureRunner-Editor gesperrt. Nach Rückmeldung des Nutzers wurde dieser gespeichert/geschlossen und der Prüflauf ausgeführt. Andere Projekte und deren Prozesse blieben unberührt.

**Offen bis zur nächsten freigegebenen APK:** tatsächlicher Joblauf bei geschlossenem Spiel auf dem A55, Systembenachrichtigung, Android-Freigabe erlauben/ablehnen, Antippen bei kaltem/warmem Spiel, Neustart und Energiesparmodus. Diese Abläufe sind lokal vorbereitet, aber noch nicht am Gerät mit dem neuen Code getestet. Keine Meldung auf dem aktuell installierten 0.8.0 vortäuschen.
