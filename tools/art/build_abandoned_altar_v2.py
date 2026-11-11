"""제단 시안을 편집 가능한 Blender 모델, FBX와 6방향 렌더로 제작한다.

Blender --background --python tools/art/build_abandoned_altar_v2.py
원본의 개별 부품을 보존하고 내보낼 때만 임시 복제본을 하나로 합친다.
좌표는 Blender Z-up, 정면은 -Y. Unity의 FBX 루트 축 보정을 보존한다.
"""
import bpy
import bmesh
import json
import math
import random
import shutil
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'res/art-source/world/abandoned-altar-v2'
EXPORT = ROOT / 'res/models/world/AbandonedAltarV2.fbx'
PREVIEW = ROOT / 'res/previews/abandoned-altar-v2'
UNITY = ROOT / 'impls/unity/Afterlife/Assets/Devlog/Youtube/Day01/Models/AbandonedAltarV2.fbx'
for folder in (SOURCE, EXPORT.parent, PREVIEW, UNITY.parent):
    folder.mkdir(parents=True, exist_ok=True)
rng = random.Random(20260920)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
parts = bpy.data.collections.new('Altar_EditableParts')
scene.collection.children.link(parts)
studio = bpy.data.collections.new('PreviewStudio_NotExported')
scene.collection.children.link(studio)

def collection(obj, dest):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    dest.objects.link(obj)

