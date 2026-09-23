using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Prototype
{
    // Moonstone이 순수 모델과 설정을 주입한다. 이 클래스는 입력/표현만 담당한다.
    public sealed partial class PrototypeView : MonoBehaviour
    {
        [Moonstone.Arc.DependencyInjection.Inject] private VillageModel _model = null; // Moonstone ServiceResolver 주입
        [Moonstone.Arc.DependencyInjection.Inject] private PrototypeSettings _settings = null;
        private Camera _camera;
        private PrototypeAudio _audio;
        private void PlaySound(SoundCue cue) { if(_audio!=null)_audio.RequestSound(cue); }
        private RenderTexture _pixels;
        private readonly Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<int, Mesh> _roofs = new Dictionary<int, Mesh>();
        private Material _surface;
        private LineRenderer _workRing, _hoverRing, _preview, _rally, _towerRange;
        private Font _font;
        private bool _ready, _pointerValid;
        private float _simulationTime;
        private int _hovered, _inspected, _screenWidth, _screenHeight;
        private Point _pointer;
        private string _notice = "대상을 클릭해 신력 필드를 설치하세요. 빈 타일은 Shift+클릭으로 설치합니다.";
        private readonly Color _cream = new Color(.93f,.88f,.72f);
        private readonly Color _cyan = new Color(.34f,.96f,.85f);
        private readonly Color _gold = new Color(1,.78f,.25f);
        private MaterialPropertyBlock _block;
        private Afterlife.Devlog.TerrainView.TerrainSurface _terrainSurface;
        private Afterlife.Devlog.Water.WaterSurface _waterSurface;
        private float GroundLift(float x,float z) => _terrainSurface==null ? 0 : _terrainSurface.Height(x,z)-Afterlife.Devlog.TerrainView.TerrainSurface.BaseHeight;
        private Vector3 GroundPosition(Point p,float y=0) => V(p,y+GroundLift(p.X,p.Z));
        private static Vector3 V(Point p,float y=0) => new Vector3(p.X,y,p.Z);

        public void SetUp()
        {
            if (_model == null || _settings == null) { Debug.LogError("Prototype injection failed"); return; }
            var shader = Shader.Find("Afterlife/Prototype/PixelSurface");
            if(shader == null) shader=Shader.Find("Universal Render Pipeline/Unlit");
            var savedSurface=Resources.Load<Material>("AfterlifePrototype/Surface");
            _surface = savedSurface!=null ? new Material(savedSurface) : new Material(shader); _materials.Add(_surface);
            _block = new MaterialPropertyBlock();
            _font = Resources.Load<Font>("Fonts/Mona12TextKR");
            if(_font==null||!_font.HasCharacter('가')||!_font.HasCharacter('0'))_font = Font.CreateDynamicFontFromOSFont(new[] {"Malgun Gothic", "Arial"}, 18);
            var cameraObject=new GameObject("Prototype Camera"); cameraObject.transform.SetParent(transform);
            _camera=cameraObject.AddComponent<Camera>(); _camera.orthographic=true; _camera.orthographicSize=8.7f;
            _camera.transform.position=new Vector3(10,15,-14);
            _camera.transform.LookAt(new Vector3(0,0,0));
            _camera.clearFlags=CameraClearFlags.SolidColor; _camera.backgroundColor=new Color(.075f,.115f,.12f);
            _camera.nearClipPlane=.1f; _camera.farClipPlane=80; _camera.allowHDR=false; _camera.allowMSAA=false;
            ApplyCameraGroundDistance();
            var cameraData=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.requiresDepthTexture=true;cameraData.requiresColorTexture=true;
            _audio=gameObject.AddComponent<PrototypeAudio>();_audio.Initialize(_model,_settings,_camera);
            BuildGround(); InitializeInteraction(); InitializePresentation();
            _workRing=Line("WorkRange",_cyan,.055f);
            _towerRange=Line("Tower attack range",new Color(.45f,.75f,1,.6f),.05f);
            _hoverRing=Line("HoverFocus",_gold,.045f);
            _preview=Line("PlacementPreview",Color.green,.065f);
            _rally=Line("RallyPoint",new Color(.4f,.65f,1),.04f);
            _model.ArrowRayBlocked=ArrowBlocked;_model.Notice+=OnNotice; _model.Contributed+=OnContributed; _model.Damaged+=OnDamaged;_model.ContributionText+=OnContributionText;_model.HarvestGold+=OnHarvestGold;_model.RunEnded+=OnRunEnded;
            _model.BannerUnlocked=PlayerPrefs.GetInt("AfterlifePrototype.Unlock.TealBanner.v1",0)==1;
            Resize(); InitializeSceneUi(); _ready=true; UpdateFog(); Sync(); InitializeGameFlow();
        }
        private void OnNotice(string message) { _notice=message;_noticeTime=Time.unscaledTime; }
        private GameObject _mapRoot;
        private int _mapRevision;
        private void BuildGround()
        {
            _mapRevision=_model.Terrain.Revision;
            _mapRoot=new GameObject("GeneratedMap");_mapRoot.transform.SetParent(transform,false);
            var palette=new[]{new Color(.30f,.40f,.23f),new Color(.43f,.34f,.25f),new Color(.68f,.61f,.44f),
                new Color(.055f,.25f,.32f),new Color(.81f,.85f,.88f),new Color(.23f,.32f,.19f),new Color(.43f,.45f,.43f)};
            var coast=new Afterlife.Devlog.Terrain.CoastalTerrain(Afterlife.Devlog.Terrain.CoastShape.Rounded,-.46f,-.82f);
            var waterShader=Resources.Load<Shader>("PrototypeWater");
            _terrainSurface=new Afterlife.Devlog.TerrainView.TerrainSurface(_mapRoot.transform,_model.Terrain.Radius,
                (x,z)=>(Afterlife.Devlog.Terrain.ShowcaseBiome)(int)_model.Terrain.At(x,z),_surface,palette,coast);
            _waterSurface=new Afterlife.Devlog.Water.WaterSurface(_mapRoot.transform,_model.Terrain.Radius,
                (x,z)=>_model.Terrain.At(x,z)==TerrainKind.Water,waterShader,coast.WaterHeight,coast.FloorHeight);
            InitializeGroundAppearance();
        }
        private GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            var renderer=go.GetComponent<Renderer>(); renderer.sharedMaterial=_surface; Tint(renderer,color);
            if(!collider) { var c=go.GetComponent<Collider>(); c.enabled=false; Destroy(c); }
            return go;
        }
        private void Tint(Renderer renderer,Color color)
        { NotifyVegetationTint(renderer,color);renderer.GetPropertyBlock(_block); _block.SetColor("_BaseColor",color); if(renderer is LineRenderer)_block.SetFloat("_Overlay",1); renderer.SetPropertyBlock(_block); }
        private GameObject CreateBody(Body body)
        {
            var root=new GameObject(body.Kind + "." + body.Id); root.transform.SetParent(transform,false);
            root.AddComponent<PrototypePick>().Id=body.Id;
            Color baseColor=ColorFor(body.Kind);
            if(BuildAuthoredBody(root,body)) { }
            else if(body.Kind==Kind.ArrowTower||body.Kind==Kind.MagicTower||body.Kind==Kind.Wall||body.Kind==Kind.Chest||body.Kind==Kind.Altar||body.Kind==Kind.AbandonedAltar||body.Kind==Kind.Ruins)BuildExpansion(root.transform,body);
            else if(body.Building)
            {
                Part(root.transform,"Foundation",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(body.Width*.92f,.2f,body.Depth*.92f),new Color(.32f,.34f,.3f));
                if(body.Kind==Kind.Farm)
                {
                    for(int row=0;row<3;row++)
                        Part(root.transform,"Wheat",PrimitiveType.Cube,new Vector3(-.28f+row*.28f,.26f,0),new Vector3(.13f,.3f,.7f),baseColor);
                }
                else
                {
                    Part(root.transform,"Walls",PrimitiveType.Cube,new Vector3(0,.55f,0),new Vector3(body.Width*.77f,.85f,.77f),baseColor);
                    var roof=new GameObject("Roof"); roof.transform.SetParent(root.transform,false);
                    if(!_roofs.TryGetValue(body.Width,out var roofMesh)) { roofMesh=Roof(body.Width*.95f,.95f); _roofs.Add(body.Width,roofMesh); }
                    roof.AddComponent<MeshFilter>().sharedMesh=roofMesh;
                    var renderer=roof.AddComponent<MeshRenderer>(); renderer.sharedMaterial=_surface; Tint(renderer,body.Kind==Kind.Market?new Color(.2f,.56f,.5f):new Color(.52f,.24f,.18f));
                    Part(root.transform,"Door",PrimitiveType.Cube,new Vector3(0,.35f,-.4f),new Vector3(.22f,.45f,.04f),new Color(.2f,.16f,.13f),false);
                    if(body.Kind==Kind.Barracks) Part(root.transform,"Banner",PrimitiveType.Cube,new Vector3(.3f,1.35f,0),new Vector3(.26f,.38f,.08f),new Color(.3f,.55f,.85f),false);
                }
            }
            else if(body.Kind==Kind.Tree||body.Kind==Kind.Rock)BuildResource(root.transform,body);
            else if(body.Friendly||body.Hostile&&body.Kind!=Kind.Slime)BuildFighter(root,body);
            else if(body.Kind==Kind.Animal)BuildAnimal(root,body);
            else if(body.Kind==Kind.Treasure)
            {
                var gem=Part(root.transform,"Valuable",PrimitiveType.Cube,new Vector3(0,.3f,0),new Vector3(.3f,.3f,.3f),new Color(.95f,.76f,.24f));
                gem.transform.localRotation=Quaternion.Euler(15,45,30);
            }
            else
            {
                Part(root.transform,"Slime",PrimitiveType.Sphere,new Vector3(0,.28f,0),new Vector3(.6f,.48f,.55f),baseColor);
                Part(root.transform,"Eye",PrimitiveType.Cube,new Vector3(-.11f,.36f,.25f),new Vector3(.065f,.07f,.04f),Color.black,false);
                Part(root.transform,"Eye",PrimitiveType.Cube,new Vector3(.11f,.36f,.25f),new Vector3(.065f,.07f,.04f),Color.black,false);
            }
            if(body.Kind==Kind.House)
            {
                if(_model.BannerUnlocked)
                {
                    Part(root.transform,"Unlocked banner pole",PrimitiveType.Cube,new Vector3(.34f,1.3f,0),new Vector3(.04f,.75f,.04f),new Color(.4f,.3f,.2f),false);
                    Part(root.transform,"Unlocked teal banner",PrimitiveType.Cube,new Vector3(.47f,1.5f,0),new Vector3(.25f,.3f,.035f),_cyan,false);
                }
                Part(root.transform,"Lamp",PrimitiveType.Cube,new Vector3(.26f,.65f,-.42f),new Vector3(.16f,.18f,.04f),new Color(1,.72f,.24f),false);
                var lamp=new GameObject("House Light");lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(0,1.1f,-.5f);
                var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.65f,.25f);light.range=1.8f;light.intensity=0;light.shadows=LightShadows.None;
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(_block);
                renderer.gameObject.AddComponent<PrototypeTint>().Original=_block.GetColor("_BaseColor");
            }
            return root;
        }
        private static Mesh Roof(float width,float depth)
        {
            float x=width/2,z=depth/2;
            var mesh=new Mesh { name="Prototype Roof" };
            mesh.vertices=new[] {new Vector3(-x,1,-z),new Vector3(x,1,-z),new Vector3(0,1.5f,-z),new Vector3(-x,1,z),new Vector3(x,1,z),new Vector3(0,1.5f,z)};
            mesh.triangles=new[] {0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};
            mesh.RecalculateNormals(); return mesh;
        }
        private static Color ColorFor(Kind kind)
        {
            switch(kind)
            {
                case Kind.Tree:return new Color(.24f,.48f,.27f); case Kind.Rock:return new Color(.52f,.57f,.57f);
                case Kind.Warrior:return new Color(.3f,.5f,.76f); case Kind.Slime:return new Color(.61f,.8f,.26f);
                case Kind.Farm:return new Color(.82f,.65f,.25f); case Kind.Market:return new Color(.65f,.62f,.4f);
                default:return new Color(.76f,.65f,.46f);
            }
        }
        private LineRenderer Line(string name,Color color,float width)
        {
            var go=new GameObject(name); go.transform.SetParent(transform);
            var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=_surface;
            line.useWorldSpace=true; line.loop=true; line.widthMultiplier=width;
            Tint(line,color); line.enabled=false; return line;
        }
        private void Circle(LineRenderer line,Point center,float radius)
        {
            line.enabled=true; line.positionCount=80;
            for(int i=0;i<80;i++) { float a=i*Mathf.PI*2/80; var p=new Point(center.X+Mathf.Cos(a)*radius,center.Z+Mathf.Sin(a)*radius); line.SetPosition(i,GroundPosition(p,.09f)); }
        }
        private void Box(LineRenderer line,Point p,float width,float depth)
        {
            line.enabled=true;
            int nx=Mathf.Max(1,Mathf.CeilToInt(width*4)),nz=Mathf.Max(1,Mathf.CeilToInt(depth*4));
            line.positionCount=2*(nx+nz);int index=0;
            for(int side=0;side<4;side++)
            {
                int count=side%2==0?nz:nx;
                for(int i=0;i<count;i++)
                {
                    float t=i/(float)count;
                    float x=p.X+(side==0?-.5f:side==1?t-.5f:side==2?.5f:.5f-t)*width;
                    float z=p.Z+(side==0?t-.5f:side==1?.5f:side==2?.5f-t:-.5f)*depth;
                    line.SetPosition(index++,GroundPosition(new Point(x,z),.08f));
                }
            }
        }
        private void Resize()
        {
            if(_screenWidth==Screen.width&&_screenHeight==Screen.height) return;
            _screenWidth=Screen.width; _screenHeight=Screen.height;
            if(_pixels!=null) { _camera.targetTexture=null; _pixels.Release(); Destroy(_pixels); }
            int h=Mathf.Clamp(_settings.PixelHeight,180,720), w=Mathf.RoundToInt(h*(float)Screen.width/Mathf.Max(1,Screen.height));
            _pixels=new RenderTexture(Mathf.Max(1,w),h,24,RenderTextureFormat.DefaultHDR) {filterMode=FilterMode.Point,antiAliasing=1,name="Prototype Point Pixels"};
            _pixels.Create(); _camera.targetTexture=_pixels; _camera.aspect=(float)w/h;
        }
    }
    public sealed class PrototypeWeapon : MonoBehaviour { public Transform Grip,LeftLeg,RightLeg,LeftArm,RightArm,Visual; public float WalkCycle,Motion; }
    public sealed class PrototypePick : MonoBehaviour { public int Id; }
    public sealed class PrototypeTint : MonoBehaviour { public Color Original; }
}
