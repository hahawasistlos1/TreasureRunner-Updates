from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2];cache=ROOT/'Downloads/AssetExpansion'
repos=json.loads((cache/'KayKit-repositories.json').read_text())
available={r['name']:r for r in repos}
path=cache/'download-plan.json';plan=json.loads(path.read_text())
for key,repo,title,kind in [
    ('kay-adventurers','KayKit-Character-Pack-Adventures-1.0','Adventurers 1.0 FREE','characters'),
    ('kay-skeletons-full','KayKit-Character-Pack-Skeletons-1.0','Skeletons 1.0 FREE','characters'),
    ('kay-dungeon-full','KayKit-Dungeon-Remastered-1.0','Dungeon Remastered 1.0 FREE','models')]:
    item=available[repo];assert item['owner']['login']=='KayKit-Game-Assets' and item['default_branch']=='main'
    pack={'id':key,'author':'Kay Lousberg','title':title,'page':item['html_url'],'license':'CC0-1.0','kind':kind,
          'links':[{'url':'https://codeload.github.com/KayKit-Game-Assets/'+repo+'/zip/refs/heads/main','title':'Official free source archive'}]}
    old=next((p for p in plan['packs'] if p['id']==key),None)
    if old is None:plan['packs'].append(pack)
    else:old.clear();old.update(pack)
path.write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
print('Three free first-party KayKit source archives registered; no paid tiers.')
