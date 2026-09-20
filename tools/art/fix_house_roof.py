"""기존 House 지붕의 안쪽 면만 뒤집고 같은 FBX로 다시 내보낸다."""
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

root_path = Path(__file__).resolve().parents[2]
source = root_path / 'res/art-source/world/AfterlifeWorld.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
root = bpy.data.objects.get('House')
if root is None:
    raise RuntimeError('House source not found')
# shell(width=.78,height=.84)가 호출하는 roof(.93,.94,.97,.39)의 여섯 꼭짓점.
corners = [Vector((x, -z, y)) for x, y, z in [
    (-.465,.97,-.47),(.465,.97,-.47),(0,1.36,-.47),
    (-.465,.97,.47),(.465,.97,.47),(0,1.36,.47)]]
fixed = 0
matched = 0
for obj in root.children_recursive:
    if obj.type != 'MESH':
        continue
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    faces = []
    for face in bm.faces:
        points = [obj.matrix_world @ vertex.co for vertex in face.verts]
        if not all(any((point-corner).length < .002 for corner in corners) for point in points):
            continue
        matched += 1
        normal = obj.matrix_world.to_3x3().inverted().transposed() @ face.normal
        center = sum(points, Vector()) / len(points)
        outward = center - Vector((0,0,1.08))
        if normal.dot(outward) < 0:
            faces.append(face)
    fixed += len(faces)
    bmesh.ops.reverse_faces(bm, faces=faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
if matched != 4:
    raise RuntimeError(f'Expected four roof faces, found {matched}; no files saved')
if fixed:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for child in root.children_recursive:
    child.select_set(True)
bpy.context.view_layer.objects.active = root
output = root_path / 'impls/unity/Afterlife/Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/House.fbx'
bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_anim=False,
    add_leaf_bones=False, colors_type='LINEAR')
print(f'House roof: {fixed} inward faces corrected; {matched} roof faces; exported {output}')
