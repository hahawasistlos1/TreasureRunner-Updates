import concurrent.futures,hashlib,json,pathlib,urllib.request
root=pathlib.Path(__file__).resolve().parents[1]
tree=json.loads((root/'Downloads/KayKit-Dungeon-Remastered-1.0-tree.json').read_text())
names=['coin','coin_stack_small','coin_stack_medium','coin_stack_large','floor_dirt_small_A','floor_tile_small','floor_tile_small_broken_A','floor_tile_small_decorated','floor_wood_small','floor_wood_small_dark','floor_foundation_allsides','floor_tile_big_spikes','floor_tile_grate','wall','wall_cracked','wall_arched','stairs','barrier','sword_shield']
manifest=root/'Assets/Resources/OpenArt/manifest.json';data=json.loads(manifest.read_text())
def fetch(name):
 path=next(x['path'] for x in tree['tree'] if x['path'].endswith('/fbx/'+name+'.fbx'))
 url=f'https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/{tree["sha"]}/{path}'
 req=urllib.request.Request(url,headers={'User-Agent':'TreasureRunner-asset-builder'})
 with urllib.request.urlopen(req,timeout=40) as response: content=response.read(10000000)
 out=root/'Assets/Resources/OpenArt/Dungeon'/f'{name}.fbx';out.write_bytes(content)
 return dict(file=out.relative_to(root).as_posix(),source=url,sha256=hashlib.sha256(content).hexdigest(),bytes=len(content))
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool: records=list(pool.map(fetch,names))
changed={r['file'] for r in records};data['files']=[r for r in data['files'] if r['file'] not in changed]+records
manifest.write_text(json.dumps(data,indent=2),encoding='utf8');print('Builder assets added:',len(records))
