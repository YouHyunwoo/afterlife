"""Build a two-mesh Tree0 model from the existing vertex-colored FBX.

Run with: blender -b -t 1 --python Split-Tree0.py
"""

from pathlib import Path

import bmesh
import bpy


world = Path(__file__).resolve().parents[2] / "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World"
source = world / "Tree0.fbx"
output = world / "Tree0_Split.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
original = bpy.data.objects["Tree0_Mesh"]
mesh = original.data
colors = mesh.color_attributes.active_color
if colors is None or colors.domain != "CORNER":
    raise RuntimeError("Tree0 needs face-corner vertex colors to separate its crown.")

green_faces = set()
for face in mesh.polygons:
    samples = [colors.data[index].color for index in face.loop_indices]
    red = sum(color[0] for color in samples) / len(samples)
    green = sum(color[1] for color in samples) / len(samples)
    blue = sum(color[2] for color in samples) / len(samples)
    if green > red * 1.08 and green > blue * 1.05:
        green_faces.add(face.index)

if not green_faces or len(green_faces) == len(mesh.polygons):
    raise RuntimeError("Tree0 must contain both green crown and brown trunk faces.")


def make_part(name, keep_faces):
    part = original.copy()
    part.data = mesh.copy()
    part.name = name
    part.data.name = name
    bpy.context.collection.objects.link(part)
    editable = bmesh.new()
    editable.from_mesh(part.data)
    editable.faces.ensure_lookup_table()
    editable.faces.index_update()
    bmesh.ops.delete(
        editable,
        geom=[face for face in editable.faces if face.index not in keep_faces],
        context="FACES",
    )
    bmesh.ops.delete(
        editable,
        geom=[vertex for vertex in editable.verts if not vertex.link_faces],
        context="VERTS",
    )
    editable.to_mesh(part.data)
    editable.free()
    part.data.update()
    return part


trunk = make_part("Tree0_Trunk", set(range(len(mesh.polygons))) - green_faces)
crown = make_part("Tree0_Crown", green_faces)
expected_faces = len(mesh.polygons)
if len(trunk.data.polygons) + len(crown.data.polygons) != expected_faces:
    raise RuntimeError("The separated model lost faces.")

bpy.data.objects.remove(original, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for item in (bpy.data.objects["Tree0"], bpy.data.objects["FrontMarker.017"], trunk, crown):
    item.select_set(True)
bpy.context.view_layer.objects.active = trunk
bpy.ops.export_scene.fbx(
    filepath=str(output),
    use_selection=True,
    use_mesh_modifiers=False,
    add_leaf_bones=False,
    bake_anim=False,
)
print(f"Tree0 split: trunk {len(trunk.data.polygons)}, crown {len(crown.data.polygons)} faces -> {output}")
