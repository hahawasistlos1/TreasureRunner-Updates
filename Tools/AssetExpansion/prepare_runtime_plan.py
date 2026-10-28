"""Build deterministic runtime import instructions from verified data manifests."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[2]
report=json.loads((ROOT/'Logs/AssetExpansion/Imports.json').read_text())
plan={'packs':[],'assets':[],'images':[],'audio':[],'characters':[]}
def identifier(pack,path):
    stem=re.sub('[^a-z0-9]+','-',Path(path).stem.lower()).strip('-')[:44]
    return pack+'.'+stem+'.'+hashlib.sha256(path.encode()).hexdigest()[:8]
def classification(name,pack):
    n=name.lower();role=4;kind=1;category='Dekoration';weapon=''
    if any(w in n for w in ['tree','plant','bush','grass','flower','rock','crystal','palm','cactus','mushroom','foliage','log']):category='Natur'
    if any(w in n for w in ['floor','ground','block','terrain','platform','stairs','brick','tile']):role=1;kind=0;category='Boden'
    if 'moving' in n or 'elevator' in n:role=1;kind=7;category='Aufzüge'
    if 'spike' in n:role=5;kind=10;category='Fallen'
    if 'saw' in n:role=6;kind=10;category='Fallen'
    if 'cannon' in n:role=10;kind=11;category='Fallen'
    if 'flamethrower' in n:role=11;kind=10;category='Fallen'
    if 'spring' in n:role=12;kind=13;category='Fallen'
    if 'coin' in n or 'gem' in n:role=2;kind=1;category='Beute'
    if 'heart' in n:role=8;kind=1;category='Beute'
    for word,value in [('crossbow','crossbow'),('bow','bow'),('sword','sword'),('axe','axe'),('hammer','hammer'),('spear','spear'),('staff','staff'),('wand','staff')]:
        if word in n and 'rack' not in n:role=7;kind=1;category='Beute';weapon=value;break
    if any(w in n for w in ['enemy','character','monster']) and not any(w in n for w in ['weapon','head','arm','hand','leg']):role=3;kind=2;category='Gegner'
    return role,kind,category,weapon

for pack in report['packs']:
    key=pack['id'];files=pack['files'];root='Assets/Art/FreeLibrary/'+key+'/'
    license_paths=pack['licenses']
    plan['packs'].append({'id':key,'title':pack['title'],'author':pack['author'],'url':pack['page'],'license':pack['license'],'licenses':license_paths,'licenseResource':'AssetLibrary/Licenses/'+key})
    for f in files:
        source=f['path'];path=Path(source);suffix=path.suffix.lower();relative=source[len(root):];n=path.stem
        asset_id=identifier(key,relative)
        if suffix=='.fbx':
            if key in {'kay-adventurers-v2','kay-dungeon-v11'}:
                if '/Animations/' in source:continue
                if '/Assets/fbx/' in source and any(other['path'].replace('/fbx(unity)/','/fbx/')==source for other in files if '/fbx(unity)/' in other['path']):continue
            role,kind,category,weapon=classification(n,key)
            character=n in {'Barbarian','Knight','Mage','Ranger','Rogue','Rogue_Hooded','Skeleton_Mage','Skeleton_Minion','Skeleton_Rogue','Skeleton_Warrior'}
            if key in {'kay-adventurers-v2','kay-dungeon-v11'}:
                if n.startswith('bed_'):role=4;kind=1;category='Dekoration'
                if n.startswith(('arrow_','quiver')):role=4;kind=1;category='Dekoration';weapon=''
                if n.startswith('shield_'):role=9;kind=1;category='Beute';weapon=''
            if pack['kind']=='characters' and ('/Converted/' in source or '/Characters/' in source) and character:role=3;kind=3 if 'mage' in n.lower() else 2;category='Gegner'
            entry={'id':'library.'+asset_id,'source':source,'title':n.replace('_',' ').replace('-',' ')+' · '+pack['title'],'pack':key,'category':category,'resource':'AssetLibrary/Models/'+asset_id,'role':role,'kind':kind,'weapon':weapon,'height':2.8 if category=='Natur' else 1.2,'animated':pack['kind']=='characters' and (key!='kay-adventurers-v2' or character)}
            if key=='kay-adventurers-v2' and character:entry['animationSources']=[f['path'] for f in files if '/Animations/fbx/' in f['path'] and f['path'].endswith('.fbx')]
            plan['assets'].append(entry)
            if key in {'kay-adventurers','kay-adventurers-v2'} and character:
                ability='skeleton-mage' if 'mage' in n.lower() else 'skeleton-scout' if 'rogue' in n.lower() or 'ranger' in n.lower() else 'skeleton-knight'
                plan['characters'].append({'id':'hero.'+n.lower().replace('_','-'),'title':n.replace('_',' '),'resource':'AssetLibrary/Models/'+asset_id,'ability':ability})
        elif suffix in {'.ogg','.wav','.mp3'} and 'preview' not in n.lower():
            if suffix!='.ogg' and any(Path(other['path']).stem==n and Path(other['path']).suffix=='.ogg' for other in files):continue
            plan['audio'].append({'source':source,'resource':'AssetLibrary/Audio/'+asset_id,'pack':key,'title':n,'use':'music' if pack['kind']=='music' else 'effect'})
        elif suffix=='.png':
            if pack['kind'] not in {'ui','sprites','effects'}:continue
            if any(w in relative.lower() for w in ['preview','sample','spritesheet','tilemap','_tilesheet']):continue
            if any(w in relative.lower() for w in ['retina','vector','/double/','/large (2']):continue
            if pack['kind']=='sprites' and not relative.startswith('Sprites/'):continue
            if pack['kind']=='effects' and ('particle' in key) and 'transparent' not in relative.lower():continue
            plan['images'].append({'source':source,'resource':'AssetLibrary/Images/'+asset_id,'id':asset_id,'pack':key,'title':n.replace('_',' '),'use':pack['kind']})
            if pack['kind']=='sprites':
                role,kind,category,weapon=classification(n,key)
                if '/Enemies/' in relative:
                    if not (n.endswith('_rest') or n.endswith('_idle')):continue
                    role=3;kind=2;category='Gegner'
                if '/Characters/' in relative:continue
                plan['assets'].append({'id':'library.'+asset_id,'source':source,'title':n.replace('_',' ')+' · 2D','pack':key,'category':category,'resource':'AssetLibrary/Models/'+asset_id,'role':role,'kind':kind,'weapon':weapon,'height':1.2,'animated':False})
for entry in plan['assets']:
    if entry['source'].endswith('.png') and entry['role']==3:
        stem=Path(entry['source']).stem;family=re.sub('_(rest|idle)$','',stem)
        candidates=[i for i in plan['images'] if i['pack']==entry['pack'] and i['source'].rsplit('/',1)[0]==entry['source'].rsplit('/',1)[0] and re.fullmatch(re.escape(family)+r'_(?:walk_|swim_)?[ab]',Path(i['source']).stem)]
        entry['frames']=[i['resource'] for i in candidates]
plan['characters']=list({c['id']:c for c in plan['characters']}.values())
(ROOT/'Downloads/AssetExpansion/runtime-plan.json').write_text(json.dumps(plan,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print(json.dumps({k:len(v) for k,v in plan.items()}))
