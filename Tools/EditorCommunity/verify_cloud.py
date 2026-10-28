"""Read-only production verification. Never writes accounts, comments or reviews."""
import json,urllib.request,urllib.error
from pathlib import Path
root=Path(__file__).resolve().parents[2]
base='https://firestore.googleapis.com/v1/projects/treasurerunner-2689b/databases/(default)/documents'
report=[]
def call(label,url,payload,expected):
    data=None if payload is None else json.dumps(payload).encode()
    req=urllib.request.Request(url,data=data,headers={'Content-Type':'application/json','User-Agent':'TreasureRunner-read-only-verification'})
    try:
        with urllib.request.urlopen(req,timeout=25) as response: status=response.status;body=json.load(response)
    except urllib.error.HTTPError as error: status=error.code;body=json.load(error)
    assert status==expected,(label,status,body)
    report.append({'check':label,'http':status})
query={'from':[{'collectionId':'entries'}],'orderBy':[{'field':{'fieldPath':'__name__'},'direction':'ASCENDING'}],'limit':20}
url=base+'/levelReviews/'+'0'*64+':runQuery'
call('Public bounded review page',url,{'structuredQuery':query},200)
query['limit']=21;call('Oversized review page denied',url,{'structuredQuery':query},403)
query.pop('limit');call('Unbounded review listing denied',url,{'structuredQuery':query},403)
call('Anonymous player profile read denied',base+'/players/treasurerunner-verification-read',None,403)
call('Anonymous review-writer clock read denied',base+'/reviewWriters/treasurerunner-verification-read',None,403)
(root/'Logs/EditorCommunity/CloudAccess.json').write_text(json.dumps({'project':'treasurerunner-2689b','readOnly':True,'checks':report},indent=2)+'\n')
print(json.dumps(report,indent=2))
