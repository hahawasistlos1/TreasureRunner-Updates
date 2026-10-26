from pathlib import Path
root=Path(__file__).resolve().parents[1]
old='Application.version=="0.7.0" && PlayerSettings.Android.bundleVersionCode==8'
new='Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode'
changed=0
for p in (root/'Assets/Scripts/Diagnostics').glob('*.cs'):
    source=p.read_text(encoding='utf-8-sig')
    text=source.replace(old,new).replace('Authorized release version 0.7.0 / code 8','Authorized current release identity')
    text=text.replace('Check(!LevelModeration.CanPublish(LevelEditorScreen.Working) && LevelModeration.PathFor(LevelEditorScreen.Working)==LevelModeration.NextPath,"Unreleased mechanics cannot enter the public 0.7 catalogs")','Check(LevelModeration.CanPublish(LevelEditorScreen.Working)==GameUpdateClient.IsNewer(Application.version,"0.7.0") && LevelModeration.PathFor(LevelEditorScreen.Working)==LevelModeration.NextPath,"New mechanics use their version-compatible catalog")')
    if text!=source:p.write_text(text,encoding='utf-8');changed+=1
settings=root/'ProjectSettings/ProjectSettings.asset'
source=settings.read_text(encoding='utf-8')
assert 'bundleVersion: 0.7.0' in source or 'bundleVersion: 0.8.0' in source
settings.write_text(source.replace('bundleVersion: 0.7.0','bundleVersion: 0.8.0').replace('AndroidBundleVersionCode: 8\n','AndroidBundleVersionCode: 9\n'),encoding='utf-8')
print('Configured authorized 0.8.0 / Android 9; updated',changed,'current-version checks')
