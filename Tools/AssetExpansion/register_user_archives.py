"""Match the user's pack folder against official filenames and existing checksums."""
from pathlib import Path
import hashlib,json,shutil,zipfile
ROOT=Path(__file__).resolve().parents[2];CACHE=ROOT/'Downloads/AssetExpansion'
plan_path=CACHE/'download-plan.json';data=json.loads(plan_path.read_text())
extra=[('kenney-nature','Nature Kit','https://kenney.nl/assets/nature-kit','kenney_nature-kit.zip','models'),
       ('kenney-particles','Particle Pack','https://kenney.nl/assets/particle-pack','kenney_particle-pack.zip','effects'),
       ('kenney-platformer','Platformer Kit','https://kenney.nl/assets/platformer-kit','kenney_platformer-kit.zip','models')]
for key,title,page,filename,kind in extra:
    if not any(p['id']==key for p in data['packs']):
        data['packs'].append({'id':key,'author':'Kenney','title':title,'page':page,'license':'CC0-1.0','kind':kind,'links':[{'url':page,'title':'User supplied official archive'}],'localFilename':filename})
records=[]
for pack in data['packs']:
    names=[pack['localFilename']] if pack.get('localFilename') else [Path(x['url']).name for x in pack.get('links',[]) if x['url'].endswith('.zip')]
    for name in names:
        source=ROOT/'pack'/name
        if not source.exists():continue
        with zipfile.ZipFile(source) as z:assert z.testzip() is None
        digest=hashlib.file_digest(source.open('rb'),'sha256').hexdigest();target=CACHE/(pack['id']+'.zip')
        if target.exists():assert hashlib.file_digest(target.open('rb'),'sha256').hexdigest()==digest,'Archive differs: '+name
        else:shutil.copyfile(source,target)
        records.append({'id':pack['id'],'userFile':str(source.relative_to(ROOT)),'sha256':digest,'bytes':source.stat().st_size})
plan_path.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
(ROOT/'Logs/AssetExpansion/UserArchives.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
print('Verified '+str(len(records))+' user archives; source ZIPs preserved.')
