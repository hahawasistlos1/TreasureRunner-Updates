from pathlib import Path
import bpy
root=Path(__file__).resolve().parents[2]
source=next((root/'Assets/Art/FreeLibrary/kay-adventurers').rglob('Knight.glb'))
bpy.ops.import_scene.gltf(filepath=str(source))
for obj in bpy.context.scene.objects:
    if obj.type in {'MESH','ARMATURE'}:
        print('OBJECT',obj.name,obj.type,'scale',tuple(obj.scale),'dimensions',tuple(obj.dimensions), 'parent',obj.parent.name if obj.parent else None,flush=True)
        if obj.type=='MESH':print('MESH',len(obj.vertex_groups),'modifiers',[(m.type,m.object.name if m.type=='ARMATURE' and m.object else None) for m in obj.modifiers],flush=True)
output=root/'Assets/Art/FreeLibrary/kay-adventurers/Diagnostics';output.mkdir(exist_ok=True)
for mode in ['FBX_SCALE_UNITS','FBX_SCALE_ALL']:
    bpy.ops.export_scene.fbx(filepath=str(output/(mode+'.fbx')),object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,apply_scale_options=mode,axis_forward='-Z',axis_up='Y')
