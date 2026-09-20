import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
out=root/'impls/unity/Afterlife/Assets/_AfterlifePrototype/Resources/AfterlifePrototype/Animals'
source=root/'res/art-source/animals'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1);m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85;return m
brown=mat('Chestnut coat',(.42,.23,.09));tan=mat('Cream muzzle',(.74,.57,.35));dark=mat('Hooves nose eyes',(.035,.023,.016));white=mat('Cream fur',(.84,.81,.69));orange=mat('Fox rust',(.66,.20,.055));boar=mat('Boar coarse fur',(.23,.16,.12));pink=mat('Inner ears',(.62,.34,.30));antler=mat('Antler bone',(.60,.47,.28))
def empty(name,loc,parent):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=loc;o.parent=parent;return o
def ico(name,loc,scale,material,parent):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(material);o.parent=parent;return o
def rod(name,a,b,r1,r2,material,parent,vertices=6):
 a,b=Vector(a),Vector(b);d=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=d.length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.parent=parent;o.data.materials.append(material);return o
animals=[]
for name in ['Deer','Rabbit','Boar','Fox']:
 rig=empty(name,(0,0,0),None);coat=brown if name=='Deer' else white if name=='Rabbit' else boar if name=='Boar' else orange
 length=.46 if name!='Rabbit' else .30;height=.68 if name=='Deer' else .42 if name=='Rabbit' else .48
 ico('Body',(0,0,height),(.23,length,.24),coat,rig)
 ico('Chest',(0,-length*.58,height+.02),(.22,.25,.26),coat,rig)
 for side,label in [(-1,'L'),(1,'R')]:
  for front,suffix in [(-1,'F'),(1,'B')]:
   leg=empty('Leg'+suffix+label,(side*.16,front*length*.62,height-.08),rig)
   rod('Upper leg',(0,0,0),(0,.03,-height*.47),.075 if name!='Deer' else .052,.045,coat,leg)
   rod('Lower leg',(0,.03,-height*.47),(0,0,-height+.10),.04,.032,coat,leg)
   ico('Foot',(0,-.055,-height+.08),(.055,.10,.04),dark if name in ['Deer','Boar'] else coat,leg)
 if name=='Deer':
  rod('Long neck',(0,-.31,.68),(0,-.49,1.05),.15,.105,coat,rig)
  ico('Head',(0,-.53,1.10),(.13,.19,.14),coat,rig);ico('Muzzle',(0,-.70,1.05),(.085,.12,.075),tan,rig)
  for side in [-1,1]:
   ico('Ear',(side*.16,-.49,1.21),(.10,.045,.07),coat,rig)
   rod('Antler main',(side*.07,-.49,1.23),(side*.19,-.40,1.63),.027,.008,antler,rig)
   rod('Antler tine',(side*.13,-.43,1.43),(side*.27,-.46,1.53),.018,.004,antler,rig)
   rod('Antler fork',(side*.11,-.46,1.35),(side*.07,-.59,1.47),.018,.004,antler,rig)
  ico('Tail',(0,.48,.72),(.08,.13,.08),tan,rig)
 elif name=='Rabbit':
  ico('Haunch',(0,.16,.39),(.27,.27,.26),coat,rig);ico('Head',(0,-.29,.60),(.17,.17,.17),coat,rig)
  for side in [-1,1]:
   ico('Long ear',(side*.085,-.28,.84),(.055,.045,.23),coat,rig);ico('Inner ear',(side*.085,-.322,.85),(.027,.012,.16),pink,rig)
  ico('Tail',(0,.36,.46),(.095,.09,.095),white,rig)
 elif name=='Boar':
  ico('Head',(0,-.43,.48),(.22,.23,.22),coat,rig);ico('Snout',(0,-.66,.41),(.13,.12,.105),pink,rig)
  for side in [-1,1]:
   rod('Tusk',(side*.15,-.57,.35),(side*.18,-.64,.50),.035,.004,white,rig);ico('Ear',(side*.15,-.33,.70),(.07,.06,.10),coat,rig)
  for y in [-.15,0,.15,.30]:rod('Back bristle',(0,y,.66),(0,y,.79),.06,0,dark,rig)
  rod('Tail',(0,.42,.46),(.06,.59,.51),.028,.006,coat,rig)
 else:
  ico('Head',(0,-.44,.65),(.16,.18,.17),coat,rig);rod('Muzzle',(0,-.48,.61),(0,-.72,.59),.105,.035,white,rig)
  for side in [-1,1]:rod('Pointed ear',(side*.11,-.40,.74),(side*.14,-.37,.95),.09,0,coat,rig)
  rod('Bushy tail',(0,.37,.45),(.10,.84,.36),.13,.18,coat,rig);rod('White tail tip',(.10,.84,.36),(.17,1.10,.45),.18,.01,white,rig)
 eyez=1.13 if name=='Deer' else .64 if name=='Rabbit' else .55 if name=='Boar' else .68
 eyey=-.64 if name=='Deer' else -.40 if name=='Rabbit' else -.58 if name=='Boar' else -.57
 for side in [-1,1]:ico('Eye',(side*(.108 if name!='Boar' else .15),eyey,eyez),(.027,.025,.028),dark,rig)
 ico('Nose',(0,-.78 if name=='Deer' else -.445 if name=='Rabbit' else -.77 if name=='Boar' else -.74,eyez-.07),(.04,.025,.03),dark,rig)
 # Group face/ears/antlers around a real neck pivot so they remain attached while nodding.
 head_parts=[o for o in rig.children if any(o.name.split('.')[0].startswith(k) for k in ['Head','Muzzle','Snout','Ear','Pointed ear','Long ear','Inner ear','Eye','Nose','Antler','Tusk'])]
 pivot=empty('HeadPivot',(0,eyey+.12,eyez-.12),rig)
 bpy.context.view_layer.update()
 for part in head_parts:
  world=part.matrix_world.copy();part.parent=pivot;part.matrix_world=world
 tail_parts=[o for o in rig.children if 'tail' in o.name.lower()]
 tail_pivot=empty('TailPivot',(0,length*.75,height),rig)
 bpy.context.view_layer.update()
 for part in tail_parts:
  world=part.matrix_world.copy();part.parent=tail_pivot;part.matrix_world=world
 bpy.ops.object.select_all(action='DESELECT')
 rig.select_set(True)
 for o in rig.children_recursive:o.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
 rig.location.x=(len(animals)-1.5)*1.7;animals.append(rig)
bpy.ops.object.camera_add(location=(4,-7,4));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=7.7;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(0,-3,6));bpy.context.object.data.energy=450;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=5
scene=bpy.context.scene;scene.world.color=(.2,.2,.2);scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(source/'animal-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'AfterlifeAnimals.blend'));bpy.ops.render.render(write_still=True)
