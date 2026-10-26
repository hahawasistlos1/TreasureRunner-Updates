"""Prepare reviewed own sources in the existing Git checkout; does not commit or push."""
from pathlib import Path
import hashlib,json,re,shutil
root=Path(__file__).resolve().parents[1];target=root/'Builds/Publish-0.9.0/GitSource'
assert (target/'.git').is_dir(), 'Clone existing repository first'
files=set()
text_ext={'.cs','.java','.gradle','.xml','.properties','.meta','.py','.ps1','.cjs','.mjs','.html','.json','.yaml','.rules','.pro','.shader','.md'}
for folder in ['Assets/Scripts','Assets/Editor','Assets/Plugins/Android','Tools','Patches','Web/InviteSite','Cloud/Identity']:
    for p in (root/folder).rglob('*'):
        parts=p.relative_to(root/folder).parts
        if any(s in {'node_modules','build','.gradle','__pycache__'} for s in parts):continue
        if p.name in {'export_source07.py','package_release08.py','device08.py','prepare_device08_level.py'} or 'debug.log' in p.name:continue
        if p.is_file() and p.suffix in text_ext:files.add(p)
for folder in ['Assets/Resources/MenuArt','Assets/Resources/Fonts']:
    files.update(p for p in (root/folder).rglob('*') if p.is_file())
for name in ['Assets/PlayerMovement.cs','Assets/PlayerMovement.cs.meta','Assets/Resources/UpdateChannel.json','Assets/Resources/CommunityStarter.json','Assets/Resources/IdentityConfig.json','Assets/Resources/ComicSurface.shader','Assets/Resources/ShieldRing.mat','Assets/Resources/ShieldRing.mat.meta','Assets/Resources/Brand/TreasureRunnerIcon.png','Assets/Resources/OpenArt/manifest.json','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','Tools/TreasureRunner.ico',
             'Documentation/Credits.md','Documentation/Asset-Herkunft.md','Documentation/Windows-Welteneditor-0.9.md','Documentation/Release-0.9.0.md','Documentation/Google-Anmeldung-Nickname.md','Documentation/Plattform-Kollisionen.md','Documentation/Automatische-Patchupdates.md','Documentation/Updatebenachrichtigungen-Hintergrund.md','Documentation/Waffen-Faehigkeiten-Fortschritt.md','Documentation/Touch-Tastengroesse.md','Documentation/Freundeseinladungen.md','Documentation/Patch-Funktion.md','Documentation/MenuArt-Prompts.txt','Documentation/Menue-Referenz-1zu1.md']:
    p=root/name
    if p.exists():files.add(p)
assert json.loads((root/'Assets/Resources/IdentityConfig.json').read_text())['firebaseProjectId']=='treasurerunner-2689b'
# Remove only these generated, unpublished local test helpers from the export.
for relative in ['Tools/device08.py','Tools/prepare_device08_level.py']:
    obsolete=target/relative
    assert obsolete.resolve().is_relative_to(target.resolve())
    if obsolete.exists():obsolete.unlink()
record=[]
for p in sorted(files):
    if p.suffix not in {'.png','.ico','.ttf'}:
        text=p.read_text(encoding='utf-8-sig')
        assert not re.search(r'gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',text),p
    relative=p.relative_to(root);destination=target/relative
    destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,destination)
    record.append({'path':relative.as_posix(),'sha256':hashlib.file_digest(p.open('rb'),'sha256').hexdigest(),'bytes':p.stat().st_size})
