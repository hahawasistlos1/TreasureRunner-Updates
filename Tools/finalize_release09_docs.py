"""Record observed release checks and refresh stale setup documentation."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
setup={'project':'treasurerunner-2689b','rulesDeployed':True,'rulesReadBackMatchesLocal':True,
       'firestoreLocation':'eur3','freeSparkVerified':True,'billingEnabled':False,
       'paidBackupsEnabled':False,'oauthExternalProduction':True,
       'realAndroidLoginTested':True,'nicknameRestoredAfterRestart':True,
       'evidence':['Firebase-Regeln.png','Firebase-Spark.png','FirestoreAnonymous.json','../Device09/03-Profil.png','../Device09/06-Profil-Neustart.png'],
       'limits':'Signed-in production profile observed before and after force-stop/relaunch; user account chooser and first nickname submission were not automated or recorded. No account switching or sign-out test. No cloud save.'}
(root/'Logs/Release09/GoogleSetupVerification.json').write_text(json.dumps(setup,indent=2)+'\n')
path=root/'Logs/Release09/AndroidVerification.json'
report=json.loads(path.read_text());report['deviceLoginTested']=True
report['nicknameRestoredAfterRestart']=True;path.write_text(json.dumps(report,indent=2)+'\n')
device={'version':'0.9.0','androidCode':10,'installedInPlace':True,'dataCleared':False,
        'profileReadyBeforeAndAfterRestart':True,'googleProviderFieldsInGameUi':False,
        'customLevelsUnchanged':json.loads((root/'Logs/Device09/LevelPreservation.json').read_text())['unchanged'],
        'runtimeErrorsObserved':False,'limits':['No exhaustive 80-level handset playthrough','No account switching/sign-out test','Future-version background notification delivery not proven']}
(root/'Logs/Device09/Verification.json').write_text(json.dumps(device,indent=2)+'\n')
p=root/'Documentation/Release-0.9.0.md';s=p.read_text(encoding='utf-8')
s=s.replace('Google-Anmeldung mit eigener Nicknamewahl im Code vorbereitet, aber noch deaktiviert: Besitzer-Projekt/Firebase-/OAuth-Konfiguration fehlen. Keine funktionsfähige Online-Anmeldung oder Cloud-Spielstandssynchronisierung behauptet.',
            'Google-Anmeldung eingerichtet, anschließend eigener Nickname. Auf dem A55 mit echtem angemeldetem Profil und nach vollständigem App-Neustart geprüft. Im Spielprofil erscheinen keine Google-E-Mail, kein Google-Profilname und kein Profilbild. Spielstände und Freundeslisten bleiben lokal; keine Cloud-Spielstandssynchronisierung.')
s=s.replace('Der Betrieb des neuen Hintergrundcodes auf einem echten Gerät bleibt zu prüfen; Android kann Prüfungen verzögern.',
            'Die native Komponente ist in der installierten APK geprüft. Eine Benachrichtigung zu einer erst künftig höheren Version bleibt gesondert zu prüfen; Android kann Prüfungen verzögern.')
p.write_text(s,encoding='utf-8')
p=root/'Tools/export_git09.py';s=p.read_text(encoding='utf-8')
s=s.replace('Der neue Code wurde noch nicht auf einem Handy getestet.',
            'Die APK wurde auf einem A55 aktualisiert; Google-Profil und Wiederherstellung nach App-Neustart sind geprüft. Eigene Leveldateien blieben unverändert. Kein vollständiger Durchspieltest aller 80 Level.')
s=s.replace('Google-Anmeldung mit selbst gewähltem Nickname ist vorbereitet, aber mangels Besitzer-Projekt und OAuth-/Firebase-Konfiguration deaktiviert.',
            'Google-Anmeldung mit selbst gewähltem Nickname ist für Android eingerichtet und auf dem A55 geprüft. Das Spielprofil zeigt keine Google-E-Mail, keinen Google-Profilnamen und kein Profilbild. Firebase läuft im kostenlosen Spark-Tarif, die Nickname-Datenbank liegt in Europa (eur3).')
p.write_text(s,encoding='utf-8')
p=root/'Documentation/Google-Anmeldung-Nickname.md';s=p.read_text(encoding='utf-8')
s=s.replace('– lokaler Stand 26.10.2026','– eingerichtet am 26.10.2026')
start=s.index('Die Anmeldung ist noch');end=s.index('\n\n## Datenfluss',start)
s=s[:start]+'''Die Android-Anmeldung ist jetzt im Besitzer-Projekt `treasurerunner-2689b` eingerichtet und in APK 0.9.0 aktiviert. Google-Anbieter und passende Signaturfingerabdrücke sind hinterlegt; OAuth-Zielgruppe extern, Status „In Produktion“. Firestore `(default)` liegt in `eur3`, im kostenlosen Spark-Tarif ohne aktiviertes Abrechnungskonto oder kostenpflichtige Backups. Die getesteten Feldregeln wurden veröffentlicht und aus der aktiven Regelversion rückgelesen. Anonyme REST-Abfrage: HTTP 403.

Auf dem A55 wurde die bestehende App ohne Datenlöschung aktualisiert. Das echte angemeldete Profil mit selbst gewähltem Nickname wurde vor und nach vollständigem App-Neustart geprüft. Die erste Kontoauswahl und das Speichern des Nicknames wurden nicht automatisiert oder aufgezeichnet; das vorhandene Nutzerkonto wurde weder abgemeldet noch verändert. Eigene Leveldateien sind bytegenau erhalten. Konto-/Gerätewechsel und eine vollständige Offline-Fehlermatrix sind damit nicht behauptet.'''+s[end:]
s=s.replace('Keine APK zusammengestellt.','Historischer Einzeltest vor dem späteren erfolgreichen APK-Bau.')
s=s.replace('keine Cloud-Regeln veröffentlicht.','damals keine Cloud-Regeln veröffentlicht; die spätere Live-Bereitstellung ist oben dokumentiert.')
s=s.replace('Kein echter Google-Login, keine bereitgestellten Cloud-Regeln und keine geräteübergreifende Synchronisierung bestätigt. Keine APK gebaut, keine Version erhöht, kein Release veröffentlicht.',
            'Finale Nachweise: `Logs/Release09/GoogleSetupVerification.json`, `AndroidVerification.json`, `Logs/Device09/Verification.json` und die beiden Profil-Screenshots. APK und beide Windows-Ausgaben erfolgreich gebaut; diese Anleitung bestätigt noch keinen veröffentlichten Release. Geräteübergreifende Spielstandssynchronisierung ist nicht enthalten.')
p.write_text(s,encoding='utf-8')
p=root/'Cloud/Identity/README.md';s=p.read_text(encoding='utf-8')
s=s.replace('Vorbereitet, nicht deployed. Der Spielclient bleibt mit leerer Konfiguration deaktiviert. Keine Zugangsdaten oder privaten Schlüssel in dieses Verzeichnis legen.',
'''Eingerichtet am 26.10.2026 im Besitzer-Projekt `treasurerunner-2689b`: Android-Client aktiv, Google-Anbieter aktiv, OAuth extern/in Produktion. Firestore `(default)` in `eur3`, kostenloser Spark-Tarif, keine Abrechnung oder kostenpflichtigen Backups aktiviert. `firestore.rules` veröffentlicht und aktive Version gegen diese Datei geprüft. Anonymer Zugriff wird abgewiesen. Echtes angemeldetes Nickname-Profil auf A55 vor/nach App-Neustart geprüft; kein Cloud-Spielstand. Keine Zugangsdaten oder privaten Schlüssel in dieses Verzeichnis legen.

Die folgende Liste beschreibt die Einrichtungsschritte für eine eigene Installation; im Besitzer-Projekt sind sie bereits ausgeführt.''')
p.write_text(s,encoding='utf-8')
p=root/'Documentation/Arbeitsregeln.md';s=p.read_text(encoding='utf-8')
s=s.replace('# Verbindliche Nutzerregel für TreasureRunner\n',
'''# Verbindliche Nutzerregel für TreasureRunner

Aktueller Nachtrag 26.10.2026 nach Nutzerauftrag „erneut versuchen“: technische Freigabestörung behoben. Firestore eur3/Spark erstellt, getestete Regeln aktiv rückgelesen, anonymer Zugriff403. Google-Login in finaler0.9-APK aktiv und echtes Nickname-Profil auf A55 vor/nach Neustart bestätigt. APK ohne Datenlöschung aktualisiert, drei eigene Leveldateien unverändert. Android-/beide Windows-Bauten und native Werkzeugprüfungen bestanden. Veröffentlichung0.9 weiterhin ausdrücklich autorisiert; ältere Einträge über fehlende Konfiguration/keinen Bau sind historischer Zwischenstand. Keine neue Freigabe für spätere Versionen daraus ableiten.
''',1)
p.write_text(s,encoding='utf-8')
print('Observed Google/device checks recorded; current setup documentation refreshed.')
