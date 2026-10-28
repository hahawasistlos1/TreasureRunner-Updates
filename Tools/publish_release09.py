"""Publish only the owner's explicitly authorized, verified 0.9.0 release.

Uses the existing Git credential helper in memory. Never prints or stores credentials.
"""
from pathlib import Path
import hashlib,json,os,subprocess,sys,time
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
def request(method,url,payload=None,upload_file=None,content_type=None):
    assert url.startswith(base+'/') or url=='https://api.github.com/user' or url.startswith('https://uploads.github.com/repos/hahawasistlos1/TreasureRunner-Updates/')
    headers={'Authorization':'Bearer '+token,'User-Agent':'TreasureRunner-authorized-release','Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','Cache-Control':'no-cache'}
    if method=='GET':url+=('&' if '?' in url else '?')+'verification='+str(time.time_ns())
    if payload is not None:headers['Content-Type']='application/json'
    elif content_type:headers['Content-Type']=content_type
    # Use the Windows TLS stack; Python OpenSSL uploads encountered transport timeouts.
    # Credentials go only through stdin config, never arguments, files or diagnostic output.
    config=['url = '+json.dumps(url),'request = '+json.dumps(method)]
    config += ['header = '+json.dumps(k+': '+v) for k,v in headers.items()]
    if payload is not None:config.append('data-binary = '+json.dumps(json.dumps(payload)))
    if upload_file is not None:
        assert upload_file in {out/p['name'] for p in expected['publicAssets']}
        config.append('data-binary = '+json.dumps('@'+upload_file.as_posix()))
    command=['curl.exe','--silent','--show-error','--connect-timeout','45','--max-time','900' if upload_file else '90','--write-out','\n%{http_code}','--config','-']
    if method=='GET':command+=['--retry','3','--retry-delay','2','--retry-all-errors']
    response=subprocess.run(command,input='\n'.join(config)+'\n',text=True,capture_output=True,encoding='utf-8')
    if response.returncode:raise RuntimeError(f'GitHub transport failed ({response.returncode}); credentials omitted')
    body,code=response.stdout.rsplit('\n',1)
    if code=='404':return None
    if not code.startswith('2'):raise RuntimeError(f'GitHub request failed: HTTP {code}, {method}; credentials omitted')
    return json.loads(body) if body.strip() else None
assert request('GET','https://api.github.com/user')['login'].lower()=='hahawasistlos1'
release=request('GET',base+'/releases/424588611')
assert release is not None,'Known draft temporarily unavailable; do not create duplicates'
if '--inspect' in sys.argv:
    print(json.dumps({'id':release['id'],'draft':release['draft'],'tag':release['tag_name'],'target':release['target_commitish'],'name':release['name'],'assets':[{'id':a['id'],'name':a['name'],'state':a['state'],'bytes':a['size'],'digest':a.get('digest')} for a in release['assets']]},indent=2),flush=True)
    token=None;sys.exit(0)
notes=(root/'Documentation/Release-0.9.0.md').read_text(encoding='utf-8')
notes+='\n[Quellcode direkt als Git-Dateien](https://github.com/hahawasistlos1/TreasureRunner-Updates/tree/'+commit+'/Assets/Scripts)\n'
assert release['draft'],'Release already public; do not mutate immutable published artifacts'
assert release['name']=='TreasureRunner 0.9.0' and (release['tag_name']=='v0.9.0' or release['tag_name'].startswith('untagged-')),'Unexpected draft'
assert release['target_commitish'] in {commit,'TennoFarm'},'Unexpected draft target'
release=request('PATCH',base+'/releases/'+str(release['id']),payload={'tag_name':'v0.9.0','target_commitish':commit,'body':notes,'name':'TreasureRunner 0.9.0'})
for item in expected['publicAssets']:
    assets=request('GET',base+'/releases/'+str(release['id'])+'/assets')
    existing=next((a for a in assets if a['name']==item['name']),None)
    if existing is not None and existing['state']=='starter':
        assert release['draft'],'Never remove public assets'
        assert existing['name']==item['name'] and existing['size'] in {0,item['bytes']} and not existing.get('digest'),'Only replace our incomplete upload reservation'
        request('DELETE',base+'/releases/assets/'+str(existing['id']))
        existing=None
    if existing is None:
        print('Uploading '+item['name']+' ('+str(item['bytes'])+' bytes)',flush=True)
        upload=release['upload_url'].split('{')[0]+'?name='+item['name']
        existing=request('POST',upload,upload_file=out/item['name'],content_type='application/vnd.android.package-archive' if item['name'].endswith('.apk') else 'application/zip')
    if existing['size']!=item['bytes'] or existing.get('digest')!='sha256:'+item['sha256']:
        print(json.dumps({'asset':existing['name'],'state':existing['state'],'bytes':existing['size'],'digest':existing.get('digest')},indent=2),flush=True)
        raise RuntimeError('Uploaded asset mismatch; draft remains unpublished')
assets=request('GET',base+'/releases/'+str(release['id'])+'/assets')
assert {a['name'] for a in assets}=={a['name'] for a in expected['publicAssets']},'Unexpected release asset; draft remains unpublished'
release=request('PATCH',base+'/releases/'+str(release['id']),payload={'draft':False,'prerelease':False,'make_latest':'true'})
token=None
(root/'Logs/Release09/Published.json').write_text(json.dumps({'url':release['html_url'],'id':release['id'],'tag':release['tag_name'],'sourceCommit':commit,'draft':release['draft'],'published_at':release['published_at'],'assets':[{'name':a['name'],'bytes':a['size'],'digest':a['digest'],'url':a['browser_download_url']} for a in release['assets']]},indent=2)+'\n')
print('Published '+release['html_url']+' with APK and Windows editor only; sources in Git.')