(target/'community/0.9.0').mkdir(parents=True,exist_ok=True)
catalog=json.loads((root/'Assets/Resources/CommunityStarter.json').read_text())
(target/'community/0.9.0/catalog.json').write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
(target/'.gitignore').write_text('Library/\nTemp/\nObj/\nLogs/\nBuilds/\nBackups/\nUserSettings/\nnode_modules/\n.gradle/\n.vs/\n*.csproj\n*.sln\n*.keystore\n*.jks\n.env*\n*.apk\n*.exe\n*.dll\n*-debug.log\n',encoding='utf-8')
(target/'.gitattributes').write_text('* text=auto\n',encoding='utf-8',newline='\n')
(target/'README.md').write_text('''# TreasureRunner

Android-Jump-and-Run mit acht Welten, 80 Kampagnenleveln, acht Waffen, Figurenfähigkeiten und einem gemeinsamen Welteneditor.

## Version 0.9.0

- [Android-APK](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/TreasureRunner.apk)
- [Windows-Welteneditor](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/TreasureRunner-Windows-Editor.zip)
- [Änderungen und Release](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/tag/v0.9.0)
- [Eigener Spielcode direkt im Git-Repository](Assets/Scripts)
- [Anleitung zum Windows-Editor](Documentation/Windows-Welteneditor-0.9.md)

Das öffentliche Windows-Programm enthält ausschließlich den Welteneditor. Testspielen ist freiwillig; **Einreichen** schickt ein Level zur Entscheidung an den Betreiber. Nur hahawasistlos1 gibt Level öffentlich frei. Die separate Verwaltung wird nicht als Download verteilt. Android und die aktuelle Verwaltung nutzen ausschließlich `community/0.9.0/catalog.json` mit Protokoll 4 und passender Spielversion. Alte Verwaltungsprogramme können die neue Community-Auswahl über ihre alten Kataloge nicht ändern. Vorhandene eigene Codes/Bauplätze bleiben importierbar.

Größere und selbst skalierbare Touch-Tasten, zusätzliche Waffen und Fähigkeiten, korrigierte bewegliche Plattformen, automatische Datenpatches ohne Rücknahme und Hintergrund-Updatebenachrichtigungen sind enthalten. APK-Installation bleibt manuell; Android steuert die Zeitpunkte der Hintergrundprüfungen. Kein neuer Spielstandsreset. Die APK wurde auf einem A55 aktualisiert; Google-Profil und Wiederherstellung nach App-Neustart sind geprüft. Eigene Leveldateien blieben unverändert. Kein vollständiger Durchspieltest aller 80 Level.

Google-Anmeldung mit selbst gewähltem Nickname ist für Android eingerichtet und auf dem A55 geprüft. Das Spielprofil zeigt keine Google-E-Mail, keinen Google-Profilnamen und kein Profilbild. Firebase läuft im kostenlosen Spark-Tarif, die Nickname-Datenbank liegt in Europa (eur3). Freunde/Spielstände werden nicht automatisch online synchronisiert. [Einrichtung und Datenfluss](Documentation/Google-Anmeldung-Nickname.md).

## Quellen als Git-Dateien

Eigener C#-/Java-/Shader-Code, Editoren, Build-/Prüfwerkzeuge, eigene Menügrafiken und Schriften mit Lizenzen stehen direkt in der Git-Historie. Kein zusätzlicher Source-ZIP-Anhang. Dies ist ein Quellcode-Auszug, **kein vollständig startfertiges Unity-Projekt**: fremde Originalmodelle, ursprüngliche Szenengrundlage und Unity-Pakete müssen rechtmäßig ergänzt werden. Originalmodelle aus dem Asset Store, private Signierschlüssel, Tokens, Spielstände und Build-Caches werden nicht veröffentlicht. Für den eigenen Code wird keine zusätzliche Open-Source-Lizenz eingeräumt. [Credits und Asset-Quellen](Documentation/Credits.md).

Entwicklungsumgebung: Unity 6000.3.17f1 / URP, Android ARM64 ab API 26 und Windows x64. Aktuelle Build-Einstiege: `TreasureRunnerRelease09.Android`, `.WindowsEditor`, `.WindowsAdmin`. `.WindowsAdmin` ist eine eigene lokale Ausgabe und kein Teil des öffentlichen Windows-Pakets. Die Repository-Schreibrechte schützen die Freigaben auch dann, wenn jemand den öffentlich einsehbaren Verwaltungscode selbst kompiliert. Builds/Versionserhöhungen/Veröffentlichungen nur auf ausdrücklichen Besitzerauftrag.

## Einladungen und Updates

Die [Einladungsseite](https://hahawasistlos1.github.io/treasurerunner/invite/) öffnet unterstützte Apps oder bietet den offiziellen APK-Download. Nach Installation dieselbe Einladung erneut öffnen. Kleine Datenpatches verändern nur unterstützte Daten; neuer Code und neue Assets benötigen eine APK. Derzeit wird mit 0.9.0 kein zusätzlicher Datenpatch veröffentlicht.
''',encoding='utf-8')
(root/'Logs/Release09/GitSourceManifest.json').write_text(json.dumps({'files':record,'count':len(record),'noSourceZip':True,'noThirdPartyOriginalModels':True,'privateCredentials':False},indent=2)+'\n',encoding='utf-8')
print(f'Prepared {len(record)} own source/art files as Git files; no source ZIP.')
