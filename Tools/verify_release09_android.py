from pathlib import Path
import hashlib,json,re,subprocess,zipfile
root=Path(__file__).resolve().parents[1];apk=root/'Builds/Publish-0.9.0/TreasureRunner.apk'
android=Path('C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer');sdk=android/'SDK/build-tools/36.0.0'
def aapt(*args):return subprocess.check_output([str(sdk/'aapt.exe'),*args],text=True,encoding='utf-8',errors='replace')
badging=aapt('dump','badging',str(apk))
for token in ["package: name='com.treasurerunner.game'","versionName='0.9.0'","versionCode='10'","sdkVersion:'26'","native-code: 'arm64-v8a'"]:assert token in badging,token
manifest=aapt('dump','xmltree',str(apk),'AndroidManifest.xml')
for token in ['UnityPlayerGameActivity','hahawasistlos1.github.io','/treasurerunner/invite/','BROWSABLE','UpdateProvider','UpdateInstalledReceiver','REQUEST_INSTALL_PACKAGES','POST_NOTIFICATIONS','UpdateNotificationJob']:
    assert token in manifest,token
assert 'android:debuggable' not in manifest
assert 'FirebaseInitProvider' not in manifest,'Do not auto-initialize a default Firebase app'
(root/'Logs/Release09/AndroidManifest.txt').write_text(manifest)
signed=subprocess.check_output([str(android/'OpenJDK/bin/java.exe'),'-jar',str(sdk/'lib/apksigner.jar'),'verify','--verbose','--print-certs',str(apk)],text=True,encoding='utf-8',errors='replace')
cert=re.search(r'certificate SHA-256 digest: ([a-f0-9]{64})',signed).group(1)
assert cert=='00d65b99df39cd8d0afbf390e365c08b4c9bcee8856369a67bc6cdd4d06b17ac'
assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signed
(root/'Logs/Release09/Signature.txt').write_text(signed)
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    assert 'lib/arm64-v8a/libil2cpp.so' in z.namelist()
    dex=b''.join(z.read(n) for n in z.namelist() if n.endswith('.dex'))
    for token in [b'IdentityBridge',b'GoogleIdTokenCredential',b'FirebaseAuth',b'FirebaseFirestore',b'CredentialManager',b'UpdateNotifications',b'UpdateNotificationJob']:
        assert token in dex,token
    assert not any(n.endswith(('.java','.cs','.keystore','.jks')) for n in z.namelist())
result={'version':'0.9.0','androidCode':10,'bytes':apk.stat().st_size,'sha256':hashlib.file_digest(apk.open('rb'),'sha256').hexdigest(),'signature_unchanged':True,'androidIdentityNativeIncluded':True,'backgroundUpdateNativeIncluded':True,'deviceLoginTested':False}
(root/'Logs/Release09/AndroidVerification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
