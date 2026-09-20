"""기존 도구 메시의 실제 날 표면을 찾아 접촉 마커를 추가한다."""
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

root_path = Path(__file__).resolve().parents[2]
source = root_path / 'res/art-source/world/AfterlifeWorld.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
for name, height in [('AxeTool', .48), ('PickTool', .40)]:
    root = bpy.data.objects[name]
    catalog_transform = root.matrix_world.copy()
    root.matrix_world = Matrix.Identity(4)
    bpy.context.view_layer.update()
    origin, direction = Vector((2, 0, height)), Vector((-1, 0, 0))
    contact, distance = None, float('inf')
    for obj in root.children_recursive:
        if obj.type != 'MESH':
            continue
        inverse = obj.matrix_world.inverted()
        hit, point, _, _ = obj.ray_cast(inverse @ origin, (inverse.to_3x3() @ direction).normalized())
        if hit:
            world = obj.matrix_world @ point
            candidate = (world-origin).length
            if candidate < distance:
                distance, contact = candidate, world
    if contact is None:
        raise RuntimeError(f'{name}: cutting surface not found')
    marker = next((child for child in root.children if child.name.split('.')[0] == 'ImpactPoint'), None)
    if marker is None:
        marker = bpy.data.objects.new('ImpactPoint', None)
        bpy.context.collection.objects.link(marker)
        marker.parent = root
    marker.location = root.matrix_world.inverted() @ contact
    marker.empty_display_type, marker.empty_display_size = 'SPHERE', .015
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    output = root_path / f'impls/unity/Afterlife/Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, axis_forward='-Z', axis_up='Y',
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', bake_anim=False,
        add_leaf_bones=False, colors_type='LINEAR')
    print(f'{name} ImpactPoint on mesh (Blender world): {tuple(contact)}')
    root.matrix_world = catalog_transform
    bpy.context.view_layer.update()
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(source))
