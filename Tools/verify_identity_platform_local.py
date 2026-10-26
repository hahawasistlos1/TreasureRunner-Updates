"""Read-only checks of release artifacts; writes local verification reports only."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def digest(path):
    return hashlib.file_digest(path.open('rb'), 'sha256').hexdigest()
def checks(path, marker):
    text = (root / path).read_text(encoding='utf-8-sig')
    assert marker in text and not any(line.startswith(('FAIL:', 'UNITY ERROR:')) for line in text.splitlines()), path
    return {'report': path, 'passed': sum(line.startswith('PASS:') for line in text.splitlines()), 'sha256': digest(root / path)}

expected = {
    'TreasureRunner.apk': '5f0fa38eff5583a6c45f88a85de1247729bdb7b97dc6ecd27eff52bdf45fc739',
    'TreasureRunner-Windows-Werkzeuge.zip': '78c457beebddfc228db2d079d8a25fe11604eb71f243ed745c8d2097f2ebc71a',
    'TreasureRunner-Source-0.8.0.zip': '8d1bc4f03be348ad17f2bdcda84a908ee413826060611353803516649aee0a29'
}
artifacts = {}
for name, value in expected.items():
    path = root / 'Builds/Publish-0.8.0' / name
    actual = digest(path)
    assert actual == value, f'Published artifact changed: {name}'
    artifacts[name] = {'sha256': actual, 'bytes': path.stat().st_size}
assert 'Version="0.8.0"' in (root / 'Assets/Scripts/Core/ReleaseInfo.cs').read_text()
assert 'AndroidCode=9' in (root / 'Assets/Scripts/Core/ReleaseInfo.cs').read_text()
settings = (root / 'ProjectSettings/ProjectSettings.asset').read_text()
assert 'bundleVersion: 0.8.0' in settings and 'AndroidBundleVersionCode: 9' in settings
assert not json.loads((root / 'Assets/Resources/IdentityConfig.json').read_text())['enabled']
assert 'BUILD SUCCESSFUL' in (root / 'Logs/GoogleIdentity/NativeCompileFinal.log').read_text()
assert 'no APK' in (root / 'Logs/GoogleIdentity/AndroidScripts.txt').read_text()
assert (root / 'Logs/GoogleIdentity/PlayerScripts/Assembly-CSharp.dll').exists()

groups = {
    'PlatformCollision': {
        'checks': checks('Logs/PlatformCollision/Checks.txt', 'ALL PLATFORM COLLISION CHECKS PASSED'),
        'controlRegression': checks('Logs/PlatformCollision/ControlRegression.txt', 'ALL CONTROL CHOICE TESTS PASSED'),
        'sources': ['Assets/PlayerMovement.cs', 'Assets/Scripts/Levels/AdventureMechanics.cs', 'Assets/Scripts/Levels/LevelFactory.cs'],
        'remaining': ['Clarify which other yellow objects the user means; fixed yellow moving block and solid treasure block verified.']
    },
    'GoogleIdentity': {
        'checks': checks('Logs/GoogleIdentity/Checks.txt', 'ALL IDENTITY CHECKS PASSED'),
        'nativeJava': 'Logs/GoogleIdentity/NativeCompileFinal.log',
        'androidScriptsOnly': 'Logs/GoogleIdentity/AndroidScripts.txt',
        'sources': ['Assets/Scripts/Social/GoogleIdentity.cs', 'Assets/Scripts/Social/NicknamePolicy.cs', 'Assets/Scripts/UI/GoogleAccountPanel.cs', 'Assets/Scripts/UI/MenuProfilePages.cs', 'Assets/Resources/IdentityConfig.json', 'Assets/Editor/TreasureRunnerIdentityAndroidSetup.cs', 'Assets/Plugins/Android/TreasureIdentity.androidlib/build.gradle', 'Assets/Plugins/Android/TreasureIdentity.androidlib/AndroidManifest.xml', 'Assets/Plugins/Android/TreasureIdentity.androidlib/consumer-rules.pro', 'Assets/Plugins/Android/TreasureIdentity.androidlib/src/com/treasurerunner/identity/IdentityBridge.java', 'Cloud/Identity/firestore.rules'],
        'remaining': ['Owner Firebase/Google project and OAuth client configuration', 'Deploy reviewed rules to that project', 'Real Android Google login and first nickname selection test after separately authorized APK build'],
        'realGoogleLoginTested': False,
        'cloudRulesDeployed': False
    }
}
if (root / 'Logs/GoogleIdentity/RulesChecks.txt').exists():
    groups['GoogleIdentity']['securityRules'] = checks('Logs/GoogleIdentity/RulesChecks.txt', 'ALL FIRESTORE IDENTITY RULES CHECKS PASSED')
for group, data in groups.items():
    data['sources'] = {path: digest(root / path) for path in data['sources']}
    data.update({'verifiedUtc': datetime.now(timezone.utc).isoformat(), 'publishedArtifactsUnchanged': artifacts, 'version': '0.8.0', 'androidCode': 9, 'apkBuilt': False, 'versionChanged': False, 'published': False})
    (root / 'Logs' / group / 'Verification.json').write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print('Local verification reports written. Release files and version unchanged; no APK built.')
