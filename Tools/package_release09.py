from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[1]
out=root/'Builds/Publish-0.9.0';win=out/'Windows-Editor'
assert (win/'Pruefung/editor.txt').read_text().rstrip().endswith('SMOKE PASSED')
assert (win/'Pruefung/blocked-admin.txt').read_text().rstrip().endswith('SMOKE PASSED')
assert (root/'Builds/Private-0.9.0/Verwaltung/Pruefung/admin.txt').read_text().rstrip().endswith('SMOKE PASSED')
assert not (win/'TreasureRunner-Verwaltung.exe').exists()
archive=out/'TreasureRunner-Windows-Editor.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=4) as z:
    for p in sorted(win.rglob('*')):
        parts=p.relative_to(win).parts
        if p.is_file() and parts[0]!='Pruefung' and not any('DoNotShip' in a or 'DontShip' in a for a in parts):
            z.write(p,Path('TreasureRunner-Windows-Editor',*parts).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert not any('Verwaltung' in name or 'Private-' in name or 'Pruefung/' in name for name in z.namelist())
files=[out/'TreasureRunner.apk',archive]
report={'version':'0.9.0','androidCode':10,'sourceDelivery':'git','publicAssets':[{'name':p.name,'bytes':p.stat().st_size,'sha256':hashlib.file_digest(p.open('rb'),'sha256').hexdigest()} for p in files]}
(root/'Logs/Release09/Packages.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
