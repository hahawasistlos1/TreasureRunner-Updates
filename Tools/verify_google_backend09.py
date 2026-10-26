"""Read-only anonymous access check; no credentials and no production writes."""
from pathlib import Path
import json,urllib.request,urllib.error
root=Path(__file__).resolve().parents[1]
url='https://firestore.googleapis.com/v1/projects/treasurerunner-2689b/databases/(default)/documents/players/treasurerunner-verification-read'
try:
    with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'TreasureRunner-access-verification'}),timeout=30) as response:
        code=response.status;status='UNEXPECTED_SUCCESS'
except urllib.error.HTTPError as error:
    code=error.code;status=json.load(error).get('error',{}).get('status','')
assert code==403 and status=='PERMISSION_DENIED',(code,status)
report={'project':'treasurerunner-2689b','anonymousReadHttp':code,'status':status,'authenticated':False,'productionWrites':False}
(root/'Logs/Release09/FirestoreAnonymous.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
