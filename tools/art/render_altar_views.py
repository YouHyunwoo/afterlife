"""저장된 제단 원본의 6방향 미리보기만 다시 렌더한다. FBX/Unity 에셋은 건드리지 않는다.

blender --background res/art-source/world/abandoned-altar-v2/AbandonedAltarV2.blend
        --python tools/art/render_altar_views.py
"""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
scene=bpy.context.scene
target=Vector((-.12,0,.73))
camera=scene.camera
ground=bpy.data.objects['PreviewGround']
views={'front':(0,-6,0),'right':(6,0,0),'back':(0,6,0),
       'left':(-6,0,0),'top':(0,0,6),'three-quarter':(4,-6,3.5)}
for name,offset in views.items():
    # 수평 정투영에서 바닥 단면이 검은 띠로 보이지 않도록 정면/측면은 바닥을 숨긴다.
    ground.hide_render=name in ('front','right','back','left')
    camera.location=target+Vector(offset)
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=3.0 if name=='top' else 3.25
    scene.render.filepath=str(ROOT/'res/previews/abandoned-altar-v2'/(name+'.png'))
    bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