palette = {
    'Stone': (.39, .455, .475),
    'LightStone': (.47, .525, .535),
    'OldStone': (.35, .407, .422),
    'Moss': (.235, .335, .115),
    'MossLight': (.34, .415, .155),
    'Inset': (.15, .23, .25),
    'Teal': (.13, .43, .405),
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    materials[name] = mat

root = bpy.data.objects.new('AbandonedAltarV2', None)
parts.objects.link(root)
root['description'] = 'Stone tabletop, twin supports, two steps, one broken column'
root['source_concept'] = 'res/concepts/abandoned-altar-v2/turnaround.png'
root['front_axis'] = '-Y in Blender / preserve imported FBX axes in Unity'

def finish(obj, name, material):
    obj.name = name
    collection(obj, parts)
    obj.parent = root
    obj.data.materials.append(materials[material])
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj

def stone(name, location, dimensions, material='Stone', bevel=.025, jitter=.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if jitter:
        for v in obj.data.vertices:
            v.co.x += rng.uniform(-jitter, jitter)
            v.co.y += rng.uniform(-jitter, jitter)
    if bevel:
        mod = obj.modifiers.new('ChippedStoneEdges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, name, material)

def mesh(name, vertices, faces, material='Stone'):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    parts.objects.link(obj)
    # 닫힌 돌 메시의 법선은 모두 바깥쪽을 향하게 한다.
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    return finish(obj, name, material)

# 두 단 기단. 중간 줄눈이 겹치지 않도록 위아래 돌 길이를 다르게 둔다.
for row, (depth, width, top, height, cuts) in enumerate([
    (1.38, 1.92, .145, .145, [-.96, -.26, .36, .96]),
    (1.14, 1.73, .30, .155, [-.865, -.04, .865]),
]):
    for index, (left, right) in enumerate(zip(cuts, cuts[1:])):
        stone(f'Step{row+1}_Block{index+1}', ((left+right)/2, 0, top-height/2),
              (right-left-.007, depth, height), 'LightStone' if row else 'Stone', .019, .006)

for side, x in [('Left', -.52), ('Right', .52)]:
    stone(f'{side}_Foot', (x, -.035, .355), (.36, .52, .115), 'Stone', .027)
    stone(f'{side}_Support', (x, -.035, .57), (.27, .39, .345), 'OldStone', .025, .006)
    stone(f'{side}_Capital', (x, -.035, .766), (.37, .51, .105), 'Stone', .023)

# 상판은 넓은 면이 주인공. 중앙 큰 돌과 양쪽 작은 돌만으로 분할한다.
cuts = [-.85, -.53, .49, .85]
for index, (left, right) in enumerate(zip(cuts, cuts[1:])):
    stone(f'Tabletop_Slab{index+1}', ((left+right)/2, -.065, .92),
          (right-left-.004, .97, .23), 'LightStone', .035, .005)

# 한쪽의 오래된 기둥. 끊어진 윗면을 실제 폐쇄 메시로 만들고 높이를 불규칙하게 준다.
stone('Column_Foundation', (-.83, .46, .23), (.49, .43, .19), 'OldStone', .025)
stone('Column_BaseMoulding', (-.83, .46, .354), (.385, .35, .09), 'Stone', .022)
cx, cy = -.83, .46
outline = [(-.14,-.17), (.14,-.17), (.18,-.13), (.18,.13),
           (.14,.17), (-.14,.17), (-.18,.13), (-.18,-.13)]
tops = [1.31, 1.34, 1.27, 1.45, 1.53, 1.43, 1.39, 1.32]
vertices = [(cx+x,cy+y,.39) for x,y in outline]
vertices += [(cx+x,cy+y,tops[i]) for i,(x,y) in enumerate(outline)]
vertices.append((cx+.018,cy+.025,1.29))
faces = [tuple(reversed(range(8)))]
faces += [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
faces += [(i+8,(i+1)%8+8,16) for i in range(8)]
mesh('Column_BrokenShaft', vertices, faces, 'Stone')

for index, (pos, dims) in enumerate([
    ((-1.12,-.02,.17), (.43,.38,.34)),
    ((-1.14,-.39,.077), (.23,.21,.154)),
    ((-1.02,.25,.105), (.25,.22,.21)),
]):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=pos)
    rock = bpy.context.object
    for v in rock.data.vertices:
        v.co *= rng.uniform(.87,1.10)
    rock.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    # 바닥 꼭짓점이 0 아래로 내려가지 않게 정렬한다.
    rock.location.z = -min(v.co.z for v in rock.data.vertices) + .005
    finish(rock, f'FallenStone{index+1}', 'OldStone' if index==0 else 'Stone')

# 정면 표식은 작은 돌 테두리와 얕은 청록 상감. 발광하는 Ruins와 구분한다.
for name, size, y, mat in [('RuneBorder',.145,-.555,'OldStone'),
                          ('RuneRim',.115,-.559,'LightStone'),
                          ('RuneInset',.089,-.563,'Inset'),
                          ('RuneTeal',.062,-.567,'Teal')]:
    obj=stone(name,(0,y,.918),(size,.008,size),mat,.001)
    obj.rotation_euler.y=math.pi/4

def patch(name, center, size, plane='top', material='Moss'):
    # 이끼는 극히 얇은 오목/볼록 실루엣 패치. 돌 위 면에만 얹어 공중에 뜨지 않는다.
    x,y,z=center
    vertices=[center]
    for i in range(16):
        angle=i*math.tau/16
        radius=rng.uniform(.55,1) * (1 if i%2 else .73)
        u=math.cos(angle)*size[0]*radius
        v=math.sin(angle)*size[1]*radius
        vertices.append((x+u,y+v,z) if plane=='top' else (x+u,y,z+v))
    faces=[(0,i+1,(i+1)%16+1) for i in range(16)]
    obj=mesh(name,vertices,faces,material)
    # 위쪽 혹은 정면을 향하는 열린 패치 법선.
    for poly in obj.data.polygons:
        desired=Vector((0,0,1)) if plane=='top' else Vector((0,-1,0))
        if poly.normal.dot(desired)<0: poly.flip()
    return obj

for i,(center,size,plane) in enumerate([
    ((-.74,-.43,1.038),(.065,.085),'top'),
    ((-.66,.30,1.038),(.16,.035),'top'),
    ((.71,-.39,1.038),(.095,.12),'top'),
    ((.79,.29,1.038),(.033,.07),'top'),
    ((-.73,-.552,.949),(.064,.047),'front'),
    ((.76,-.552,.908),(.047,.085),'front'),
    ((-.63,-.49,.303),(.17,.05),'top'),
    ((.66,-.46,.303),(.11,.06),'top'),
    ((.77,.33,.303),(.037,.15),'top'),
    ((-.65,-.692,.079),(.18,.035),'front'),
    ((.79,-.692,.068),(.082,.038),'front'),
    ((.46,-.619,.149),(.15,.04),'top'),
    ((-.80,.287,.433),(.1,.033),'front'),
    ((-.93,.287,1.22),(.024,.09),'front'),
    ((-.51,-.302,.357),(.095,.025),'front'),
]):
    patch(f'MossPatch{i+1:02}',center,size,plane)
    # 드문 작은 밝은 이끼가 주 패치를 분절한다.
    if i%3==0:
        offset=(center[0]+.012,center[1]-.001,center[2]+.001)
        patch(f'MossAccent{i+1:02}',offset,(size[0]*.4,size[1]*.5),plane,'MossLight')

# 얕은 균열 두 개: 상판 윗면의 좁은 끊어진 선. 물리 조명과 관계없는 실제 틈 표현.
def crack(name, points, width=.004):
    for index,(a,b) in enumerate(zip(points,points[1:])):
        midpoint=(Vector(a)+Vector(b))*.5
        length=(Vector(b)-Vector(a)).length
        obj=stone(f'{name}_{index}',midpoint,(width,length,.001),'OldStone',0)
        obj.rotation_euler.z=-math.atan2(b[0]-a[0],b[1]-a[1])
crack('TableChipLeft',[(-.82,-.18,1.039),(-.72,-.21,1.039),(-.68,-.32,1.039)])
crack('TableChipRight',[(.80,.16,1.039),(.66,.11,1.039),(.60,-.03,1.039)])

# 모든 컬러를 face-corner 정점색에 굽는다. 조명/그림자는 베이크하지 않는다.
vertex_material=bpy.data.materials.new('AltarVertexPalette')
vertex_material.use_nodes=True
bsdf=vertex_material.node_tree.nodes.get('Principled BSDF')
bsdf.inputs['Roughness'].default_value=.91
node=vertex_material.node_tree.nodes.new('ShaderNodeVertexColor')
node.layer_name='Color'
vertex_material.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color'])
meshes=[obj for obj in parts.objects if obj.type=='MESH']
for obj in meshes:
    data=obj.data
    colors=data.color_attributes.new(name='Color',type='BYTE_COLOR',domain='CORNER')
    data.color_attributes.active_color=colors
    for poly in data.polygons:
        base=data.materials[poly.material_index].diffuse_color
        factor=rng.uniform(.945,1.04)
        for index in poly.loop_indices:
            colors.data[index].color=tuple(min(1,c*factor) for c in base[:3])+(1,)
    data.materials.clear()
    data.materials.append(vertex_material)

bpy.context.view_layer.update()
points=[obj.matrix_world@v.co for obj in meshes for v in obj.data.vertices]
bounds_min=[min(v[i] for v in points) for i in range(3)]
bounds_max=[max(v[i] for v in points) for i in range(3)]
triangle_count=sum(sum(len(p.vertices)-2 for p in obj.data.polygons) for obj in meshes)

# 원본 부품은 그대로 두고 복제 메시만 통합해 FBX를 한 렌더러로 내보낸다.
bpy.ops.object.select_all(action='DESELECT')
duplicates=[]
for obj in meshes:
    copy=obj.copy()
    copy.data=obj.data.copy()
    scene.collection.objects.link(copy)
    copy.parent=None
    copy.matrix_world=obj.matrix_world.copy()
    copy.select_set(True)
    duplicates.append(copy)
bpy.context.view_layer.objects.active=duplicates[0]
bpy.ops.object.join()
export_mesh=bpy.context.object
export_mesh.name='AbandonedAltarV2_Mesh'
scene.cursor.location=(0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
export_mesh.data.materials.clear()
export_mesh.data.materials.append(vertex_material)
# 나중에 손으로 그린 텍스처를 적용할 수 있도록 UV도 남긴다.
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(island_margin=.015)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.fbx(filepath=str(EXPORT),use_selection=True,
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,
    colors_type='LINEAR',use_mesh_modifiers=True,object_types={'MESH'})
bpy.data.objects.remove(export_mesh,do_unlink=True)
shutil.copy2(EXPORT,UNITY)

# 모델링 확인용 스튜디오는 FBX에 포함하지 않는다.
ground=stone('PreviewGround',(0,0,-.045),(200,200,.06),'LightStone',0)
collection(ground,studio)
ground.parent=None
groundmat=bpy.data.materials.new('PreviewGroundMaterial')
groundmat.diffuse_color=(.24,.285,.28,1)
ground.data.materials.clear()
ground.data.materials.append(groundmat)
world=bpy.data.worlds.new('AltarPreviewWorld')
world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.54,.60,.64,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.55
scene.world=world
def light(name,location,energy,size):
    data=bpy.data.lights.new(name,'AREA')
    obj=bpy.data.objects.new(name,data)
    studio.objects.link(obj)
    obj.location=location
    obj.rotation_euler=(Vector((0,0,.5))-obj.location).to_track_quat('-Z','Y').to_euler()
    data.energy=energy
    data.shape='DISK'
    data.size=size
light('Key',(-3,-4,6),460,4)
light('Fill',(4,-1,3),230,5)
light('Rim',(0,4,4),320,3)
camera_data=bpy.data.cameras.new('PreviewCamera')
camera=bpy.data.objects.new('PreviewCamera',camera_data)
studio.objects.link(camera)
scene.camera=camera
camera_data.type='ORTHO'
camera_data.ortho_scale=3.20
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.resolution_x=960
scene.render.resolution_y=800
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
target=Vector((-.12,0,.73))
views={'front':(0,-6,0),'right':(6,0,0),'back':(0,6,0),
       'left':(-6,0,0),'top':(0,0,6),'three-quarter':(4,-6,3.5)}
for name,offset in views.items():
    ground.hide_render=name in ('front','right','back','left')
    camera.location=target+Vector(offset)
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.ortho_scale=3.25 if name!='top' else 3.0
    scene.render.filepath=str(PREVIEW/(name+'.png'))
    bpy.ops.render.render(write_still=True)

manifest={'name':'AbandonedAltarV2','seed':20260920,
          'sourceConcept':'res/concepts/abandoned-altar-v2/turnaround.png',
          'editableMeshes':len(meshes),'exportedMeshes':1,'triangles':triangle_count,
          'blenderBoundsMin':bounds_min,'blenderBoundsMax':bounds_max,
          'sizeXYZ':[bounds_max[i]-bounds_min[i] for i in range(3)],
          'vertexColor':'Color / CORNER / BYTE_COLOR','renderViews':list(views),
          'fbxAxisForward':'-Z','fbxAxisUp':'Y',
          'notes':'Geometry and palette only; no baked lighting. Studio is not exported.'}
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
# Blender 열었을 때 사선 모델이 바로 보이도록 한다.
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
bpy.context.view_layer.objects.active=root
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_distance=4.2
        area.spaces.active.region_3d.view_location=target
        area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        area.spaces.active.shading.type='MATERIAL'
studio.hide_viewport=True
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'AbandonedAltarV2.blend'))
print('ALTAR_BUILD_COMPLETE '+json.dumps(manifest))
