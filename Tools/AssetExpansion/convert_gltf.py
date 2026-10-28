"""Run inside installed Blender with --background --factory-startup; data conversion only."""
from pathlib import Path
import hashlib,json,re,sys
import bpy
ROOT=Path(__file__).resolve().parents[2];REPORT=ROOT/'Logs/AssetExpansion'
PACKS=['kay-adventurers','kay-skeletons-full','kay-dungeon-full']
converted=[]
for key in PACKS:
    pack_root=ROOT/'Assets/Art/FreeLibrary'/key
    metadata_path=REPORT/(key+'.json');metadata=json.loads(metadata_path.read_text())
    paths=sorted(p for p in pack_root.rglob('*') if p.suffix.lower() in {'.glb','.gltf'})
    # Prefer embedded GLB when both versions of the same item exist.
    bases={p.stem.replace('.gltf','') for p in paths if p.suffix=='.glb'}
    paths=[p for p in paths if p.suffix=='.glb' or p.stem not in bases]
    for source in paths:
        assert source.resolve().is_relative_to(pack_root.resolve())
        if source.suffix=='.gltf':
            doc=json.loads(source.read_text())
            for item in doc.get('buffers',[])+doc.get('images',[]):
                uri=item.get('uri','')
                assert uri.startswith('data:') or (not re.match(r'^[a-z]+:',uri,re.I) and (source.parent/uri).resolve().is_relative_to(pack_root.resolve())),'External model reference'
        name=source.stem.replace('.gltf','');target=pack_root/'Converted'/(name+'.fbx');target.parent.mkdir(parents=True,exist_ok=True)
        digest=hashlib.file_digest(source.open('rb'),'sha256').hexdigest()
        marker=target.with_suffix('.conversion.json')
        version=3 if key in {'kay-adventurers','kay-skeletons-full'} else 2
        if not target.exists() or not marker.exists() or json.loads(marker.read_text()).get('converterVersion')!=version or json.loads(marker.read_text())['sourceSha256']!=digest:
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.gltf(filepath=str(source))
            texture_root=target.parent/'Textures';texture_root.mkdir(exist_ok=True)
            replacements={}
            for image in list(bpy.data.images):
                if image.packed_file:
                    data=bytes(image.packed_file.data);extension='.jpg' if data.startswith(b'\xff\xd8') else '.png'
                    texture=texture_root/(re.sub('[^A-Za-z0-9_.-]','_',Path(image.name).stem)+'-'+hashlib.sha256(data).hexdigest()[:8]+extension)
                    texture.write_bytes(data);replacements[image]=bpy.data.images.load(str(texture),check_existing=True)
            for material in bpy.data.materials:
                if material.use_nodes:
                    for node in material.node_tree.nodes:
                        if node.type=='TEX_IMAGE' and node.image in replacements:node.image=replacements[node.image]
            bpy.ops.export_scene.fbx(filepath=str(target),use_selection=False,object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,path_mode='RELATIVE',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=1.0,apply_scale_options='FBX_SCALE_UNITS' if version==3 else 'FBX_SCALE_NONE',axis_forward='-Z',axis_up='Y')
            marker.write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),'sourceSha256':digest,'converterVersion':version,'converter':'installed Blender 5.2 glTF to FBX'})+'\n')
        record={'path':target.relative_to(ROOT).as_posix(),'sha256':hashlib.file_digest(target.open('rb'),'sha256').hexdigest(),'bytes':target.stat().st_size,'derivedFrom':source.relative_to(ROOT).as_posix(),'sourceSha256':digest}
        metadata['files']=[f for f in metadata['files'] if f['path']!=record['path']]+[record];converted.append(record)
        print('Converted '+key+'/'+name,flush=True)
    metadata_path.write_text(json.dumps(metadata,indent=2)+'\n')
packs=[json.loads(p.read_text()) for p in REPORT.glob('*.json') if p.name not in {'Imports.json','Converted.json'}]
packs=[p for p in packs if isinstance(p,dict) and p.get('status')=='imported-data']
(REPORT/'Imports.json').write_text(json.dumps({'packs':sorted(packs,key=lambda p:p['id'])},indent=2)+'\n')
(REPORT/'Converted.json').write_text(json.dumps({'files':converted,'count':len(converted)},indent=2)+'\n')
print('CONVERSION COMPLETE: '+str(len(converted))+' models',flush=True)
