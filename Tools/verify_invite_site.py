from pathlib import Path
import urllib.request, hashlib, json, re, xml.etree.ElementTree as ET

base='https://hahawasistlos1.github.io/'
folder=Path('Web/InviteSite')
report={'site':base+'treasurerunner/invite/','files':[],'apk_built':False,'version_changed':False}
for remote,local in [('treasurerunner/invite/','treasurerunner/invite/index.html'),('.well-known/assetlinks.json','.well-known/assetlinks.json'),('','index.html')]:
    request=urllib.request.Request(base+remote,headers={'User-Agent':'TreasureRunner-invite-verification','Cache-Control':'no-cache'})
    with urllib.request.urlopen(request,timeout=30) as response:
        raw=response.read();content_type=response.headers.get('Content-Type');code=response.status;url=response.url
    assert code==200 and url==base+remote,(code,url)
    expected=(folder/local).read_bytes()
    assert raw==expected, f'Public {local} differs from tested source'
    report['files'].append({'url':url,'status':code,'content_type':content_type,'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw)})
    if remote.endswith('.json'):
        assert content_type.startswith('application/json'),content_type
        cert=re.search(r'digest: ([a-f0-9]{64})',Path('Logs/SignatureVerification07.txt').read_text()).group(1)
        link=json.loads(raw)[0]['target'];assert link['package_name']=='com.treasurerunner.game'
        assert link['sha256_cert_fingerprints']==[':'.join(cert[i:i+2].upper() for i in range(0,64,2))]

android='{http://schemas.android.com/apk/res/android}'
source=ET.parse('Assets/Plugins/Android/TreasureUpdates.androidlib/AndroidManifest.xml').getroot()
generated=ET.parse('Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/src/main/AndroidManifest.xml').getroot()
activity=source.find('application/activity');existing=generated.find('application/activity')
assert activity.get(android+'name')==existing.get(android+'name')=='com.unity3d.player.UnityPlayerGameActivity'
assert existing.get(android+'launchMode')=='singleTask'
filters=activity.findall('intent-filter');assert len(filters)==2 and filters[0].get(android+'autoVerify')=='true'
assert filters[0].find('data').get(android+'host')=='hahawasistlos1.github.io'
assert filters[0].find('data').get(android+'pathPrefix')=='/treasurerunner/invite/'
for filter in filters:
    assert filter.find('action').get(android+'name')=='android.intent.action.VIEW'
    assert {c.get(android+'name') for c in filter.findall('category')}=={'android.intent.category.BROWSABLE','android.intent.category.DEFAULT'}
assert source.find('application/provider') is not None and source.find('application/receiver') is not None
report['manifest_declarations_checked']=True
report['native_share_compiled']=(Path('Builds/InviteNativeCheck/com/treasurerunner/updates/UpdateBridge.class').is_file())
assert report['native_share_compiled']
for name,digest in [('TreasureRunner.apk','819bda00f4fa49ecb03656debf2b6f47f33e08531bfaa95434f22d52b4532cc4'),('TreasureRunner-Windows-Werkzeuge.zip','b0d129849093475c2fe31d13236ae3da4d57126acaf774c8f63e60b8f14d8e6f'),('TreasureRunner-Source-0.7.0.zip','989426ce86adffdc2a9339dc8358fad6d758f787525eb9873cf10b72588ed8ba')]:
    assert hashlib.sha256((Path('Builds')/name).read_bytes()).hexdigest()==digest, f'Released {name} was changed'
report['published_game_files_unchanged']=True
Path('Logs/InviteSiteDeployment.json').write_text(json.dumps(report,indent=2)+'\n')
print('PUBLIC INVITATION SITE VERIFIED: 3 exact files, signing certificate, native sharing, manifest and unchanged game artifacts')
