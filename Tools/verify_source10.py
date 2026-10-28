"""Audit staged public files without displaying matching content or credentials."""
from pathlib import Path
import json,re,subprocess
root=Path(__file__).resolve().parents[1];checkout=root/'Builds/Publish-0.10.0/GitSource'
git='C:/Users/Jir3n/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/cmd/git.exe'
names=subprocess.check_output([git,'-C',str(checkout),'diff','--cached','--name-only','--diff-filter=ACMR','-z']).decode().split('\0')
names=[n for n in names if n]
for name in names:
    p=checkout/name
    assert p.is_file() and p.resolve().is_relative_to(checkout.resolve())
    assert not any(part in {'node_modules','.gradle','Backups','Builds','Logs','UserSettings'} for part in Path(name).parts)
    assert p.suffix not in {'.apk','.exe','.dll','.zip','.jks','.keystore','.fbx','.glb','.gltf','.blend'}
    assert p.name not in {'device08.py','prepare_device08_level.py','google-services.json'}
    if p.suffix not in {'.png','.ico','.ttf'}:
        s=p.read_text(encoding='utf-8-sig')
        assert not re.search(r'gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',s),name
        assert not re.search(r'"(?:private_key|client_secret|refresh_token|id_token)"\s*:\s*"[^"\s]{12,}"',s),name
catalog=json.loads((checkout/'community/0.10.0/catalog.json').read_text(encoding='utf-8-sig'))
assert catalog['moderationProtocol']==4 and catalog['gameVersion']=='0.10.0' and catalog['levels']==[]
assert (checkout/'Assets/Scripts/Core/ReleaseInfo.cs').read_text(encoding='utf-8-sig')==(root/'Assets/Scripts/Core/ReleaseInfo.cs').read_text(encoding='utf-8-sig')
report={'filesChecked':len(names),'embeddedCredentialMatches':0,'thirdPartyOriginalModels':False,'sourceZip':False,'catalogVersion':'0.10.0'}
(root/'Logs/Release10/SourceAudit.json').write_text(json.dumps(report,indent=2)+'\n')
print('Source audit passed for '+str(len(names))+' staged files; no private credentials, original models or binary release packages.')
