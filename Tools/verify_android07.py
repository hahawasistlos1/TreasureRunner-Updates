from pathlib import Path
import zipfile,hashlib,json,subprocess,re
p=Path('Builds/TreasureRunner.apk');sdk=Path('C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK');aapt=sdk/'build-tools/36.0.0/aapt.exe'
badging=subprocess.check_output([str(aapt),'dump','badging',str(p)],text=True,encoding='utf-8',errors='replace')
baseline=subprocess.check_output([str(aapt),'dump','badging','Builds/Archive/TreasureRunner-0.6.0.apk'],text=True,encoding='utf-8',errors='replace')
assert re.search(r"package: name='([^']+)'",badging).group(1)==re.search(r"package: name='([^']+)'",baseline).group(1), 'Application identity changed'
assert "sdkVersion:'26'" in badging
assert "versionName='0.7.0'" in badging and "versionCode='8'" in badging
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(p),'AndroidManifest.xml'],text=True,encoding='utf-8',errors='replace')
assert 'REQUEST_INSTALL_PACKAGES' in manifest and 'UpdateInstalledReceiver' in manifest and 'UpdateProvider' in manifest and 'android:debuggable' not in manifest
Path('Logs/AndroidManifest07.txt').write_text(manifest,encoding='utf-8')
with zipfile.ZipFile(p) as z:
 assert z.testzip() is None and 'lib/arm64-v8a/libil2cpp.so' in z.namelist()
 forbidden=[n for n in z.namelist() if n.endswith(('.cs','.java','.keystore','.jks'))];assert not forbidden
 dex=b''.join(z.read(n) for n in z.namelist() if n.endswith('.dex'))
 for name in [b'UpdateBridge',b'UpdateProvider',b'UpdateInstalledReceiver',b'shareLevel']:assert name in dex
 icons=re.findall(r"application-icon-\d+:'([^']+)'",badging);assert icons,'Application icon missing'
 for i,icon in enumerate(dict.fromkeys(icons)):
  if icon.endswith('.png'):Path(f'Builds/Revision07Preview/Android-Icon-{i}.png').write_bytes(z.read(icon))
sha=hashlib.sha256(p.read_bytes()).hexdigest();Path(str(p)+'.sha256').write_text(sha+'  TreasureRunner.apk\n')
d={'version':'0.7.0','versionCode':8,'bytes':p.stat().st_size,'sha256':sha,'zip_integrity':True,'arm64':True,'native_updater_and_level_share':True,'debuggable':False,'source_or_key_files_in_apk':forbidden,'device_test':False,'application_icons':icons}
Path('Logs/ArtifactVerification07.json').write_text(json.dumps(d,indent=2));print(json.dumps(d,indent=2))
