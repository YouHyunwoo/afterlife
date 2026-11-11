"""Afterlife authored low-poly world. Run with Blender --background --python.
All dimensions use game metres (X/right, Y/up, Z/front). No third-party assets.
Rigid parts are joined per articulation; flat corner colours preserve readable facets.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'impls/unity/Afterlife/Assets/Prototype/Resources/AfterlifePrototype/World'
SRC = ROOT/'res/art-source/world'
OUT.mkdir(parents=True, exist_ok=True); SRC.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
random.seed(3701)
PAL = dict(wood=(.29,.15,.075), oak=(.51,.30,.13), cut=(.76,.56,.30), plaster=(.83,.75,.57),
 stone=(.43,.49,.50), slate=(.22,.29,.33), iron=(.57,.65,.68), blade=(.81,.89,.88),
 brass=(.77,.53,.19), dark=(.055,.072,.078), roof=(.47,.16,.105), tile=(.64,.25,.13),
 teal=(.12,.47,.44), blue=(.16,.30,.48), gold=(.97,.76,.28), cream=(.95,.87,.65),
 green=(.20,.41,.15), leaf=(.32,.53,.20), pine=(.11,.30,.21), bark=(.30,.19,.105),
 skin=(.79,.58,.38), goblin=(.38,.57,.19), orc=(.31,.40,.14), lizard=(.12,.46,.39),
 purple=(.39,.23,.50), glow=(.32,.88,.78), dirt=(.26,.18,.10))
materials={}
for key,col in PAL.items():
 m=bpy.data.materials.new(key);m.diffuse_color=(*col,1);materials[key]=m

def v(p): return (p[0],-p[2],p[1])
def empty(name,pos=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.parent=parent;o.location=v(pos);return o
def finish(o,name,pos,scale,mat,parent):
 o.name=name;o.parent=parent;o.location=v(pos);o.scale=(scale[0],scale[2],scale[1]);o.data.materials.append(materials[mat]);return o
def cube(name,pos,size,mat,parent,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1)
 o=finish(bpy.context.object,name,pos,size,mat,parent)
 if bevel:
  bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  mod=o.modifiers.new('Carved edges','BEVEL');mod.width=bevel;mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def ico(name,pos,size,mat,parent,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1)
 return finish(bpy.context.object,name,pos,size,mat,parent)
def cone(name,pos,r1,r2,height,mat,parent,n=8):
 bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r1,radius2=r2,depth=height)
 return finish(bpy.context.object,name,pos,(1,1,1),mat,parent)
def beam(name,a,b,r,mat,parent,r2=None,n=6):
 aa,bb=Vector(v(a)),Vector(v(b));d=bb-aa
 bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r if r2 is None else r2,depth=d.length)
 o=bpy.context.object;o.name=name;o.parent=parent;o.location=(aa+bb)/2;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(materials[mat]);return o
def mesh(name,verts,faces,mat,parent):
 data=bpy.data.meshes.new(name);data.from_pydata([v(p) for p in verts],[],faces);data.update()
 o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.parent=parent;data.materials.append(materials[mat]);return o
def roof(parent,w=.94,d=.92,y=.93,h=.39,mat='roof'):
 mesh('Gabled roof',[(-w/2,y,-d/2),(w/2,y,-d/2),(0,y+h,-d/2),(-w/2,y,d/2),(w/2,y,d/2),(0,y+h,d/2)],[(2,1,0),(4,5,3),(3,5,2,0),(2,5,4,1)],mat,parent)
 for side in [-1,1]:
  for row in range(3):
   t=(row+1)/4
   beam('Roof course',(side*w*.5*t,y+h*(1-t)+.016,-d/2),(side*w*.5*t,y+h*(1-t)+.016,d/2),.012,'tile' if mat=='roof' else mat,parent,n=4)
 for z in [-d*.5,d*.5]:
  beam('Bargeboard',(-w/2,y,z),(0,y+h,z),.023,'wood',parent)
  beam('Bargeboard',(0,y+h,z),(w/2,y,z),.023,'wood',parent)
 beam('Ridge cap',(0,y+h+.01,-d/2-.025),(0,y+h+.01,d/2+.025),.037,'oak',parent)
def window(parent,x,y,z):
 cube('Window recess',(x,y,z),(.18,.22,.02),'wood',parent)
 cube('Amber glazing',(x,y,z+.012),(.135,.17,.014),'gold',parent)
 cube('Mullion',(x,y,z+.025),(.022,.19,.02),'oak',parent)
 cube('Window sill',(x,y-.11,z+.035),(.22,.035,.07),'oak',parent)
def shell(parent,width=.78,height=.84):
 cube('Stone footing',(0,.07,0),(width+.10,.14,.88),'slate',parent,.025)
 cube('Limewashed walls',(0,height*.5+.13,0),(width,height,.72),'plaster',parent,.017)
 for x in [-width*.5,width*.5]:
  for z in [-.37,.37]:cube('Corner timber',(x,height*.5+.13,z),(.065,height+.05,.07),'wood',parent)
 for y in [.19,height+.1]:cube('Facade crossbeam',(0,y,.385),(width+.08,.065,.065),'wood',parent)
 for side in [-1,1]:
  for y in [.19,height+.1]:cube('Side sill',(side*(width*.5+.013),y,0),(.052,.065,.79),'wood',parent)
  cube('Side post',(side*(width*.5+.017),height*.5+.13,0),(.06,height,.055),'wood',parent)
  for z in [-.21,.21]:
   cube('Side window recess',(side*(width*.5+.026),.63,z),(.02,.21,.14),'wood',parent)
   cube('Side glazing',(side*(width*.5+.040),.63,z),(.013,.16,.10),'gold',parent)
   cube('Side mullion',(side*(width*.5+.052),.63,z),(.016,.17,.018),'oak',parent)
 cube('Door frame',(0,.38,.379),(.27,.45,.055),'wood',parent)
 for i in range(4):cube('Door plank',(-.081+i*.054,.38,.412),(.049,.38,.025),'oak',parent)
 ico('Door latch',(.068,.38,.439),(.018,.018,.018),'brass',parent)
 cube('Doorstep',(0,.115,.44),(.36,.09,.18),'stone',parent,.018)
 roof(parent,width+.15,.94,height+.13)
def barrel(parent,pos,scale=.16):
 x,y,z=pos;cone('Barrel',(x,y+scale,z),scale*.65,scale*.65,scale*2,'oak',parent,10)
 for yy in [.35,1.6]:cone('Barrel hoop',(x,y+scale*yy,z),scale*.68,scale*.68,.025,'iron',parent,10)
def sword(parent,scale=1):
 cube('Leather grip',(0,.075*scale,0),(.055*scale,.18*scale,.055*scale),'wood',parent)
 ico('Pommel',(0,-.02,0),(.045,.045,.04),'brass',parent)
 cube('Crossguard',(0,.17*scale,0),(.27*scale,.042,.075),'brass',parent,.01)
 mesh('Forged blade',[(-.047,.19,0),(.047,.19,0),(.036,.64,0),(0,.77,0),(-.036,.64,0),(0,.19,.028),(0,.65,.022),(0,.19,-.028),(0,.65,-.022)],[(0,4,6,5),(5,6,3,2,1),(0,7,8,4),(7,1,2,3,8),(4,8,3,6),(0,5,1,7)],'blade',parent)
def shield(parent,colour='blue'):
 mesh('Kite shield',[(-.145,.05,.08),(.145,.05,.08),(.16,-.17,.08),(0,-.35,.13),(-.16,-.17,.08),(0,-.10,.17)],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5)],colour,parent)
 for a,b in [((-.145,.05,.08),(.145,.05,.08)),((.145,.05,.08),(.16,-.17,.08)),((.16,-.17,.08),(0,-.35,.13)),((0,-.35,.13),(-.16,-.17,.08)),((-.16,-.17,.08),(-.145,.05,.08))]:beam('Shield rim',a,b,.016,'brass',parent)
 cube('Shield heraldry',(0,-.10,.177),(.035,.21,.018),'cream',parent)

assets=[];manifest=[]
def asset(name):
 r=empty(name);empty('FrontMarker',(0,0,1),r);return r
def export(r):
 # Consolidate static surfaces per moving parent, with corner colours baked before joining.
 groups={}
 for o in list(r.children_recursive):
  if o.type!='MESH':continue
  attr=o.data.color_attributes.new(name='Color',type='BYTE_COLOR',domain='CORNER')
  for poly in o.data.polygons:
   base=o.data.materials[poly.material_index].diffuse_color
   fac=random.uniform(.965,1.025)
   for index in poly.loop_indices:attr.data[index].color=tuple(min(1,c*fac) for c in base[:3])+(1,)
  groups.setdefault(o.parent,[]).append(o)
 for parent,parts in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in parts:o.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=parent.name+'_Mesh'
  o.data.materials.clear()
  if 'WorldPalette' not in bpy.data.materials:
   m=bpy.data.materials.new('WorldPalette');m.use_nodes=True
   node=m.node_tree.nodes.new('ShaderNodeVertexColor');node.layer_name='Color';m.node_tree.links.new(node.outputs['Color'],m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
   m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.86
  o.data.materials.append(bpy.data.materials['WorldPalette'])
 if r.name in ('AxeTool','PickTool'):
  bpy.context.view_layer.update()
  height=.48 if r.name=='AxeTool' else .40
  origin=Vector((2,0,height));direction=Vector((-1,0,0));contact=None;nearest=float('inf')
  for o in r.children_recursive:
   if o.type!='MESH':continue
   inv=o.matrix_world.inverted()
   hit,point,_,_=o.ray_cast(inv@origin,(inv.to_3x3()@direction).normalized())
   if hit:
    world=o.matrix_world@point;distance=(world-origin).length
    if distance<nearest:nearest=distance;contact=world
  if contact is None:raise RuntimeError(r.name+' cutting surface not found')
  marker=empty('ImpactPoint',parent=r);marker.location=r.matrix_world.inverted()@contact
 bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
 for o in r.children_recursive:o.select_set(True)
 bpy.context.view_layer.objects.active=r
 bpy.ops.export_scene.fbx(filepath=str(OUT/(r.name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,colors_type='LINEAR')
 meshes=[o for o in r.children_recursive if o.type=='MESH']
 manifest.append(dict(name=r.name,meshes=len(meshes),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)))
 r.location=(len(assets)%8*2.3,len(assets)//8*2.8,0);assets.append(r)

for name in ['House','Market','Barracks','Research','Farm','LargeTest','Altar','AbandonedAltar','Ruins','ArrowTower','MagicTower','Wall']:
 r=asset(name)
 if name in ['House','Market','Barracks','Research']:
  shell(r)
  window(r,-.25,.67,.377);window(r,.25,.67,.377)
  if name=='House':
   cube('Stone chimney',(.25,1.2,-.18),(.17,.52,.19),'stone',r,.015)
   cube('Chimney lip',(.25,1.47,-.18),(.22,.07,.23),'slate',r)
   cube('Chimney soot',(.25,1.508,-.18),(.12,.006,.13),'dark',r)
   for x in [-.31,.31]:beam('Facade diagonal',(x,.24,.395),(x*.55,.51,.395),.018,'oak',r,n=4)
  if name=='Market':
   # Folded striped canopy, merchandise crates and a hanging balance sign.
   for i in range(6):
    mesh('Canvas stripe',[(-.45+i*.15,.87,.39),(-.30+i*.15,.87,.39),(-.30+i*.15,.69,.66),(-.45+i*.15,.69,.66)],[(0,1,2,3)],'teal' if i%2==0 else 'cream',r)
   for x in [-.43,.43]:beam('Canopy post',(x,.08,.62),(x,.73,.62),.025,'oak',r)
   cube('Display counter',(0,.24,.58),(.78,.30,.2),'oak',r)
   for i in range(6):ico('Produce',(-.30+i*.115,.41,.58),(.05,.06,.05),'gold' if i%2 else 'green',r)
   barrel(r,(-.42,.1,-.22),.13)
  if name=='Barracks':
   banner=empty('Banner',(.38,1.4,0),r);beam('Standard pole',(0,-.4,0),(0,.45,0),.021,'oak',banner)
   mesh('Military banner',[(0,.38,0),(.24,.38,0),(.24,.03,0),(.12,.09,0),(0,.03,0)],[(0,1,2,3,4)],'blue',banner)
   rack=empty('Arms rack',(-.47,.13,.1),r)
   for i in range(3):beam('Spear shaft',(0,0,i*.16),(0,1.0,i*.16),.016,'oak',rack);cone('Spear point',(0,1.08,i*.16),.045,0,.18,'iron',rack)
   plate=empty('Crossed swords',(0,1.16,.49),r);sword(plate);plate.scale=(.32,.32,.32)
  if name=='Research':
   cone('Observatory drum',(0,1.37,0),.26,.26,.23,'stone',r,12)
   ico('Copper observatory dome',(0,1.52,0),(.29,.22,.29),'teal',r,2)
   beam('Telescope',(0,1.56,.05),(.21,1.78,.32),.066,'brass',r,n=10)
   beam('Lens',(.21,1.78,.32),(.23,1.80,.35),.073,'glow',r,n=10)
   cube('Book lectern',(.43,.45,.31),(.22,.12,.24),'wood',r)
   book=cube('Open folio',(.43,.54,.31),(.22,.045,.20),'cream',r);book.rotation_euler.x=.3
 elif name in ['Farm','LargeTest']:
  width=1.86 if name=='LargeTest' else .92
  cube('Garden soil',(0,.035,0),(width,.07,.90),'dirt',r,.02)
  for x in [-width/2,width/2]:
   for z in [-.42,0,.42]:cube('Fence post',(x,.22,z),(.045,.43,.045),'oak',r)
   for y in [.16,.32]:beam('Fence rail',(x,y,-.44),(x,y,.44),.018,'wood',r,n=4)
  for z in [-.42,.42]:
   for y in [.16,.32]:beam('Fence crossrail',(-width/2,y,z),(width/2,y,z),.018,'wood',r,n=4)
  if name=='Farm':
   for x in [-.28,0,.28]:
    for z in [-.25,0,.25]:
     for dx in [-.06,.045]:beam('Wheat stalk',(x+dx,.05,z),(x+dx+.025,.34,z+.01),.009,'green',r,n=4);ico('Wheat ear',(x+dx+.025,.34,z+.01),(.045,.13,.034),'gold',r)
  else:
   barn=empty('Livestock shed',(-.48,0,-.17),r);shell(barn,.55,.48);barn.scale=(.8,.8,.8)
   cube('Water trough',(.48,.14,.24),(.46,.23,.24),'wood',r,.02);cube('Trough water',(.48,.263,.24),(.39,.012,.17),'teal',r)
   for x in [.28,.64]:ico('Wool sheep',(x,.23,-.13),(.16,.18,.24),'cream',r,2);ico('Sheep head',(x,.29,.06),(.075,.08,.09),'dark',r)
 elif name in ['Altar','AbandonedAltar','Ruins']:
  cone('Octagonal steps',(0,.07,0),.52,.52,.14,'slate',r)
  cone('Sanctuary platform',(0,.17,0),.44,.44,.08,'stone',r)
  if name=='Ruins':
   for side in [-1,1]:
    for i in range(4 if side==-1 else 3):cube('Masonry pier',(side*.32,.34+i*.19,0),(.23,.18,.27),'stone',r,.016)
   for i in range(5):
    a=math.pi*i/6+.3;cube('Broken arch stone',(.36*math.cos(a),.96+.21*math.sin(a),0),(.21,.18,.25),'stone',r,.02).rotation_euler.y=a-.9
   ico('Rune fragment',(0,.34,.08),(.13,.19,.1),'glow',r)
  else:
   cube('Carved plinth',(0,.43,0),(.43,.43,.42),'stone',r,.035)
   cube('Offering slab',(0,.69,0),(.70,.10,.59),'cream',r,.023)
   cone('Floating soul prism',(0,.95,0),.15,0,.39,'glow' if name=='Altar' else 'teal',r,5)
   for x in [-.25,.25]:cone('Candle',(x,.81,.18),.025,.025,.14,'cream',r);ico('Candle flame',(x,.91,.18),(.022,.055,.022),'gold',r)
   for side in [-1,1]:beam('Shrine shoulder',(side*.29,.22,-.18),(side*.39,.90,-.18),.065,'stone',r)
  if name!='Altar':
   for i in range(6):ico('Moss',(random.uniform(-.4,.4),.225,random.uniform(-.36,.36)),(.12,.025,.09),'green',r)
 elif name in ['ArrowTower','MagicTower']:
  cone('Broad foot',(0,.12,0),.47,.44,.24,'slate',r)
  cone('Tapered masonry',(0,1.01,0),.34,.28,1.6,'stone',r)
  for y in [.37,.79,1.22,1.68]:cone('Stone belt',(0,y,0),.355-y*.031,.355-y*.031,.064,'slate',r)
  for side in [-1,1]:cube('Arrow slit',(side*.30,1.13,.08),(.018,.30,.065),'dark',r)
  cone('Crown corbel',(0,1.87,0),.31,.46,.24,'stone',r)
  cone('Parapet walk',(0,2.04,0),.46,.46,.12,'slate',r)
  for i in range(8):
   a=i*math.pi/4;cube('Battlement',(math.sin(a)*.37,2.21,math.cos(a)*.37),(.17,.27,.17),'stone',r,.012)
  t=empty('Turret',(0,2.30,0),r)
  if name=='ArrowTower':
   cone('Swivel mount',(0,-.12,0),.14,.11,.24,'oak',t)
   cube('Ballista track',(0,0,.08),(.115,.09,.65),'wood',t)
   for side in [-1,1]:beam('Curved bow',(0,0,.26),(side*.36,0,.05),.025,'oak',t);beam('Taut string',(side*.36,0,.05),(0,.015,-.17),.009,'cream',t)
   beam('Bolt shaft',(0,.055,-.25),(0,.055,.46),.014,'oak',t)
   ico('Bolt head',(0,.055,.49),(.035,.027,.08),'iron',t)
  else:
   for i in range(3):
    a=i*math.pi*2/3;beam('Arc conductor',(math.cos(a)*.21,-.12,math.sin(a)*.21),(math.cos(a)*.25,.39,math.sin(a)*.25),.033,'brass',t)
   crystal=empty('Crystal',(0,.20,0),t);ico('Lightning heart',(0,0,0),(.18,.37,.18),'glow',crystal)
 elif name=='Wall':
  cube('Wall foundation',(0,.065,0),(.56,.13,.56),'slate',r,.025)
 export(r)

for name in ['WallPost','WallCorner','WallArm','HouseBridge','AltarPath']:
 r=asset(name)
 if name in ['WallPost','WallCorner']:
  if name=='WallCorner':
   cone('Round corner tower',(0,.66,0),.33,.30,1.32,'stone',r,12)
   for y in [.18,.55,.92,1.29]:cone('Corner masonry belt',(0,y,0),.34,.34,.045,'slate',r,12)
   cone('Corner crown',(0,1.35,0),.38,.38,.12,'stone',r,12)
   for i in range(6):a=i*math.pi/3;cube('Corner merlon',(math.cos(a)*.30,1.49,math.sin(a)*.30),(.15,.23,.15),'stone',r,.012)
  else:
   for row in range(5):cube('Pier course',(0,.14+row*.235,0),(.47,.22,.47),'stone' if row%2 else 'slate',r,.015)
   cube('Coping cap',(0,1.27,0),(.55,.16,.55),'stone',r,.018)
   for z in [-.18,.18]:cube('Post merlon',(0,1.42,z),(.32,.22,.16),'stone',r,.012)
 elif name=='WallArm':
  for row in range(5):
   for z in [.16,.42]:cube('Bonded wall stone',(0,.13+row*.22,z),(.40,.21,.25),'stone' if row%2 else 'slate',r,.009)
  cube('Coping',(0,1.18,.30),(.46,.13,.60),'stone',r,.012)
  cube('Merlon',(0,1.34,.46),(.26,.23,.18),'stone',r,.01)
 elif name=='HouseBridge':
  cube('Porch floor',(0,.12,.40),(.47,.12,.44),'oak',r)
  for x in [-.205,.205]:beam('Porch post',(x,.12,.50),(x,.79,.50),.025,'wood',r,n=4)
  cube('Porch shelter',(0,.84,.40),(.54,.08,.48),'roof',r,.015)
 else:
  for i in range(3):cube('Connected sacred paving',(0,.06,.20+i*.14),(.37,.10,.13),'stone',r,.018)
 export(r)

for typ in ['Tree','Rock']:
 for variant in range(4):
  r=asset(typ+str(variant))
  if typ=='Tree':
   bark='cream' if variant==2 else 'bark'
   beam('Tapered trunk',(0,.02,0),(.035,.96,0),.14,bark,r,.072,n=7)
   for i in range(4):
    a=i*math.pi/2;beam('Root flare',(0,.19,0),(math.cos(a)*.25,.015,math.sin(a)*.25),.085,bark,r,.022)
   if variant==1:
    for i in range(4):cone('Layered pine crown',(0,.65+i*.30,0),.53-i*.105,.018,.67,'pine' if i%2==0 else 'green',r,9)
   else:
    n=4 if variant==3 else 3
    for i in range(n):
     a=i*math.pi*2/n;end=(math.cos(a)*.29,.94+(.12 if i%2 else 0),math.sin(a)*.26)
     beam('Branch',(.01,.61,0),end,.063,bark,r,.025)
     ico('Faceted leaf crown',(end[0],end[1]+.20,end[2]),(.39,.45 if variant!=2 else .54,.38),'leaf' if i%2 else 'green',r,2)
    ico('Top crown',(0,1.45,0),(.34,.34,.32),'leaf',r,1)
   if variant==2:
    for i in range(5):cube('Birch bark scar',(.025,.22+i*.14,.10),(.12 if i%2 else .075,.035,.026),'slate',r)
   if variant==3:ico('Trunk knot',(0,.52,.123),(.04,.06,.012),'wood',r)
  else:
   if variant==1:
    for i in range(4):cube('Layered slate',((i%2)*.04,.10+i*.10,0),(.72-i*.1,.14,.58-i*.05),'slate' if i%2 else 'stone',r,.05).rotation_euler.z=i*.18
   else:
    ico('Faceted boulder',(0,.26,0),(.40,.31,.35),'stone' if variant!=3 else 'plaster',r,1)
    ico('Broken side stone',(.29,.10,.15),(.17,.14,.20),'slate',r,1)
    if variant==2:
     for i in range(4):cone('Quartz crystal',((i-1.5)*.13,.40+(i%2)*.09,0),.065,0,.39+(i%2)*.15,'glow' if i%2 else 'blade',r,5)
    elif variant==0:ico('Moss patch',(-.16,.49,-.09),(.17,.028,.13),'green',r)
    else:
     for i in range(5):ico('Granite mica',(random.uniform(-.21,.21),.49,random.uniform(-.14,.14)),(.035,.018,.03),'slate',r)
  export(r)

for grade in range(3):
 r=asset('Chest'+str(grade));wood=['oak','blue','purple'][grade];metal=['iron','glow','brass'][grade]
 cube('Bottom',(0,.07,0),(.78,.12,.49),'wood',r,.025)
 # Real empty interior, rim and four walls. Lid hinge is at the rear edge.
 cube('Dark interior',(0,.14,0),(.67,.025,.37),'dark',r)
 for z in [-.22,.22]:cube('Chest plank wall',(0,.25,z),(.76,.29,.055),wood,r,.012)
 for x in [-.365,.365]:cube('End panel',(x,.25,0),(.065,.29,.43),wood,r,.012)
 for x in [-.25,.25]:
  for z in [-.253,.253]:cube('Body strap',(x,.25,z),(.055,.31,.02),metal,r)
  for z in [-.253,.253]:
   for y in [.16,.34]:ico('Strap rivet',(x,y,z*1.045),(.012,.012,.01),'brass',r)
 lid=empty('Lid',(0,.395,-.22),r)
 # Barrel-vault lid, seam at local y=0, curved top at y=.18.
 verts=[]
 for x in [-.39,.39]:
  for i in range(9):
   a=math.pi*i/8;verts.append((x,math.sin(a)*.20,.22-math.cos(a)*.24))
 faces=[tuple(range(8,-1,-1)),tuple(range(9,18))]+[(i,i+1,i+10,i+9) for i in range(8)]+[(0,9,17,8)]
 mesh('Vaulted wooden lid',verts,faces,wood,lid)
 for x in [-.25,.25]:
  for i in range(8):
   a=math.pi*i/8;b=math.pi*(i+1)/8
   beam('Lid iron curve',(x,math.sin(a)*.211,.22-math.cos(a)*.249),(x,math.sin(b)*.211,.22-math.cos(b)*.249),.022,metal,lid,n=4)
 cube('Lid clasp',(0,-.025,.466),(.10,.145,.035),metal,lid,.009)
 for x in [-.25,.25]:beam('Hinge pin',(x-.05,.005,0),(x+.05,.005,0),.03,metal,lid)
 if grade:ico('Grade jewel',(0,.215,.22),(.075,.045,.075),metal,lid)
 treasure=empty('ChestTreasure',(0,.18,0),r)
 for i in range(9):cone('Gold coin',((i%3-1)*.16,.02+(i%2)*.02,(i//3-1)*.11),.072,.072,.025,'gold',treasure,8)
 ico('Reward gem',(.10,.065,0),(.05,.075,.055),'glow',treasure)
 export(r)

for name in ['Warrior','Archer','Mage','Knight','Goblin','GoblinWarrior','GoblinShaman','Orc','Lizard']:
 r=asset(name);visual=empty('Visual',parent=r)
 hostile=name in ['Goblin','GoblinWarrior','GoblinShaman','Orc','Lizard'];gob='Goblin' in name;orc=name=='Orc';caster=name in ['Mage','GoblinShaman'];archer=name in ['Archer','Lizard']
 skin='orc' if orc else 'goblin' if gob else 'lizard' if name=='Lizard' else 'skin'
 cloth='purple' if caster else 'green' if archer else 'wood' if hostile else 'blue'
 width=.40 if orc else .29
 ico('Tunic torso',(0,.57,0),(width*.57,.25,.16),cloth,visual,1)
 cube('Belt',(0,.43,.01),(width+.035,.067,.28),'wood',visual,.01);cube('Buckle',(0,.43,.157),(.065,.05,.02),'brass',visual)
 if caster:
  cone('Flared robe',(0,.33,0),.23,.15,.42,cloth,visual,8)
  for side in [-1,1]:beam('Robe embroidery',(side*.10,.64,.147),(side*.16,.14,.19),.012,'brass',visual,n=4)
 head=empty('Head',(0,.87,0),visual)
 ico('Face',(0,0,0),(.205 if orc else .17,.17,.15),skin,head,2)
 ico('Nose',(0,-.015,.153),(.038,.05,.045),skin,head)
 if name=='Lizard':ico('Long muzzle',(0,-.065,.16),(.12,.07,.17),skin,head);ico('Lower jaw',(0,-.11,.15),(.115,.025,.14),'cream',head)
 for side in [-1,1]:
  cube('Eye',(side*.07,.025,.143),(.039,.038,.018),'gold' if hostile else 'dark',head)
  if gob:mesh('Long pointed ear',[(side*.12,.035,0),(side*.33,.08,-.04),(side*.17,-.055,.005),(side*.17,.018,.04)],[(0,1,3),(1,2,3),(2,0,3)],skin,head)
  if orc:cone('Tusk',(side*.1,-.085,.17),.032,0,.12,'cream',head,5)
 if name in ['Warrior','Knight','GoblinWarrior']:
  ico('Helmet dome',(0,.073,-.012),(.186,.145,.168),'iron' if name!='GoblinWarrior' else 'slate',head,2)
  cube('Helmet brow',(0,.012,.154),(.32,.047,.032),'iron',head)
  for side in [-1,1]:cube('Cheek guard',(side*.14,-.065,.085),(.05,.12,.1),'iron',head,.012)
  if name=='Knight':
   cube('Visor',(0,-.031,.175),(.21,.09,.037),'slate',head,.01);cube('Visor slit',(0,-.01,.198),(.18,.018,.006),'dark',head)
   for i in range(3):ico('Plumed crest',(0,.23,-.09+i*.075),(.054,.08,.057),'teal',head)
   cube('Breastplate',(0,.60,.13),(.29,.25,.085),'iron',visual,.025)
 elif caster:
  if name=='GoblinShaman':
   ico('Bone skull crown',(0,.145,0),(.19,.12,.13),'cream',head)
   for side in [-1,1]:
    beam('Ritual antler',(side*.14,.20,-.03),(side*.25,.39,-.06),.027,'cream',head,.008)
    beam('Antler tine',(side*.21,.31,-.05),(side*.31,.32,.03),.019,'cream',head,.004)
   cube('Skull nose',(0,.08,.142),(.035,.05,.02),'dark',head)
  else:
   cone('Broad hat brim',(0,.125,0),.235,.23,.035,cloth,head,10)
   cone('Bent wizard hat',(0,.295,-.025),.17,.023,.35,cloth,head,7)
   cone('Hat band',(0,.16,0),.166,.15,.05,'brass',head)
 elif archer:
  ico('Leather hood',(0,.044,-.034),(.185,.19,.154),'green',head,2)
  # Face remains forward of hood.
 elif not hostile:ico('Hair',(0,.10,-.03),(.17,.10,.14),'wood',head)
 for side,label in [(-1,'Right'),(1,'Left')]:
  leg=empty(label+'Leg',(side*.092,.36,0),visual)
  beam('Trouser',(0,0,0),(0,-.19,0),.063,cloth,leg,.052)
  cube('Boot',(0,-.27,.045),(.135,.16,.21),'wood' if not hostile else skin,leg,.022)
  shoulder=empty(label+'Arm',(side*(width*.5+.055),.70,0),visual)
  ico('Shoulder pad',(0,-.025,0),(.09,.10,.10),'iron' if name in ['Knight','Warrior','GoblinWarrior'] else cloth,shoulder)
  beam('Sleeve',(0,-.06,0),(0,-.22,.015),.058,cloth,shoulder,.043)
  ico('Hand',(0,-.24,.02),(.052,.063,.05),skin,shoulder)
  if label=='Right':grip=empty('Grip',(0,-.24,.04),shoulder)
  elif name in ['Warrior','Knight','GoblinWarrior']:shield(shoulder,'wood' if hostile else 'blue')
 if caster:
  beam('Crooked staff',(0,-.12,0),(0,.71,0),.025,'oak',grip)
  for side in [-1,1]:beam('Focus claw',(0,.55,0),(side*.10,.76,0),.018,'brass',grip)
  ico('Staff soul',(0,.77,0),(.075,.11,.065),'glow' if not hostile else 'gold',grip)
 elif archer:
  for side in [-1,1]:beam('Bow limb',(0,0,.13),(0,side*.21,.06),.02,'oak',grip);beam('Bow tip',(0,side*.21,.06),(0,side*.32,-.05),.015,'oak',grip)
  beam('Bowstring',(0,-.32,-.05),(0,.32,-.05),.005,'cream',grip)
  quiver=empty('Quiver',(.09,.59,-.17),visual);cone('Quiver leather',(0,0,0),.07,.08,.33,'oak',quiver)
  for i in range(3):beam('Arrow',(i*.035-.035,0,0),(i*.035-.035,.29,0),.009,'cut',quiver);ico('Fletching',(i*.035-.035,.26,0),(.028,.055,.02),'cream',quiver)
 elif name in ['Goblin','Orc']:
  beam('Club haft',(0,0,0),(0,.49,0),.035,'oak',grip,.06)
  ico('Club head',(0,.51,0),(.11,.17,.10),'wood',grip,1)
  if orc:
   for i in range(4):a=i*math.pi/2;beam('Iron spike',(math.cos(a)*.07,.52,math.sin(a)*.07),(math.cos(a)*.18,.54,math.sin(a)*.18),.035,'iron',grip,0)
 else:sword(grip)
 if name=='Lizard':
  tail=empty('Tail',(0,.38,-.10),visual)
  for i in range(4):beam('Tail taper',(0,-i*.045,-i*.14),(0,-(i+1)*.045,-(i+1)*.14),.09-i*.019,skin,tail,.075-i*.019)
  for i in range(3):cone('Dorsal spike',(0,.45-i*.1,-.14-i*.14),.06,0,.15,'gold',visual,4)
 # Export proportions, not runtime scale guesses.
 visual.scale=(1.13,1.13,1.13) if orc else (.86,.86,.86) if gob else (1,1,1)
 export(r)

r=asset('Slime');visual=empty('Visual',parent=r)
ico('Jelly body',(0,.31,0),(.39,.34,.34),'leaf',visual,2)
ico('Gel crown',(-.08,.58,-.04),(.16,.12,.14),'green',visual,1)
for side in [-1,1]:
 ico('Eye',(side*.115,.37,.291),(.042,.06,.025),'dark',visual,2)
 ico('Eye glint',(side*.115-.008,.39,.313),(.012,.017,.009),'cream',visual)
ico('Smile',(0,.24,.318),(.056,.021,.01),'dark',visual)
ico('Jelly shine',(-.20,.50,.18),(.07,.027,.043),'cream',visual)
export(r)
r=asset('Treasure');cone('Purse',(0,.16,0),.13,.09,.25,'oak',r);cone('Purse tie',(0,.30,0),.07,.07,.04,'brass',r)
for i in range(3):cone('Loose coin',(.09+i*.045,.025,.1-i*.04),.048,.048,.015,'gold',r,8)
export(r)

for name in ['AxeTool','PickTool','HammerTool','SwordTool','HandTool']:
 r=asset(name)
 if name=='SwordTool':sword(r)
 elif name=='HandTool':
  cube('Palm',(0,0,0),(.22,.09,.19),'skin',r,.018)
  for i in range(4):
   cube('Finger knuckle',(-.083+i*.055,0,.13),(.046,.08,.12),'skin',r,.015)
   cube('Finger tip',(-.083+i*.055,-.005,.205),(.043,.073,.08),'skin',r,.012)
  cube('Thumb',(.14,-.015,.03),(.12,.075,.065),'skin',r,.016)
 else:
  beam('Wooden handle',(0,0,0),(0,.51,0),.029,'oak',r,.035,n=8)
  for y in [.04,.08,.12]:cone('Leather grip wrap',(0,y,0),.034,.034,.022,'wood',r)
  if name=='AxeTool':
   mesh('Forged axe head',[(-.07,.56,-.035),(.20,.61,-.035),(.30,.62,-.022),(.32,.34,-.022),(.19,.37,-.035),(-.07,.42,-.035),(-.07,.56,.035),(.20,.61,.035),(.30,.62,.022),(.32,.34,.022),(.19,.37,.035),(-.07,.42,.035)],[(0,1,2,3,4,5),(6,11,10,9,8,7),(0,6,7,1),(1,7,8,2),(2,8,9,3),(3,9,10,4),(4,10,11,5),(5,11,6,0)],'iron',r)
   beam('Honed edge',(.31,.35,0),(.29,.61,0),.018,'blade',r,n=4)
  elif name=='PickTool':
   beam('Pick left horn',(0,.50,0),(-.22,.42,0),.044,'iron',r,.008)
   beam('Pick sharp end',(0,.50,0),(.265,.40,0),.045,'blade',r,.007)
   cube('Pick eye',(0,.5,0),(.12,.12,.12),'iron',r,.018)
  else:
   cube('Hammer head',(0,.5,0),(.34,.21,.19),'iron',r,.025)
   for x in [-.17,.17]:cube('Hammer face',(x,.50,0),(.034,.20,.18),'blade',r,.009)
 export(r)

for name in ['Fern','Reeds','Pebbles','FallenLog','Flowers']:
 r=asset(name)
 if name=='Fern':
  for i in range(7):
   a=i*math.pi*2/7;dx,dz=math.cos(a),math.sin(a)
   beam('Fern stem',(0,0,0),(dx*.28,.19,dz*.28),.007,'green',r,n=4)
   for j in range(1,4):
    t=j/4;cx,cz=dx*.28*t,dz*.28*t
    for side in [-1,1]:mesh('Fern frond',[(cx,.20*t,cz),(cx-dz*.09*side,.20*t+.018,cz+dx*.09*side),(cx+dx*.075,.20*t+.045,cz+dz*.075)],[(0,1,2)],'leaf',r)
 elif name=='Reeds':
  for i in range(5):
   x,z=random.uniform(-.14,.14),random.uniform(-.14,.14);h=random.uniform(.28,.52)
   beam('Reed stalk',(x,0,z),(x+.025,h,z),.009,'green',r,n=4)
   beam('Cattail',(x+.025,h-.06,z),(x+.025,h+.07,z),.025,'wood',r,n=6)
 elif name=='Pebbles':
  for i in range(5):ico('Pebble',(random.uniform(-.25,.25),.028,random.uniform(-.2,.2)),(.05+i*.009,.035,.05),'stone' if i%2 else 'slate',r)
 elif name=='FallenLog':
  beam('Fallen bark',(-.34,.12,0),(.34,.12,0),.12,'bark',r,n=9)
  beam('Cut end',(.338,.12,0),(.35,.12,0),.104,'cut',r,n=9)
  beam('Heartwood ring',(.350,.12,0),(.353,.12,0),.054,'oak',r,n=8)
  beam('Snapped branch',(-.13,.18,0),(-.13,.32,.11),.038,'bark',r,.025)
 elif name=='Flowers':
  for i in range(4):
   x,z=random.uniform(-.17,.17),random.uniform(-.17,.17)
   beam('Flower stem',(x,0,z),(x,.16,z),.007,'green',r,n=4)
   ico('Wildflower',(x,.17,z),(.033,.016,.032),'cream' if i%2 else 'gold',r)
 export(r)

# Preview actions are stored in the .blend as editable object animation examples;
# game poses use the same named pivots and model-controlled impact times.
for r in assets:
 lid=next((o for o in r.children_recursive if o.name.split('.')[0]=='Lid'),None)
 if lid:
  for frame,angle in [(1,0),(7,.10),(20,-1.83),(28,-1.70)]:lid.rotation_euler.x=angle;lid.keyframe_insert('rotation_euler',frame=frame)
  lid.rotation_euler.x=0
 parts={o.name.split('.')[0]:o for o in r.children_recursive if o.type=='EMPTY'}
 if 'LeftLeg' in parts:
  for label,sign in [('LeftLeg',1),('RightLeg',-1),('LeftArm',-.5),('RightArm',.5)]:
   joint=parts[label]
   for frame,angle in [(1,0),(7,30),(13,0),(19,-30),(25,0)]:joint.rotation_euler.x=math.radians(angle*sign);joint.keyframe_insert('rotation_euler',frame=frame)
  for frame,angle in [(30,0),(35,-145),(38,-70),(45,0)]:parts['RightArm'].rotation_euler.x=math.radians(angle);parts['RightArm'].keyframe_insert('rotation_euler',frame=frame)
  for frame,angle in [(30,0),(35,115),(38,150),(45,0)]:parts['Grip'].rotation_euler.x=math.radians(angle);parts['Grip'].keyframe_insert('rotation_euler',frame=frame)
 if r.name=='Slime':
  visual=parts['Visual']
  for frame,z,stretch in [(1,0,1),(5,0,.77),(9,.19,1.12),(14,.26,1.06),(20,0,.8),(25,0,1)]:
   visual.location.z=z;visual.scale=(1+(1-stretch)*.65,1+(1-stretch)*.65,stretch);visual.keyframe_insert('location',frame=frame);visual.keyframe_insert('scale',frame=frame)
scene=bpy.context.scene;scene.frame_set(1)
bpy.ops.object.camera_add(location=(15,-19,24));cam=bpy.context.object;cam.rotation_euler=(Vector((7,7,.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=26;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(3,-7,18));bpy.context.object.data.energy=2600;bpy.context.object.data.size=12
scene.world.color=(.16,.18,.21);scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(SRC/'world-catalog.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'AfterlifeWorld.blend'))
(SRC/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.render.render(write_still=True)
print('WORLD_ASSETS_COMPLETE',len(manifest))
