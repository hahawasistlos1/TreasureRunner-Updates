from pathlib import Path
import hashlib,json
from datetime import datetime,timezone
root=Path(__file__).resolve().parents[2]
def digest(p):
    with p.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def checked(relative,marker):
    p=root/relative;text=p.read_text(encoding='utf-8-sig')
    assert marker in text and not any(line.startswith(('FAIL:','ERROR:','UNITY ERROR:')) for line in text.splitlines()),relative
    return {'passed':sum(line.startswith('PASS:') for line in text.splitlines()),'sha256':digest(p),'report':relative}
state=json.loads((root/'Logs/EditorCommunity/Progress.json').read_text(encoding='utf-8-sig'));assert state.get('status')=='passed',state
groups={
 'features':checked('Logs/EditorCommunity/Checks.txt','ALL EDITOR COMMUNITY CHECKS PASSED'),
 'liveEditor':checked('Logs/LiveEditor/Checks.txt','ALL LIVE EDITOR CHECKS PASSED'),
 'platforms':checked('Logs/PlatformCollision/Checks.txt','ALL PLATFORM COLLISION CHECKS PASSED'),
 'reviewRules':checked('Logs/EditorCommunity/ReviewRulesChecks.txt','ALL COMMUNITY REVIEW RULES CHECKS PASSED'),
 'identityRules':checked('Logs/GoogleIdentity/RulesChecks.txt','ALL FIRESTORE IDENTITY RULES CHECKS PASSED'),
 'windowsEditor':checked('Builds/AssetExpansion-Dev/Welteneditor/Pruefung/editor.txt','SMOKE PASSED'),
 'blockedAdmin':checked('Builds/AssetExpansion-Dev/Welteneditor/Pruefung/blocked-admin.txt','SMOKE PASSED'),
 'privateAdmin':checked('Builds/AssetExpansion-Dev/Verwaltung/Pruefung/admin.txt','SMOKE PASSED')}
expected={
 'Builds/Publish-0.9.0/TreasureRunner.apk':'fab8f7862352dd9469d7ddc739464252d750ba0319220d59439070920e47b20a',
 'Builds/Publish-0.9.0/TreasureRunner-Windows-Editor.zip':'94d74a81683bb1542dbe2a92a2f716be44051a426de00712700c6018a87dd720',
 'Builds/BowFix-Test/TreasureRunner-BowFix-Test.apk':'cb40dd022ee735c67491c51810c2710406b998e15c3cb27d6cc34026096e07a2'}
for path,sha in expected.items():assert digest(root/path)==sha,path
assert 'BUILD SUCCESSFUL' in (root/'Logs/EditorCommunity/NativeJava.log').read_text()
assert 'no APK built' in (root/'Logs/AssetExpansion/AndroidScripts.txt').read_text()
settings=(root/'ProjectSettings/ProjectSettings.asset').read_text();assert 'bundleVersion: 0.9.0' in settings and 'AndroidBundleVersionCode: 10' in settings
rules=(root/'Cloud/Identity/firestore.rules').read_text().strip()
assert (root/'Backups/EditorCommunity/firestore-active-after.rules').read_text().strip()==rules
cloud=json.loads((root/'Logs/EditorCommunity/CloudAccess.json').read_text());assert [c['http'] for c in cloud['checks']]==[200,403,403,403,403]
files=['Assets/Scripts/Levels/SecretRoom.cs','Assets/Scripts/Levels/CommunityLevel.cs','Assets/Scripts/UI/EditorLevelScene.cs','Assets/Scripts/UI/LevelEditorScreen.cs','Assets/Scripts/UI/EditorPreviewProjectile.cs','Assets/Scripts/Levels/WorldTrap.cs','Assets/Scripts/Levels/AdventureMechanics.cs','Assets/Scripts/Levels/AssetHazard.cs','Assets/Scripts/Enemy.cs','Assets/Scripts/EnemyPatrol.cs','Assets/Scripts/Enemies/EnemyRangedAttack.cs','Assets/Scripts/Enemies/LibraryEnemyEquipment.cs','Assets/Scripts/Social/CommunityReviews.cs','Assets/Scripts/UI/CommunityReviewPanel.cs','Assets/Scripts/UI/CommunityLevelsPage.cs','Assets/Scripts/Tools/LevelModeration.cs','Assets/Plugins/Android/TreasureIdentity.androidlib/src/com/treasurerunner/identity/IdentityBridge.java','Assets/Plugins/Android/TreasureIdentity.androidlib/src/com/treasurerunner/identity/CommunityReviewBridge.java','Assets/Plugins/Android/TreasureIdentity.androidlib/consumer-rules.pro','Cloud/Identity/firestore.rules']
report={'verifiedUtc':datetime.now(timezone.utc).isoformat(),'groups':groups,'cloudReadChecks':cloud,'sources':{f:digest(root/f) for f in files},'unchangedArtifacts':expected,'version':'0.9.0','androidCode':10,'apkBuilt':False,'versionChanged':False,'gamePublished':False,'cloudRulesActivated':True,'newDeviceTest':False,'realReviewWritten':False,'billingChanged':False}
(root/'Logs/EditorCommunity/Verification.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Verified',sum(v['passed'] for v in groups.values())+len(cloud['checks']),'checks; immutable artifacts and version unchanged. No APK or game release.')
