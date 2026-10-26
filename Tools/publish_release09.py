"""Publish only the owner's explicitly authorized, verified 0.9.0 release.

Uses the existing Git credential helper in memory. Never prints or stores credentials.
"""
from pathlib import Path
import hashlib,json,os,subprocess,urllib.request,urllib.error
root=Path(__file__).resolve().parents[1];out=root/'Builds/Publish-0.9.0';checkout=out/'GitSource'
git=Path('C:/Users/Jir3n/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/cmd/git.exe')
env=dict(os.environ,GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='never')
expected=json.loads((root/'Logs/Release09/Packages.json').read_text())
assert expected['version']=='0.9.0' and expected['sourceDelivery']=='git'
identity=json.loads((root/'Assets/Resources/IdentityConfig.json').read_text())
assert identity['enabled'] and identity['firebaseProjectId']=='treasurerunner-2689b' and identity['googleWebClientId'].endswith('.apps.googleusercontent.com'),'Owner requires configured Google sign-in before publishing'
setup=json.loads((root/'Logs/Release09/GoogleSetupVerification.json').read_text())
assert all(setup[k] for k in ['rulesDeployed','realAndroidLoginTested','nicknameRestoredAfterRestart','freeSparkVerified','oauthExternalProduction']),'Complete and verify owner Google/Firebase setup first'
assert setup['firestoreLocation']=='eur3' and not setup['billingEnabled']
assert {p['name'] for p in expected['publicAssets']}=={'TreasureRunner.apk','TreasureRunner-Windows-Editor.zip'}
for item in expected['publicAssets']:
    p=out/item['name'];assert p.stat().st_size==item['bytes'] and hashlib.file_digest(p.open('rb'),'sha256').hexdigest()==item['sha256']
assert json.loads((root/'Logs/Release09/AndroidVerification.json').read_text())['signature_unchanged']
commit=subprocess.check_output([str(git),'-C',str(checkout),'rev-parse','HEAD'],text=True,env=env).strip()
remote=subprocess.check_output([str(git),'-C',str(checkout),'ls-remote','origin','refs/heads/TennoFarm'],text=True,env=env).split()[0]
assert commit==remote,'Push verified source commit first'
credential=subprocess.run([str(git),'credential','fill'],input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True,check=True,env=env)
fields=dict(line.split('=',1) for line in credential.stdout.splitlines() if '=' in line)
token=fields['password'];credential=None;fields=None
base='https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates'
def request(method,url,payload=None,raw=None,content_type=None):
    assert url.startswith(base+'/') or url=='https://api.github.com/user' or url.startswith('https://uploads.github.com/repos/hahawasistlos1/TreasureRunner-Updates/')
    headers={'Authorization':'Bearer '+token,'User-Agent':'TreasureRunner-authorized-release','Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'}
    data=raw
    if payload is not None:data=json.dumps(payload).encode();headers['Content-Type']='application/json'
    elif content_type:headers['Content-Type']=content_type
    req=urllib.request.Request(url,data=data,method=method,headers=headers)
    try:
        with urllib.request.urlopen(req,timeout=180) as response:return json.load(response)
    except urllib.error.HTTPError as error:
        if error.code==404:return None
        raise RuntimeError(f'GitHub request failed: HTTP {error.code}, {method}; credentials omitted') from None
assert request('GET','https://api.github.com/user')['login'].lower()=='hahawasistlos1'
release=request('GET',base+'/releases/tags/v0.9.0')
notes=(root/'Documentation/Release-0.9.0.md').read_text(encoding='utf-8')
notes+='\n[Quellcode direkt als Git-Dateien](https://github.com/hahawasistlos1/TreasureRunner-Updates/tree/'+commit+'/Assets/Scripts)\n'
if release is None:
    release=request('POST',base+'/releases',payload={'tag_name':'v0.9.0','target_commitish':commit,'name':'TreasureRunner 0.9.0','body':notes,'draft':True,'prerelease':False})
else:
    assert release['draft'],'Release already public; do not mutate immutable published artifacts'
    assert release['target_commitish'] in {commit,'TennoFarm'},'Unexpected draft target'
    release=request('PATCH',base+'/releases/'+str(release['id']),payload={'body':notes,'name':'TreasureRunner 0.9.0'})
for item in expected['publicAssets']:
    assets=request('GET',base+'/releases/'+str(release['id'])+'/assets')
    existing=next((a for a in assets if a['name']==item['name']),None)
    if existing is None:
        upload=release['upload_url'].split('{')[0]+'?name='+item['name']
        existing=request('POST',upload,raw=(out/item['name']).read_bytes(),content_type='application/vnd.android.package-archive' if item['name'].endswith('.apk') else 'application/zip')
    assert existing['size']==item['bytes'] and existing['digest']=='sha256:'+item['sha256'],'Uploaded asset mismatch; draft remains unpublished'
assets=request('GET',base+'/releases/'+str(release['id'])+'/assets')
assert {a['name'] for a in assets}=={a['name'] for a in expected['publicAssets']},'Unexpected release asset; draft remains unpublished'
release=request('PATCH',base+'/releases/'+str(release['id']),payload={'draft':False,'prerelease':False,'make_latest':'true'})
token=None
(root/'Logs/Release09/Published.json').write_text(json.dumps({'url':release['html_url'],'id':release['id'],'tag':release['tag_name'],'sourceCommit':commit,'draft':release['draft'],'published_at':release['published_at'],'assets':[{'name':a['name'],'bytes':a['size'],'digest':a['digest'],'url':a['browser_download_url']} for a in release['assets']]},indent=2)+'\n')
print('Published '+release['html_url']+' with APK and Windows editor only; sources in Git.')
