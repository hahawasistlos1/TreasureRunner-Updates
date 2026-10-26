from pathlib import Path
import urllib.request,json,hashlib,time
root=Path(__file__).resolve().parents[1]
items=[('https://hahawasistlos1.github.io/treasurerunner/invite/','Web/InviteSite/treasurerunner/invite/index.html'),('https://raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/TennoFarm/README.md','Builds/Publish-0.8.0/README.md'),('https://hahawasistlos1.github.io/.well-known/assetlinks.json','Web/InviteSite/.well-known/assetlinks.json')]
report=[]
for url,filename in items:
    request=urllib.request.Request(url+'?v=08-'+str(int(time.time())),headers={'User-Agent':'TreasureRunner-web-verification','Cache-Control':'no-cache'})
    with urllib.request.urlopen(request,timeout=30) as response:body=response.read();status=response.status
    expected=(root/filename).read_bytes()
    assert body.replace(b'\r\n',b'\n')==expected.replace(b'\r\n',b'\n'),(url,'Public web content differs from verified local file')
    report.append({'url':url,'status':status,'sha256':hashlib.sha256(body).hexdigest(),'bytes':len(body),'exact_bytes':body==expected,'text_matches_with_line_endings_normalized':True})
(root/'Logs/WebReleaseVerification08.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Invitation page, release README and Android association verified against exact local files')
