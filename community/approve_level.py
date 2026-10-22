"""Usage: python approve_level.py level-code.txt catalog.json. Local curator helper, no network."""
import base64, collections, hashlib, json, pathlib, sys, zlib
def validate(level):
    assert level.get('format')==1 and 0<=level.get('world',-1)<8,'Unsupported format/world'
    assert 16<=level.get('width',0)<=96 and 8<=level.get('height',0)<=16,'Invalid dimensions'
    assert 0<len(level.get('title',''))<=40 and 0<len(level.get('author',''))<=24,'Invalid title/author'
    assert 1<=level.get('revision',0)<=10000 and len(level.get('id',''))<=64,'Invalid revision/id'
    tiles=level['tiles'];assert len(tiles)<=650
    cells=set();floors=set();goals=[]
    for t in tiles:
        x,y,k=t['x'],t['y'],t['kind'];assert all(type(v) is int for v in (x,y,k))
        assert 0<=x<level['width'] and 0<=y<level['height'] and 0<=k<=13
        assert (x,y) not in cells,'Duplicate cell';cells.add((x,y))
        if k!=1:floors.add((x,y))
        if k==9:goals.append((x,y))
    start=(level['spawnX'],level['spawnY']);assert start in floors and len(goals)==1
    seen={start};pending=collections.deque([start])
    while pending:
        x,y=pending.popleft()
        for nx,ny in floors:
            if (nx,ny) not in seen and abs(nx-x)<=4 and -6<=ny-y<=1:
                seen.add((nx,ny));pending.append((nx,ny))
    assert goals[0] in seen,'Disconnected goal'
def main():
    code=pathlib.Path(sys.argv[1]).read_text(encoding='utf-8').split('TRL1:',1)[1].split()[0].strip('`')
    assert len(code)<=100000
    unpacker=zlib.decompressobj(-15);raw=unpacker.decompress(base64.b64decode(code,validate=True),65537)
    assert len(raw)<=65536 and unpacker.eof,'Compressed level exceeds limit or is incomplete'
    level=json.loads(raw);validate(level)
    if not level.get('id'):level['id']=hashlib.sha256(raw).hexdigest()[:32]
    target=pathlib.Path(sys.argv[2]);catalog=json.loads(target.read_text(encoding='utf-8'))
    assert catalog['version']==1
    old=next((l for l in catalog['levels'] if l['id']==level['id']),None)
    if old:
        assert level['revision']>old['revision'],'Increase the revision to update a published level'
        catalog['levels'][catalog['levels'].index(old)]=level
    else:catalog['levels'].append(level)
    assert len(catalog['levels'])<=30
    for item in catalog['levels']:validate(item)
    text=json.dumps(catalog,ensure_ascii=False,separators=(',',':'))
    assert len(text.encode('utf-8'))<=262144
    target.with_suffix('.json.backup').write_bytes(target.read_bytes());target.write_text(text,encoding='utf-8')
    print('Validated catalog saved. Test the level in-game, then publish community/catalog.json on TennoFarm.')
if __name__=='__main__':main()
