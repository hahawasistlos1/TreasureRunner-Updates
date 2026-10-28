"""Prepare reviewed own sources in the existing Git checkout; does not commit or push."""
from pathlib import Path
import hashlib,json,re,shutil
root=Path(__file__).resolve().parents[1];target=root/'Builds/Publish-0.10.0/GitSource'
assert (target/'.git').is_dir(), 'Clone existing repository first'
files=set()
text_ext={'.cs','.java','.gradle','.xml','.properties','.meta','.py','.ps1','.cjs','.mjs','.html','.json','.yaml','.rules','.pro','.shader','.md'}
for folder in ['Assets/Scripts','Assets/Editor','Assets/Plugins/Android','Tools','Patches','Web/InviteSite','Cloud/Identity']:
    for p in (root/folder).rglob('*'):
        parts=p.relative_to(root/folder).parts
        if any(s in {'node_modules','build','.gradle','__pycache__'} for s in parts):continue
        if p.name in {'export_source07.py','package_release08.py','device08.py','prepare_device08_level.py'} or 'device' in p.name.lower() or 'debug.log' in p.name:continue
        if p.is_file() and p.suffix in text_ext:files.add(p)
for folder in ['Assets/Resources/MenuArt','Assets/Resources/Fonts']:
    files.update(p for p in (root/folder).rglob('*') if p.is_file())
for name in ['Assets/PlayerMovement.cs','Assets/PlayerMovement.cs.meta','Assets/Resources/UpdateChannel.json','Assets/Resources/CommunityStarter.json','Assets/Resources/IdentityConfig.json','Assets/Resources/ComicSurface.shader','Assets/Resources/ShieldRing.mat','Assets/Resources/ShieldRing.mat.meta','Assets/Resources/Brand/TreasureRunnerIcon.png','Assets/Resources/OpenArt/manifest.json','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','Tools/TreasureRunner.ico','Tools/TreasureRunner-v2.ico','Assets/Resources/Brand/TreasureRunnerIcon-v2.png','Assets/Resources/AssetLibrary/Catalog.json',
             'Documentation/Assetbibliothek-0.10.md','Documentation/Live-Levelbau.md','Documentation/Jaegerbogen-Grafikfehler.md','Documentation/Credits.md','Documentation/Asset-Herkunft.md','Documentation/Windows-Welteneditor-0.10.md','Documentation/Release-0.10.0.md','Documentation/Google-Anmeldung-Nickname.md','Documentation/Plattform-Kollisionen.md','Documentation/Automatische-Patchupdates.md','Documentation/Updatebenachrichtigungen-Hintergrund.md','Documentation/Waffen-Faehigkeiten-Fortschritt.md','Documentation/Touch-Tastengroesse.md','Documentation/Freundeseinladungen.md','Documentation/Patch-Funktion.md','Documentation/MenuArt-Prompts.txt','Documentation/Menue-Referenz-1zu1.md']:
    p=root/name
    if p.exists():files.add(p)
files.update(p for p in (root/'Assets/Resources/AssetLibrary/Licenses').glob('*.txt'))
assert json.loads((root/'Assets/Resources/IdentityConfig.json').read_text(encoding='utf-8-sig'))['firebaseProjectId']=='treasurerunner-2689b'
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
(target/'community/0.10.0').mkdir(parents=True,exist_ok=True)
catalog=json.loads((root/'Assets/Resources/CommunityStarter.json').read_text(encoding='utf-8-sig'))
catalog_path=target/'community/0.10.0/catalog.json'
if catalog_path.exists():
    existing=json.loads(catalog_path.read_text(encoding='utf-8'));assert existing['gameVersion']=='0.10.0' and existing['moderationProtocol']==4
