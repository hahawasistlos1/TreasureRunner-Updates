from pathlib import Path
import subprocess, re, json, hashlib, zipfile
root=Path(__file__).resolve().parents[1]
apk=root/'Builds/Publish-0.8.0/TreasureRunner.apk'
android=Path('C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer')
sdk=android/'SDK/build-tools/36.0.0'
badging=subprocess.check_output([str(sdk/'aapt.exe'),'dump','badging',str(apk)],text=True,encoding='utf-8',errors='replace')
assert "package: name='com.treasurerunner.game'" in badging and "versionName='0.8.0'" in badging and "versionCode='9'" in badging
assert "sdkVersion:'26'" in badging and "native-code: 'arm64-v8a'" in badging
manifest=subprocess.check_output([str(sdk/'aapt.exe'),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8',errors='replace')
for part in ['UnityPlayerGameActivity','hahawasistlos1.github.io','/treasurerunner/invite/','treasurerunner','BROWSABLE','UpdateProvider','UpdateInstalledReceiver','REQUEST_INSTALL_PACKAGES']:
    assert part in manifest,part
assert 'android:autoVerify' in manifest and 'android:debuggable' not in manifest
(root/'Logs/AndroidManifest08.txt').write_text(manifest,encoding='utf-8')
signed=subprocess.check_output([str(android/'OpenJDK/bin/java.exe'),'-jar',str(sdk/'lib/apksigner.jar'),'verify','--verbose','--print-certs',str(apk)],text=True,encoding='utf-8',errors='replace')
certificate=re.search(r'certificate SHA-256 digest: ([a-f0-9]{64})',signed).group(1)
baseline=re.search(r'certificate SHA-256 digest: ([a-f0-9]{64})',(root/'Logs/SignatureVerification07.txt').read_text()).group(1)
assert certificate==baseline and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signed
(root/'Logs/SignatureVerification08.txt').write_text('Verified using v2 scheme (APK Signature Scheme v2): true\nSigner #1 certificate SHA-256 digest: '+certificate+'\n',encoding='utf-8')
with zipfile.ZipFile(apk) as archive:
    assert archive.testzip() is None and 'lib/arm64-v8a/libil2cpp.so' in archive.namelist()
    assert not any(n.endswith(('.keystore','.jks','.java','.cs')) for n in archive.namelist())
    dex=b''.join(archive.read(n) for n in archive.namelist() if n.endswith('.dex'))
    for name in [b'UpdateBridge',b'shareLevel',b'shareInvite',b'UpdateInstalledReceiver']:
        assert name in dex,name
result={'version':'0.8.0','androidCode':9,'bytes':apk.stat().st_size,'sha256':hashlib.sha256(apk.read_bytes()).hexdigest(),'arm64':True,'signature_unchanged':True,'manual_installer_and_invite_sharing':True,'app_links_manifest':True,'device_test':False}
(root/'Logs/ArtifactVerification08.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
