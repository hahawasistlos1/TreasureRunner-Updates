"""Publish the explicitly authorized verified 0.10.0 APK/editor release only."""
from pathlib import Path
import hashlib,json,subprocess
from github_release10_api import Client,ROOT,GIT,ENV,BASE

out=ROOT/'Builds/Publish-0.10.0';logs=ROOT/'Logs/Release10';checkout=out/'GitSource'
def sha(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def record(name,value):
    (logs/name).write_text(json.dumps(value,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
expected=json.loads((logs/'Packages.json').read_text(encoding='utf-8'))
assert expected['version']=='0.10.0' and expected['androidCode']==11 and expected['sourceDelivery']=='git'
assert json.loads((logs/'BuildProgress.json').read_text(encoding='utf-8-sig'))['status']=='passed'
assert json.loads((logs/'Apk/Verification.json').read_text(encoding='utf-8'))['same_signing_certificate_as_09']
assert json.loads((logs/'SourceAudit.json').read_text(encoding='utf-8'))['catalogVersion']=='0.10.0'
identity=json.loads((ROOT/'Assets/Resources/IdentityConfig.json').read_text(encoding='utf-8'))
assert identity['enabled'] and identity['firebaseProjectId']=='treasurerunner-2689b'
assert {p['name'] for p in expected['publicAssets']}=={'TreasureRunner.apk','TreasureRunner-Windows-Editor.zip'}
for item in expected['publicAssets']:assert (out/item['name']).stat().st_size==item['bytes'] and sha(out/item['name'])==item['sha256']
commit=subprocess.check_output([str(GIT),'-C',str(checkout),'rev-parse','HEAD'],text=True,env=ENV).strip()
remote=subprocess.check_output([str(GIT),'-C',str(checkout),'ls-remote','origin','refs/heads/TennoFarm'],text=True,env=ENV).split()[0]
assert commit==remote,'Verified source commit must be pushed before release'
client=Client()
old=client.request('GET',BASE+'/releases/tags/v0.9.0')
assert old is not None and not old['draft']
old_assets={a['name']:(a['id'],a['size'],a['digest']) for a in old['assets']}
record('Prior09BeforePublish.json',{'id':old['id'],'assets':old_assets})
release=client.request('GET',BASE+'/releases/tags/v0.10.0')
if release is None and (logs/'Draft.json').is_file():
    release=client.request('GET',BASE+'/releases/'+str(json.loads((logs/'Draft.json').read_text(encoding='utf-8-sig'))['id']))
notes=(ROOT/'Documentation/Release-0.10.0.md').read_text(encoding='utf-8')+'\n[Eigene Quellen als Git-Dateien](https://github.com/hahawasistlos1/TreasureRunner-Updates/tree/'+commit+'/Assets/Scripts)\n'
if release is None:
    release=client.request('POST',BASE+'/releases',{'tag_name':'v0.10.0','target_commitish':commit,'name':'TreasureRunner 0.10.0','body':notes,'draft':True,'prerelease':False})
assert release['draft'] and release['tag_name']=='v0.10.0' and release['name']=='TreasureRunner 0.10.0','Never mutate an already public release or unrelated draft'
record('Draft.json',{'id':release['id'],'tag':release['tag_name'],'sourceCommit':commit})
release=client.request('PATCH',BASE+'/releases/'+str(release['id']),{'target_commitish':commit,'body':notes})
for item in expected['publicAssets']:
    record('UploadProgress.json',{'status':'uploading','asset':item['name'],'bytes':item['bytes'],'releaseId':release['id']})
    assets=client.request('GET',BASE+'/releases/'+str(release['id'])+'/assets')
    asset=next((a for a in assets if a['name']==item['name']),None)
    if asset is not None and asset['state']=='starter':
        assert asset['size'] in {0,item['bytes']} and not asset.get('digest')
        client.request('DELETE',BASE+'/releases/assets/'+str(asset['id']));asset=None
    if asset is None:
        print('Uploading '+item['name']+' ('+str(item['bytes'])+' bytes)',flush=True)
        asset=client.request('POST',release['upload_url'].split('{')[0]+'?name='+item['name'],upload_file=out/item['name'])
    assert asset['state']=='uploaded' and asset['size']==item['bytes'] and asset.get('digest')=='sha256:'+item['sha256'],'Asset mismatch; leave draft unpublished'
    print('Verified GitHub digest: '+item['name'],flush=True)
assets=client.request('GET',BASE+'/releases/'+str(release['id'])+'/assets')
assert {a['name'] for a in assets}=={p['name'] for p in expected['publicAssets']}
release=client.request('PATCH',BASE+'/releases/'+str(release['id']),{'draft':False,'prerelease':False,'make_latest':'true'})
old_after=client.request('GET',BASE+'/releases/tags/v0.9.0')
assert {a['name']:(a['id'],a['size'],a['digest']) for a in old_after['assets']}==old_assets,'Prior release changed unexpectedly'
record('Published.json',{'id':release['id'],'url':release['html_url'],'tag':release['tag_name'],'draft':release['draft'],'sourceCommit':commit,'published_at':release['published_at'],'assets':[{'name':a['name'],'bytes':a['size'],'digest':a['digest'],'url':a['browser_download_url']} for a in release['assets']],'prior09Unchanged':True})
record('UploadProgress.json',{'status':'published','url':release['html_url']})
client.close();print('Published '+release['html_url'],flush=True)
