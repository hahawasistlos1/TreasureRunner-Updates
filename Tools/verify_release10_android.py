"""Validate the authorized 0.10.0 release APK without touching any published artifact or device."""
from pathlib import Path
import hashlib
import io
import json
import re
import subprocess
import zipfile
from PIL import Image, ImageChops, ImageStat

root = Path(__file__).resolve().parents[1]
apk = root / 'Builds/Publish-0.10.0/TreasureRunner.apk'
baseline = root / 'Builds/Publish-0.9.0/TreasureRunner.apk'
logs = root / 'Logs/Release10/Apk'
logs.mkdir(parents=True, exist_ok=True)
android = Path('C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer')
sdk = android / 'SDK/build-tools/36.0.0'

def run(*args):
    return subprocess.check_output([str(a) for a in args], text=True, encoding='utf-8', errors='replace')

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

assert apk.is_file(), 'Test APK missing'
badging = run(sdk / 'aapt.exe', 'dump', 'badging', apk)
for token in ["package: name='com.treasurerunner.game'", "versionName='0.10.0'", "versionCode='11'", "sdkVersion:'26'", "targetSdkVersion:'36'", "native-code: 'arm64-v8a'"]:
    assert token in badging, token
manifest = run(sdk / 'aapt.exe', 'dump', 'xmltree', apk, 'AndroidManifest.xml')
for token in ['UnityPlayerGameActivity', 'hahawasistlos1.github.io', '/treasurerunner/invite/', 'BROWSABLE', 'UpdateProvider', 'UpdateInstalledReceiver', 'REQUEST_INSTALL_PACKAGES', 'POST_NOTIFICATIONS', 'UpdateNotificationJob']:
    assert token in manifest, token
assert 'android:debuggable' not in manifest
assert 'FirebaseInitProvider' not in manifest
signature = run(android / 'OpenJDK/bin/java.exe', '-jar', sdk / 'lib/apksigner.jar', 'verify', '--verbose', '--print-certs', apk)
certificate = re.search(r'certificate SHA-256 digest: ([a-f0-9]{64})', signature).group(1)
assert certificate == '00d65b99df39cd8d0afbf390e365c08b4c9bcee8856369a67bc6cdd4d06b17ac', 'Signature differs from installed 0.9'
assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signature
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    names = z.namelist()
    assert 'lib/arm64-v8a/libil2cpp.so' in names
    metadata = z.read(next(n for n in names if n.endswith('/global-metadata.dat')))
    for token in [b'EditorLevelScene', b'LevelEditorScreen', b'SharedLinkInput', b'LibraryWorldArt', b'CampaignEnemyArt']:
        assert token in metadata, token
    for token in [b'CommunityReviewPanel',b'CommunityReviews',b'OnReviewResult',b'EditorPreviewProjectile',b'SetSimulation',b'RequiredFormat']:
        assert token in metadata,token
    for token in [b'DoorRetired',b'PanBy',b'OpenDocument',b'Gesture']:
        assert token in metadata,token
    for token in [b'LevelModerationScreen', b'ModeratorCredential']:
        assert token not in metadata, 'Private Windows administration must not ship in APK'
    dex = b''.join(z.read(n) for n in names if n.endswith('.dex'))
    for token in [b'IdentityBridge', b'GoogleIdTokenCredential', b'FirebaseAuth', b'FirebaseFirestore', b'CredentialManager', b'UpdateNotifications', b'UpdateNotificationJob']:
        assert token in dex, token
    assert b'CommunityReviewBridge' in dex and b'saveReview' in dex and b'deleteReview' in dex
    assert not any(n.endswith(('.java', '.cs', '.keystore', '.jks')) for n in names)
    icons = re.findall(r"application-icon-\d+:'([^']+)'", badging)
    assert icons, 'No launcher icon in APK'
    icon_path = icons[-1]
    icon = Image.open(io.BytesIO(z.read(icon_path))).convert('RGB')
    source = Image.open(root / 'Assets/Resources/Brand/TreasureRunnerIcon-v2.png').convert('RGB').resize(icon.size, Image.Resampling.LANCZOS)
    difference = ImageStat.Stat(ImageChops.difference(icon, source)).mean
    assert max(difference) < 25, ('Launcher icon does not match new source', difference)
    icon.save(logs / 'Launcher-Icon.png')
assert sha(baseline) == 'fab8f7862352dd9469d7ddc739464252d750ba0319220d59439070920e47b20a'
assert sha(root / 'Builds/Publish-0.9.0/TreasureRunner-Windows-Editor.zip') == '94d74a81683bb1542dbe2a92a2f716be44051a426de00712700c6018a87dd720'
assert sha(apk) != sha(baseline)
report = {'status': 'verified_release_apk', 'apk': str(apk), 'version': '0.10.0', 'android_code': 11, 'bytes': apk.stat().st_size,
          'sha256': sha(apk), 'signing_certificate_sha256': certificate, 'same_signing_certificate_as_09': True,
          'live_editor_metadata_included': True, 'new_launcher_icon_included': True, 'google_identity_and_background_updates_included': True,
          'private_administration_excluded': True, 'published_apk_and_windows_zip_unchanged': True,
          'published': False, 'device_installed_or_tested': False}
for name, value in [('Badging.txt', badging), ('AndroidManifest.txt', manifest), ('Signature.txt', signature)]:
    (logs / name).write_text(value, encoding='utf-8')
(logs / 'Verification.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps(report, indent=2, ensure_ascii=False))
