"""Verify published invitation HTML and direct APK redirect, without downloading an APK."""
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler
from urllib.parse import urlsplit
import hashlib, json, sys

root=Path(__file__).resolve().parents[1]
base='https://hahawasistlos1.github.io/'
apk='https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/latest/download/TreasureRunner.apk'
redirects=[]
class Redirects(HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        target=urlsplit(newurl)
        redirects.append({'status':code,'host':target.hostname,'path':target.path})
        return super().redirect_request(req,fp,code,msg,headers,newurl)
opener=build_opener(Redirects())
headers={'User-Agent':'TreasureRunner-invite-verification','Cache-Control':'no-cache'}
with opener.open(Request(apk,method='HEAD',headers=headers),timeout=30) as response:
    assert response.status==200,response.status
    size=int(response.headers.get('Content-Length','0'))
    assert size>1000000,size
    assert any(r['host']=='github.com' and '/releases/download/' in r['path'] and r['path'].endswith('/TreasureRunner.apk') for r in redirects),redirects
    report={'apk_url':apk,'head_status':response.status,'bytes':size,'redirects':redirects,'apk_downloaded':False,'apk_built':False,'version_changed':False,'release_published':False}
if '--before-publish' not in sys.argv:
    files=[]
    for remote,local in [('treasurerunner/invite/','treasurerunner/invite/index.html'),('.well-known/assetlinks.json','.well-known/assetlinks.json'),('','index.html')]:
        with opener.open(Request(base+remote,headers=headers),timeout=30) as response:
            raw=response.read();assert response.status==200
            assert raw==(root/'Web/InviteSite'/local).read_bytes(),f'Public file differs: {local}'
            files.append({'url':base+remote,'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw)})
    report['public_files']=files
(root/'Logs/InviteDownload').mkdir(parents=True,exist_ok=True)
name='DownloadHead.json' if '--before-publish' in sys.argv else 'PublicVerification.json'
(root/'Logs/InviteDownload'/name).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Verified direct latest APK redirect (HEAD only, '+str(size)+' bytes); '+str(len(report.get('public_files',[])))+' exact public files.')
