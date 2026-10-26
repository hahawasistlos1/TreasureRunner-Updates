from pathlib import Path
import urllib.request,json,hashlib
root=Path(__file__).resolve().parents[1]
expected=json.loads((root/'Logs/Release08Packages.json').read_text())
url='https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates/releases/latest'
request=urllib.request.Request(url,headers={'User-Agent':'TreasureRunner-release-verification','Accept':'application/vnd.github+json','Cache-Control':'no-cache'})
with urllib.request.urlopen(request,timeout=30) as response:data=json.load(response)
assert data['tag_name']=='v0.8.0' and not data['draft'] and not data['prerelease'], 'Latest release is not published 0.8.0'
by_name={a['name']:a for a in data['assets']}
assert set(by_name)=={a['name'] for a in expected['assets']}
assets=[]
for local in expected['assets']:
    remote=by_name[local['name']]
    assert remote['size']==local['bytes'] and remote['digest']=='sha256:'+local['sha256'],local['name']
    assert remote['browser_download_url']=='https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.8.0/'+local['name']
    assets.append({'name':local['name'],'bytes':remote['size'],'digest':remote['digest'],'url':remote['browser_download_url']})
old=[('TreasureRunner.apk','819bda00f4fa49ecb03656debf2b6f47f33e08531bfaa95434f22d52b4532cc4'),('TreasureRunner-Windows-Werkzeuge.zip','b0d129849093475c2fe31d13236ae3da4d57126acaf774c8f63e60b8f14d8e6f'),('TreasureRunner-Source-0.7.0.zip','989426ce86adffdc2a9339dc8358fad6d758f787525eb9873cf10b72588ed8ba')]
assert all(hashlib.sha256((root/'Builds'/name).read_bytes()).hexdigest()==digest for name,digest in old)
result={'url':data['html_url'],'tag':data['tag_name'],'published_at':data['published_at'],'latest':True,'draft':False,'assets':assets,'original07_artifacts_unchanged':True}
(root/'Logs/GitHubReleaseVerification08.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
