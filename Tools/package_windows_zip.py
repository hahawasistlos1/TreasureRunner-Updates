import hashlib, json, pathlib, re, shutil, zipfile

root=pathlib.Path(__file__).resolve().parents[1]
folder=root/'Builds/Windows-Werkzeuge'
for mode in ['editor','admin']:
    assert (folder/'Pruefung'/f'{mode}.txt').read_text(encoding='utf8').strip().endswith('SMOKE PASSED'), f'{mode} has not passed'
for name in ['TreasureRunner-Levelbau.exe','TreasureRunner-Verwaltung.exe','Runtime/TreasureRunnerTools.exe','Bitte-zuerst-lesen.md','Lizenzen/Credits.md']:
    assert (folder/name).is_file(), f'Missing {name}'
settings=(root/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf8')
assert re.search(r'bundleVersion: 0\.7\.0\s',settings)
assert 'AndroidBundleVersionCode: 8' in settings
apksha=hashlib.sha256((root/'Builds/Archive/TreasureRunner-0.6.0.apk').read_bytes()).hexdigest()
assert apksha=='c7c297b8fcd92e6392712c515e00e6b3301d1a155ce21c80495a39636f7cfbae'
preview=folder/'Vorschau';preview.mkdir(exist_ok=True)
for name in ['Levelbau','Bauteile','Verwaltung','Nutzer-Level-1']:
    p=folder/'Pruefung'/f'{name}.png'
    if p.exists():shutil.copyfile(p,preview/p.name)
archive=root/'Builds/TreasureRunner-Windows-Werkzeuge.zip'
count=0
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as z:
    for p in sorted(folder.rglob('*')):
        relative=p.relative_to(folder)
        if not p.is_file() or relative.parts[0]=='Pruefung' or any('DoNotShip' in s or 'DontShip' in s for s in relative.parts):continue
        z.write(p,pathlib.PurePosixPath('TreasureRunner-Windows-Werkzeuge',*relative.parts));count+=1
report={'archive':str(archive),'bytes':archive.stat().st_size,'files':count,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),
        'gameVersion':'0.7.0','androidCode':8,'baselineApkSha256':apksha,'published':False,
        'checks':{'gameAndCatalog':sum(1 for line in (root/'Logs/ToolsTests.txt').read_text(encoding='utf8').splitlines() if line.startswith('PASS:')),'hearts':33,'windowsEditor':'passed','windowsModerator':'passed'}}
(root/'Logs/WindowsToolsVerification.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
