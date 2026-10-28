"""Collect actual R2 test/build evidence; never modify player data or publish."""
from pathlib import Path
import hashlib,json
from datetime import datetime,timezone

root=Path(__file__).resolve().parents[2]
out=root/'Logs/EditorWorkspace-R2'
def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def passed(path,marker):
    text=path.read_text(encoding='utf-8-sig')
    assert marker in text and not any(line.startswith(('FAIL:','ERROR:','UNITY ERROR:')) for line in text.splitlines()),path
    return {'report':str(path.relative_to(root)),'passed_assertions':sum(line.startswith('PASS:') for line in text.splitlines()),'sha256':sha(path)}

checks=[passed(out/'Checks.txt','ALL EDITOR COMMUNITY CHECKS PASSED'),passed(root/'Logs/LiveEditor/Checks.txt','ALL LIVE EDITOR CHECKS PASSED')]
for folder,case in [('Welteneditor','editor'),('Welteneditor','blocked-admin'),('Verwaltung','admin')]:
    checks.append(passed(root/f'Builds/AssetExpansion-Dev/{folder}/Pruefung/{case}.txt','SMOKE PASSED'))
assert json.loads((out/'Progress.json').read_text(encoding='utf-8-sig'))['status']=='passed'
apk=json.loads((out/'TestApk/Verification.json').read_text())
assert sha(Path(apk['apk']))==apk['sha256']
published={'Builds/Publish-0.9.0/TreasureRunner.apk':'fab8f7862352dd9469d7ddc739464252d750ba0319220d59439070920e47b20a',
'Builds/Publish-0.9.0/TreasureRunner-Windows-Editor.zip':'94d74a81683bb1542dbe2a92a2f716be44051a426de00712700c6018a87dd720',
'Builds/EditorCommunity-Test/TreasureRunner-EditorCommunity-Test.apk':'36a6ddfd951cded1e0bf8c519032597ac0d7745fd37e3ec25110aadc63abfaf4'}
for name,digest in published.items():assert sha(root/name)==digest,name
sources=['Assets/Scripts/UI/LevelEditorScreen.cs','Assets/Scripts/UI/EditorLevelScene.cs','Assets/Scripts/Levels/SecretRoom.cs','Assets/Scripts/Levels/CommunityLevel.cs',
'Assets/Scripts/TreasureGameBootstrap.cs','Assets/Scripts/UI/CommunityLevelsPage.cs','Assets/Scripts/Diagnostics/BuilderTestNavigation.cs',
'Assets/Editor/TreasureRunnerEditorCommunityTests.cs','Assets/Editor/TreasureRunnerLiveEditorTests.cs','Assets/Scripts/Tools/DesktopToolsSmoke.cs']
# Locate bootstrap by its actual repository path rather than assuming a folder.
sources=[str(next((root/'Assets').rglob(Path(s).name)).relative_to(root)) if not (root/s).is_file() else s for s in sources]
report={'recorded_utc':datetime.now(timezone.utc).isoformat(),'status':'local_test_build_verified','checks':checks,'package_verification_before_install':apk,
'source_sha256':{name:sha(root/name) for name in sources},'immutable_artifacts_unchanged':published,'version_changed':False,'published':False,'device':{'tested':False}}
backup=root/'Backups/EditorWorkspace-R2-Device'
if (backup/'AfterTest/manifest.json').is_file():
    initial=json.loads((backup/'BeforeInstallFinal/manifest.json').read_text());final=json.loads((backup/'AfterTest/manifest.json').read_text())
    assert initial==final,'Own level files changed'
    status=json.loads((out/'Device/Status-Final.json').read_text())
    assert status['apkSha256']==apk['sha256']
    report['device']={'tested':True,'installed_sha256_matches':True,'own_level_files_unchanged':len(final),'scope':'Screenshots and Runtime.log; automated door regression was local Unity, not handset gameplay.','handset_checks':['empty full-screen editor','shared asset palette','single block placed and undone during simulation','vertical pan without zoom','upper block placed and undone','native enemy animation'],'parallel_input_confirmed_by_user':True,'test_pause_then_resumption_confirmed':True,'two_finger_gestures_on_handset_tested':False,'user_unsaved_construction_retained':True}
(out/'Verification.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'status':report['status'],'checks':sum(c['passed_assertions'] for c in checks),'device':report['device'],'apk_sha256':apk['sha256'],'published':False},indent=2))
