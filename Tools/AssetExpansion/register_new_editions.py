"""Register supplied newer FREE editions alongside the original stable asset IDs."""
from pathlib import Path
import hashlib,json,shutil
from import_archives import import_pack
ROOT=Path(__file__).resolve().parents[2];CACHE=ROOT/'Downloads/AssetExpansion';REPORT=ROOT/'Logs/AssetExpansion'
editions=[('kay-adventurers-v2','Adventurers 2.0 FREE','KayKit_Adventurers_2.0_FREE.zip','characters','https://kaylousberg.itch.io/kaykit-adventurers'),
          ('kay-dungeon-v11','Dungeon Pack 1.1 FREE','KayKit_Dungeon_Pack_1.1_FREE.zip','models','https://kaylousberg.itch.io/kaykit-dungeon-pack')]
plan_path=CACHE/'download-plan.json';plan=json.loads(plan_path.read_text())
for key,title,filename,kind,page in editions:
    source=ROOT/'pack'/filename;target=CACHE/(key+'.zip')
    digest=hashlib.file_digest(source.open('rb'),'sha256').hexdigest()
    if target.exists():assert hashlib.file_digest(target.open('rb'),'sha256').hexdigest()==digest
    else:shutil.copyfile(source,target)
    pack={'id':key,'title':title,'author':'Kay Lousberg','page':page,'license':'CC0-1.0','kind':kind,'localFilename':filename,'links':[]}
    if not (REPORT/(key+'.json')).exists():import_pack(pack)
    if not any(p['id']==key for p in plan['packs']):plan['packs'].append(pack)
plan_path.write_text(json.dumps(plan,indent=2)+'\n')
packs=[]
for path in REPORT.glob('*.json'):
    if path.name=='Imports.json':continue
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    if isinstance(data,dict) and data.get('status')=='imported-data':packs.append(data)
(REPORT/'Imports.json').write_text(json.dumps({'packs':sorted(packs,key=lambda p:p['id'])},indent=2)+'\n')
print('Registered two newer FREE editions; older imported models and IDs preserved.')
