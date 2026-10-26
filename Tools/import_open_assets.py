"""Import selected data files from official CC0 packs, recording exact provenance."""
import concurrent.futures, hashlib, json, pathlib, re, urllib.request, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/Resources/OpenArt'
DEST.mkdir(parents=True, exist_ok=True)
records = []

def download(url):
    req = urllib.request.Request(url, headers={'User-Agent':'TreasureRunner-CC0-import'})
    with urllib.request.urlopen(req, timeout=60) as response:
        return response.read(50 * 1024 * 1024)

def put(group, name, data, source):
    path = DEST / group / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return dict(file=path.relative_to(ROOT).as_posix(), source=source,
                sha256=hashlib.sha256(data).hexdigest(), bytes=len(data))

jobs = []
packs = {
 'Skeletons': ('KayKit-Character-Pack-Skeletons-1.0', [
  'Characters/fbx/Skeleton_Mage.fbx','Characters/fbx/Skeleton_Minion.fbx',
  'Characters/fbx/Skeleton_Rogue.fbx','Characters/fbx/Skeleton_Warrior.fbx',
  'Textures/skeleton_texture.png','Assets/fbx/Skeleton_Crossbow.fbx',
  'Assets/fbx/Skeleton_Staff.fbx','Assets/fbx/Skeleton_Blade.fbx']),
 'Dungeon': ('KayKit-Dungeon-Remastered-1.0', [
  'Assets/fbx/chest_gold.fbx','Assets/fbx/key.fbx','Assets/fbx/pillar_decorated.fbx',
  'Assets/fbx/barrel_large.fbx','Assets/fbx/crates_stacked.fbx',
  'Assets/fbx/banner_red.fbx','Assets/fbx/banner_blue.fbx',
  'Assets/fbx/torch_mounted.fbx','Assets/fbx/rubble_large.fbx',
  'Assets/texture/dungeon_texture.png'])}
for group,(repo,suffixes) in packs.items():
    tree = json.loads((ROOT/'Downloads'/f'{repo}-tree.json').read_text())
    commit = tree['sha']
    paths = [x['path'] for x in tree['tree']]
    suffixes.append('LICENSE.txt')
    for suffix in suffixes:
        matches = [p for p in paths if p.endswith('/'+suffix) or p==suffix]
        if not matches: raise ValueError(f'Missing official asset: {suffix}')
        p = min(matches, key=len)
        url=f'https://raw.githubusercontent.com/KayKit-Game-Assets/{repo}/{commit}/{p}'
        jobs.append((group,pathlib.PurePosixPath(p).name,url))

def fetch(job):
    group,name,url=job
    return put(group,name,download(url),url)
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    records.extend(pool.map(fetch,jobs))

# Reuse the official, previously downloaded Nature Kit archive; only copy a curated subset.
with zipfile.ZipFile(ROOT/'Downloads/kenney_nature-kit.zip') as archive:
    desired=['tree_oak','tree_pineTallA','tree_palmTall','tree_palm','mushroom_red',
             'rock_largeA','rock_largeB','rock_tallA','plant_bush','plant_bushLarge',
             'flowers_purpleA','flowers_yellow','snow_tree','canoe','tent_smallOpen',
             'statue_obelisk','log_stack','campfire_stones','tree_dead_large']
    names=archive.namelist()
    selected=[n for n in names if n.startswith('Models/FBX format/') and
              pathlib.PurePosixPath(n).stem in desired and n.endswith('.fbx')]
    selected += [n for n in names if n.lower().endswith('license.txt')]
    for n in selected:
        records.append(put('Nature',pathlib.PurePosixPath(n).name,archive.read(n),
                           'https://kenney.nl/assets/nature-kit'))
    # Nature models use the supplied shared colour atlas.
    for n in names:
        if 'Textures/' in n and n.endswith('.png'):
            records.append(put('Nature',pathlib.PurePosixPath(n).name,archive.read(n),
                               'https://kenney.nl/assets/nature-kit'))

url='https://kenney.nl/media/pages/assets/particle-pack/f8fe0f8cb8-1677578741/kenney_particle-pack.zip'
data=download(url)
(ROOT/'Downloads/kenney_particle-pack.zip').write_bytes(data)
import io
with zipfile.ZipFile(io.BytesIO(data)) as archive:
    names=archive.namelist()
    print('Particle files:', names[:20])
    wanted=['star_04.png','spark_05.png','smoke_04.png','circle_05.png','fire_01.png']
    for n in names:
        if n.lower().endswith('license.txt') or ('transparent' in n.lower() and pathlib.PurePosixPath(n).name in wanted):
            records.append(put('Particles',pathlib.PurePosixPath(n).name,archive.read(n),url))

(DEST/'manifest.json').write_text(json.dumps({'license':'CC0-1.0','files':records},indent=2),encoding='utf8')
print(f'Imported {len(records)} verified data files, {sum(r["bytes"] for r in records)} bytes.')