else:catalog_path.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
(target/'.gitignore').write_text('Library/\nTemp/\nObj/\nLogs/\nBuilds/\nBackups/\nUserSettings/\nnode_modules/\n.gradle/\n.vs/\n*.csproj\n*.sln\n*.keystore\n*.jks\n.env*\n*.apk\n*.exe\n*.dll\n*-debug.log\n',encoding='utf-8')
(target/'.gitattributes').write_text('* text=auto\n',encoding='utf-8',newline='\n')
(target/'README.md').write_text('# TreasureRunner\n\nAndroid-Jump-and-Run mit acht Welten, 80 Kampagnenleveln, acht Waffen, Figurenfähigkeiten und gemeinsamem Welteneditor.\n\n## Version 0.10.0\n\n- [Android-APK](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.10.0/TreasureRunner.apk)\n- [Windows-Welteneditor](https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.10.0/TreasureRunner-Windows-Editor.zip)\n- [Änderungen und Prüfgrenzen](Documentation/Release-0.10.0.md)\n- [Windows-Anleitung](Documentation/Windows-Welteneditor-0.10.md)\n- [Eigener Spielcode](Assets/Scripts)\n\nNeuer Vollbildeditor mit Zahnrad für die gemeinsame Assetpalette und getrenntem Funktionsmenü. Leere Welt mit bis zu 384 × 32 Feldern und 2400 Bauteilen; Bauen und Löschen bei laufender Gegner-/Fallensimulation, verschiebbare Ansicht und Randscrollen. Eigene Bauplätze werden ausdrücklich gespeichert und geladen. Testspielen ist freiwillig, Einreichen führt zur Betreiberentscheidung. [Assetbibliothek und Quellen](Documentation/Assetbibliothek-0.10.md).\n\nDie separate Verwaltung bleibt beim Betreiber und gehört nicht zum öffentlichen Windows-Download. APK und aktuelle Verwaltung verwenden ausschließlich community/0.10.0/catalog.json mit Protokoll4 und passender Spielversion. Alte Werkzeuge schreiben ihre bisherigen Kataloge. Eigene ältere Level bleiben importierbar; kein neuer Spielstandsreset.\n\nGeheimtüren verschwinden nach Schatzfund und Verlassen für den Versuch. Jägerbogen bei Richtungswechseln und Upgrades korrigiert. Freigegebene Community-Level bieten Sterne und optionale Kommentare unter eigenem Nickname. Google-Anmeldung ist eingerichtet; keine Google-E-Mail oder Provider-Profilnamen im Spielprofil. Firebase Spark/Europa eur3; keine Cloud-Spielstandssynchronisierung. Der neue Katalog startet leer, daher noch kein regulärer öffentlicher Bewertungsschreibtest.\n\n## Quellen und Lizenzen\n\nEigener C#-/Java-/Shader-Code, Editor- und Buildwerkzeuge, eigene Menügrafiken und lizenzierte Schriften stehen als Git-Dateien bereit. Kein zusätzlicher Source-ZIP-Anhang. Dieser Quellcode-Auszug ist kein vollständig startfertiges Unity-Projekt: fremde Originalmodelle, ursprüngliche Szenen und benötigte Unity-Pakete sind rechtmäßig zu ergänzen. Keine privaten Signierschlüssel, Tokens, Spielstände oder Build-Caches. Für eigenen Code wird keine zusätzliche Open-Source-Lizenz eingeräumt. [Credits](Documentation/Credits.md).\n\nUnity6000.3.17f1/URP, Android ARM64 ab API26, Windows x64. Build-Einstiege TreasureRunnerRelease10.Android, .WindowsEditor, .WindowsAdmin. Verwaltungscode ist einsehbar; öffentliche Freigaben werden durch die GitHub-Schreibrechte des Betreibers geschützt.\n\n## Einladungen und Updates\n\nDie [Einladungsseite](https://hahawasistlos1.github.io/treasurerunner/invite/) öffnet die App oder bietet den offiziellen Latest-APK-Download. Nach Installation dieselbe Einladung erneut öffnen. Kleine Datenpatches verändern nur unterstützte Daten; neuer Code und neue Assets benötigen eine APK. 0.10.0 ist ein normales APK-Update, kein zusätzlicher Datenpatch. Download über die bestehende Updatefunktion, Android-Installation manuell; Hintergrundprüfungen unterliegen Android-Zeitplänen und Berechtigungen.\n',encoding='utf-8')
(root/'Logs/Release10/GitSourceManifest.json').write_text(json.dumps({'files':record,'count':len(record),'noSourceZip':True,'noThirdPartyOriginalModels':True,'privateCredentials':False},indent=2)+'\n',encoding='utf-8')
print(f'Prepared {len(record)} own source/art files as Git files; no source ZIP.')
