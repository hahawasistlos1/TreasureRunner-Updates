"""Download creator archives and import data only, preserving licenses and hashes."""
from pathlib import Path,PurePosixPath
import concurrent.futures,hashlib,json,re,subprocess,sys,zipfile
ROOT=Path(__file__).resolve().parents[2];CACHE=ROOT/'Downloads/AssetExpansion'
DEST=ROOT/'Assets/Art/FreeLibrary';DEST.mkdir(parents=True,exist_ok=True)
REPORT=ROOT/'Logs/AssetExpansion';REPORT.mkdir(parents=True,exist_ok=True)
DATA={'.fbx','.obj','.mtl','.gltf','.glb','.bin','.png','.jpg','.jpeg','.tga','.ogg','.wav','.mp3','.txt','.md'}

def download(url,path,limit=536870912):
    if path.exists() and path.stat().st_size>0:return
    temp=path.with_suffix(path.suffix+'.partial')
    args=['curl.exe','--fail','--silent','--show-error','--location','--connect-timeout','30','--max-time','900','--retry','3','--retry-all-errors','--max-filesize',str(limit),'--output',str(temp),url]
    result=subprocess.run(args,capture_output=True)
    if result.returncode:raise RuntimeError('Download failed: '+path.name+' (transport '+str(result.returncode)+')')
    temp.replace(path)

def import_pack(pack):
    key=pack['id'];assert re.fullmatch('[a-z0-9-]+',key)
    urls=[x['url'] for x in pack.get('links',[]) if x['url'].lower().endswith('.zip') or '/zip/refs/heads/' in x['url']]
    archive=CACHE/(key+'.zip')
    if not archive.exists():
        if len(urls)!=1:return dict(pack,status='pending-download',files=[])
        print('Download '+key,flush=True);download(urls[0],archive)
    files=[];licenses=[];group=DEST/key
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        for entry in z.infolist():
            relative=PurePosixPath(entry.filename)
            if entry.is_dir() or relative.suffix.lower() not in DATA:continue
            if any(x in {'__MACOSX','.git'} for x in relative.parts):continue
            assert not relative.is_absolute() and '..' not in relative.parts and all(':' not in x and '\\' not in x for x in relative.parts)
            assert entry.file_size<134217728,'Oversized data file'
            target=group.joinpath(*relative.parts);assert target.resolve().is_relative_to(group.resolve())
            content=z.read(entry);digest=hashlib.sha256(content).hexdigest()
            target.parent.mkdir(parents=True,exist_ok=True)
            if target.exists():assert hashlib.file_digest(target.open('rb'),'sha256').hexdigest()==digest,'Existing imported file differs'
            else:target.write_bytes(content)
            name=target.relative_to(ROOT).as_posix()
            files.append({'path':name,'sha256':digest,'bytes':len(content)})
            if re.search('licen[sc]e|copying',target.name,re.I):licenses.append(name)
    assert files and licenses,'Missing data/license in '+key
    result={k:v for k,v in pack.items() if k not in {'links','error'}}
    result.update(status='imported-data',archiveSha256=hashlib.file_digest(archive.open('rb'),'sha256').hexdigest(),archiveBytes=archive.stat().st_size,download=urls[0] if urls else pack['page'],licenses=licenses,files=files)
    print('Imported '+key+': '+str(len(files))+' data files',flush=True)
    (REPORT/(key+'.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return result

if __name__=='__main__':
    plan=json.loads((CACHE/'download-plan.json').read_text())['packs']
    if '--kenney' in sys.argv:plan=[p for p in plan if p['id'].startswith('kenney-')]
    only=next((a.split('=',1)[1] for a in sys.argv if a.startswith('--only=')),None)
    if only:plan=[p for p in plan if p['id'].startswith(only)]
    results=[]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        futures={pool.submit(import_pack,p):p for p in plan}
        for future in concurrent.futures.as_completed(futures):
            p=futures[future]
            try:results.append(future.result())
            except Exception as error:results.append({'id':p['id'],'status':'error','error':str(error)});print(p['id']+': '+str(error),flush=True)
    existing=[]
    for path in REPORT.glob('*.json'):
        if path.name=='Imports.json':continue
        data=json.loads(path.read_text());
        if isinstance(data,dict) and data.get('status')=='imported-data':existing.append(data)
    (REPORT/'Imports.json').write_text(json.dumps({'packs':sorted(existing,key=lambda p:p['id'])},indent=2)+'\n',encoding='utf-8')
    print('Imported packages: '+str(len(existing)),flush=True)
