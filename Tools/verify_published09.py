"""Read public GitHub evidence and preserve immutable prior-release checks."""
from pathlib import Path
import hashlib,json,subprocess,sys
root=Path(__file__).resolve().parents[1]
base='https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates'
def read(url):
    result=subprocess.run(['curl.exe','--silent','--show-error','--fail','--connect-timeout','30','--max-time','90','--retry','2','--header','User-Agent: TreasureRunner-public-release-check','--header','Cache-Control: no-cache',url],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,'Public GitHub read failed: '+str(result.returncode)
    return json.loads(result.stdout)
expected=json.loads((root/'Logs/Release09/Packages.json').read_text())
published=json.loads((root/'Logs/Release09/Published.json').read_text())
release=read(base+'/releases/latest')
assert release['tag_name']=='v0.9.0' and not release['draft'] and not release['prerelease']
assets={a['name']:a for a in release['assets']}
assert set(assets)=={a['name'] for a in expected['publicAssets']}
for p in expected['publicAssets']:
    a=assets[p['name']];assert a['size']==p['bytes'] and a['digest']=='sha256:'+p['sha256']
    assert a['browser_download_url']=='https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/'+p['name']
repo=read(base);assert repo['default_branch']=='TennoFarm'
branch=read(base+'/branches/TennoFarm');assert branch['commit']['sha']==published['sourceCommit']
commit=published['sourceCommit']
raw='https://raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/'+commit+'/'
catalog=read(raw+'community/0.9.0/catalog.json')
assert catalog['moderationProtocol']==4 and catalog['gameVersion']=='0.9.0'
identity=read(raw+'Assets/Resources/IdentityConfig.json')
assert identity['enabled'] and identity['firebaseProjectId']=='treasurerunner-2689b'
old08=json.loads((root/'Logs/Release08Packages.json').read_text())
remote08=None
if '--current-only' not in sys.argv:
    release08=read(base+'/releases/tags/v0.8.0');remote08={a['name']:a for a in release08['assets']}
for p in old08['assets']:
    if remote08 is not None:assert remote08[p['name']]['digest']=='sha256:'+p['sha256']
    local=root/'Builds/Publish-0.8.0'/p['name']
    assert hashlib.file_digest(local.open('rb'),'sha256').hexdigest()==p['sha256']
result={'version':'0.9.0','latest':True,'draft':False,'url':release['html_url'],
        'sourceCommit':commit,'defaultBranch':'TennoFarm','publicAssets':[{'name':a['name'],'bytes':a['size'],'digest':a['digest'],'url':a['browser_download_url']} for a in release['assets']],
        'sourceGitVerified':True,'privateAdminNotPublished':True,'previous08LocalUnchanged':True,'previous08RemoteChecked':remote08 is not None}
(root/'Logs/Release09/PublicVerification.json').write_text(json.dumps(result,indent=2)+'\n')
print('Public latest v0.9.0 verified; both asset sizes/digests match; Git sources and versioned catalog readable; previous local 0.8 artifacts unchanged.')
