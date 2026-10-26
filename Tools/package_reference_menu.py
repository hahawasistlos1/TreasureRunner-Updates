from pathlib import Path
import zipfile, json, hashlib

root=Path(__file__).resolve().parent.parent
target=root/'Builds/TreasureRunner-Menue-Referenz-Projektdateien.zip'
reports=['ReferenceMenuTests.txt','ReferenceMenuRegression.txt','InviteTests.txt']
for name in reports:
    text=(root/'Logs'/name).read_text(encoding='utf-8')
    assert text.rstrip().splitlines()[-1].startswith('ALL '),name
files=[]
for folder in ['Assets/Scripts','Assets/Resources/MenuArt','Assets/Resources/Fonts']:
    files.extend(p for p in (root/folder).rglob('*') if p.is_file())
for name in ['Assets/Editor/TreasureRunnerReferenceMenuTests.cs','Assets/Editor/TreasureRunnerReferenceMenuTests.cs.meta','Documentation/Menue-Referenz-1zu1.md','Documentation/MenuArt-Prompts.txt','Logs/ReferenceMenuArtifacts.json']:
    files.append(root/name)
files.extend(root/'Logs'/name for name in reports)
files.extend((root/'Builds/ReferenceMenuPreview').glob('*.png'))
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
    archive.writestr('README-Menue-Referenz.txt','TreasureRunner: funktionierendes Menü nach der eigenen Nutzer-Vorlage.\nErgänzung des vorhandenen Unity-Projekts, kein eigenständig installierbares Spiel. Enthält lokale Quellen, eigene Menügrafiken, Schriften/Lizenzen, Vorschauen und Prüfberichte.\nUnveränderte fremde Modelle, Packages und Szenengrundlage sind nicht erneut enthalten. Kein neuer Android-/Windows-Build und keine Versionsänderung.\nVersion bleibt 0.7.0 / Android-Code 8. Vorschauen verwenden einen Testspielstand.\n')
    for file in sorted(set(files)):
        assert file.is_file(),file
        archive.write(file,file.relative_to(root).as_posix())
with zipfile.ZipFile(target) as archive:
    assert archive.testzip() is None
    names=archive.namelist()
    assert not any(n.endswith(('.apk','.exe','.jks','.keystore')) for n in names)
result={'file':str(target.relative_to(root)),'bytes':target.stat().st_size,'files':len(names),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'checks':sum(sum(line.startswith('PASS:') for line in (root/'Logs'/name).read_text().splitlines()) for name in reports),'apk_built':False}
(root/'Logs/ReferenceMenuPackage.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
