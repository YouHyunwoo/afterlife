using System;
using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using UnityEngine;

namespace Afterlife.Devlog.Hover
{
    public enum GrassHoverMode { Radial, Sway }

    public sealed class ShowcaseTileHover : IDisposable
    {
        public sealed class Map
        {
            public int MinX=-6, MinZ=-5, Width=12, Depth=10;
            public Vector3 OriginOffset;
            public Func<int,int,ShowcaseBiome> Biome=ShowcaseBiomeMap.At;
            public Shader DustShader;
        }
        private readonly Map _map;
        public bool HasTerrainStamps => _terrainCount>0;
        private readonly Dictionary<string,Vector4> _previousVectors=new Dictionary<string,Vector4>();
        private readonly Dictionary<string,int> _previousCounts=new Dictionary<string,int>();
        private readonly Dictionary<string,Vector4[]> _previousArrays=new Dictionary<string,Vector4[]>();
        private readonly Dictionary<string,float[]> _previousWeights=new Dictionary<string,float[]>();
        private readonly Texture _previousBiomes;
        private readonly float _previousTime;

        public void SetPaused(bool paused)
        {
            if (_dust==null) return;
            if (paused) _dust.Pause(); else if (_dust.isPaused) _dust.Play();
        }
        public GrassHoverMode GrassMode { get; set; }
        public event Action WaterRippleCreated;
        private readonly Vector4[] _stamps=new Vector4[128];
        private readonly float[] _weights=new float[128];
        private readonly Vector4[] _grassStamps=new Vector4[96],_terrainStamps=new Vector4[96];
        private readonly float[] _terrainWeights=new float[96];
        private int _terrainCount;
        // xy: 중심, z: 전파 반경, w: 높이 감쇠. 살아 있는 물결만 GPU에 전달한다.
        private readonly Vector4[] _waves=new Vector4[32];
        private readonly float[] _ringFades=new float[32];
        private bool _pointerMoved, _waterActive;
        private float _lastWaterMotion=-10, _waterDistance;
        private Vector3 _lastWaterPoint;
        private int _lastKind=-1;
        private const float RippleSpacing=.45f;
        private readonly Vector3 _origin;
        private readonly Texture2D _biomes;
        private readonly ParticleSystem _dust;
        private readonly Material _dustMaterial;
        private int _next;
        private float _time,_lastDust=-10,_strength;
        private Vector3? _pointer;
        private Vector3 _last,_grassPoint;
        private ShowcaseBiome _biome;
        private bool _hasLast;
        public ShowcaseTileHover(Transform stage, Map map=null)
        {
            _previousBiomes=Shader.GetGlobalTexture("_DevlogHoverBiomes");
            _previousTime=Shader.GetGlobalFloat("_DevlogHoverTime");
            foreach(var name in new[]{"_DevlogHoverOrigin","_DevlogHoverBounds","_DevlogGrassCursor"})_previousVectors[name]=Shader.GetGlobalVector(name);
            foreach(var name in new[]{"_DevlogGrassStampCountV3","_DevlogTerrainStampCountV3","_DevlogWaveCount"})_previousCounts[name]=Shader.GetGlobalInt(name);
            foreach(var name in new[]{"_DevlogGrassStampsV3","_DevlogTerrainStampsV3","_DevlogWaves"})_previousArrays[name]=Shader.GetGlobalVectorArray(name);
            foreach(var name in new[]{"_DevlogTerrainWeightsV3","_DevlogRingFades"})_previousWeights[name]=Shader.GetGlobalFloatArray(name);
            _map=map ?? new Map();
            _origin=stage.position+_map.OriginOffset;
            _biomes=new Texture2D(_map.Width,_map.Depth,TextureFormat.RGBA32,false,true) { name="HoverBiomeMap",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
            for (int z=0;z<_map.Depth;z++) for (int x=0;x<_map.Width;x++)
                _biomes.SetPixel(x,z,new Color(Kind(_map.Biome(x+_map.MinX,z+_map.MinZ))/3f,0,0,1));
            _biomes.Apply();
            var shader=_map.DustShader!=null ? _map.DustShader : Shader.Find("Afterlife/Devlog/HoverDust");
            if (shader!=null)
            {
                _dustMaterial=new Material(shader);
                var go=new GameObject("HoverDirtDust"); go.transform.SetParent(stage,false);
                _dust=go.AddComponent<ParticleSystem>(); _dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=_dust.main; main.playOnAwake=false; main.loop=true; main.maxParticles=96;
                main.simulationSpace=ParticleSystemSimulationSpace.World; main.useUnscaledTime=true;
                main.startLifetime=.65f; main.startSpeed=0; main.startSize=new ParticleSystem.MinMaxCurve(.27f,.54f);
                var emission=_dust.emission; emission.enabled=false;
                var shape=_dust.shape; shape.enabled=false;
                var fade=_dust.colorOverLifetime; fade.enabled=true;
                var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.4f,.12f),new GradientAlphaKey(0,1)}); fade.color=gradient;
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial=_dustMaterial;
                _dust.Play();
            }
            Reset();
        }
        private static int Kind(ShowcaseBiome biome) => biome==ShowcaseBiome.Sand ? 2 : biome==ShowcaseBiome.Snow ? 3 : 0;
        public void SetPointer(Vector3? point,ShowcaseBiome biome=ShowcaseBiome.Rocky,bool pointerMoved=false)
        { _pointer=point; _biome=biome; _pointerMoved=point.HasValue && pointerMoved; }
        private void Stamp(Vector3 point,int kind)
        {
            // 아직 살아 있는 흔적을 새 커서 위치로 덮어쓰지 않는다.
            int start=kind==1 ? 0 : 32, count=kind==1 ? 32 : 96;
            for (int i=0;i<count;i++)
            {
                int slot=start+(_next+i)%count;
                var old=_stamps[slot];
                float lifetime=old.w>=2 ? 2.5f : old.w==1 ? 2 : 1.2f;
                if (old.w>=0 && _time-old.z<lifetime) continue;
                _stamps[slot]=new Vector4(point.x,point.z,_time,kind);
                _next=(slot+1)%_stamps.Length;
                if (kind==1) WaterRippleCreated?.Invoke();
                return;
            }
        }
        public void Tick(float delta)
        {
            _time+=delta;
            bool grass=_pointer.HasValue && (_biome==ShowcaseBiome.Grass || _biome==ShowcaseBiome.Forest);
            _strength=Mathf.MoveTowards(_strength,grass ? 1 : 0,delta*5);
            if (grass) _grassPoint=_pointer.Value;
            if (_pointer.HasValue)
            {
                Vector3 p=_pointer.Value;
                float distance=_hasLast ? Vector2.Distance(new Vector2(p.x,p.z),new Vector2(_last.x,_last.z)) : 10;
                bool moved=!_hasLast || distance>.09f;
                int kind=_biome==ShowcaseBiome.Water ? 1 : Kind(_biome);
                if (kind!=_lastKind) { _hasLast=false; moved=true; }
                _lastKind=kind;
                // 정지 중 표면이 복원되며 교차점이 움직여도 새 흔적을 생성하지 않는다.
                if (kind==1)
                {
                    if (_pointerMoved)
                    {
                        bool started=!_waterActive || _time-_lastWaterMotion>.15f;
                        if (started)
                        {
                            Stamp(p,1); _waterDistance=0;
                        }
                        else
                        {
                            Vector3 from=_lastWaterPoint;
                            float segment=Vector2.Distance(new Vector2(from.x,from.z),new Vector2(p.x,p.z));
                            float remaining=segment;
                            while (remaining>0 && _waterDistance+remaining>=RippleSpacing)
                            {
                                float advance=RippleSpacing-_waterDistance;
                                from=Vector3.Lerp(from,p,advance/remaining);
                                Stamp(from,1); remaining-=advance; _waterDistance=0;
                            }
                            _waterDistance+=remaining;
                        }
                        _lastWaterPoint=p;
                        _lastWaterMotion=_time;
                        _waterActive=true;
                    }
                }
                else _waterActive=false;
                if ((grass || kind>=2) && _pointerMoved && moved)
                {
                    int steps=_hasLast && distance<2 && kind>=2 ? Mathf.Clamp(Mathf.CeilToInt(distance/.09f),1,8) : 1;
                    for (int i=1;i<=steps;i++) Stamp(Vector3.Lerp(_last,p,i/(float)steps),kind);
                    _last=p; _hasLast=true;
                }
                if (_pointerMoved && _biome==ShowcaseBiome.Dirt && _dust!=null && _time-_lastDust>.10f)
                {
                    _lastDust=_time;
                    var emit=new ParticleSystem.EmitParams { position=p+Vector3.up*.025f,
                        velocity=new Vector3(Mathf.Sin(_time*31)*.06f,.12f,Mathf.Cos(_time*23)*.06f),startColor=new Color(.43f,.34f,.25f,.30f) };
                    _dust.Emit(emit,2);
                }
            }
            else { _hasLast=false; _waterActive=false; }
            // 입력 유무와 무관하게 각 흔적의 수명과 GPU 감쇠를 갱신한다.
            for (int i=0;i<_stamps.Length;i++)
            {
                float lifetime=_stamps[i].w>=2 ? 2.5f : _stamps[i].w==1 ? 2 : 1.2f;
                float age=_time-_stamps[i].z;
                _weights[i]=_stamps[i].w<0 ? 0 : 1-Mathf.SmoothStep(0,1,age/lifetime);
                if (age>=lifetime) _stamps[i].w=-1;
            }
            UploadWaves();
            UploadActiveStamps();
            Shader.SetGlobalFloat("_DevlogHoverTime",_time);
            Shader.SetGlobalVector("_DevlogHoverOrigin",_origin);
            Shader.SetGlobalVector("_DevlogGrassCursor",new Vector4(_grassPoint.x,_grassPoint.z,_strength,(int)GrassMode));
            Shader.SetGlobalTexture("_DevlogHoverBiomes",_biomes);
            Shader.SetGlobalVector("_DevlogHoverBounds",new Vector4(_map.MinX,_map.MinZ,_map.Width,_map.Depth));
        }
        private void UploadActiveStamps()
        {
            int grassCount=0; _terrainCount=0;
            for (int i=32;i<_stamps.Length;i++)
            {
                var stamp=_stamps[i];
                if (stamp.w==0) _grassStamps[grassCount++]=stamp;
                else if (stamp.w>=2)
                { _terrainStamps[_terrainCount]=stamp; _terrainWeights[_terrainCount++]=_weights[i]; }
            }
            Shader.SetGlobalInt("_DevlogGrassStampCountV3",grassCount);
            Shader.SetGlobalInt("_DevlogTerrainStampCountV3",_terrainCount);
            // 배열 용량은 고정하고 실제 반복 횟수만 바꾼다.
            if (grassCount>0) Shader.SetGlobalVectorArray("_DevlogGrassStampsV3",_grassStamps);
            if (_terrainCount>0)
            {
                Shader.SetGlobalVectorArray("_DevlogTerrainStampsV3",_terrainStamps);
                Shader.SetGlobalFloatArray("_DevlogTerrainWeightsV3",_terrainWeights);
            }
        }
        private void UploadWaves()
        {
            int count=0;
            for (int i=0;i<32;i++)
            {
                var stamp=_stamps[i];
                float age=_time-stamp.z;
                if (stamp.w!=1 || age<0 || age>=2) continue;
                _waves[count]=new Vector4(stamp.x,stamp.y,age*.85f,
                    .035f*Mathf.SmoothStep(0,1,age/.12f)*_weights[i]);
                _ringFades[count]=Mathf.SmoothStep(0,1,age/.10f)
                    *(1-Mathf.SmoothStep(0,1,(age-.2f)/1.8f));
                count++;
            }
            Shader.SetGlobalInt("_DevlogWaveCount",count);
            Shader.SetGlobalVectorArray("_DevlogWaves",_waves);
            Shader.SetGlobalFloatArray("_DevlogRingFades",_ringFades);
        }
        public float HeightOffset(Vector3 world)
        {
            if (_terrainCount==0) return 0;
            Vector3 p=world-_origin;
            int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.z);
            int kind=Kind(_map.Biome(x,z));
            if (kind==0 || x<_map.MinX || x>=_map.MinX+_map.Width || z<_map.MinZ || z>=_map.MinZ+_map.Depth) return 0;
            float mask=1,u=p.x-x,v=p.z-z;
            if (Kind(_map.Biome(x-1,z))!=kind) mask=Mathf.Min(mask,Mathf.SmoothStep(0,1,u/.10f));
            if (Kind(_map.Biome(x+1,z))!=kind) mask=Mathf.Min(mask,Mathf.SmoothStep(0,1,(1-u)/.10f));
            if (Kind(_map.Biome(x,z-1))!=kind) mask=Mathf.Min(mask,Mathf.SmoothStep(0,1,v/.10f));
            if (Kind(_map.Biome(x,z+1))!=kind) mask=Mathf.Min(mask,Mathf.SmoothStep(0,1,(1-v)/.10f));
            float depression=0,ridge=0;
            for (int i=0;i<_terrainCount;i++)
            {
                var stamp=_terrainStamps[i];
                float age=_time-stamp.z;
                if ((int)stamp.w!=kind || age<0 || age>2.5f) continue;
                float dx=world.x-stamp.x,dz=world.z-stamp.y;
                if (dx*dx+dz*dz>.25f) continue;
                float r=Mathf.Sqrt(dx*dx+dz*dz)/.28f;
                float fade=1-Mathf.SmoothStep(0,1,age/2.5f);
                depression=Mathf.Max(depression,Mathf.Exp(-r*r*4)*fade);
                ridge=Mathf.Max(ridge,Mathf.Exp(-(r-.9f)*(r-.9f)*25)*fade);
            }
            return (-.09f*depression+.018f*ridge)*mask*Mathf.Clamp01((p.y+.8f)/.4f);
        }
        // 기존 소비자와의 API 호환성을 유지한다. 픽셀 물결의 실제 높이는 항상 0이다.
        public float RippleHeight(Vector3 world) => 0;
        public void Reset()
        {
            _hasLast=false; _pointer=null; _strength=0; _pointerMoved=false; _waterActive=false;
            _lastWaterMotion=-10; _waterDistance=0; _lastKind=-1; _lastDust=-10;
            for (int i=0;i<_stamps.Length;i++) { _stamps[i]=new Vector4(0,0,-100,-1); _weights[i]=0; }
            UploadWaves();
            UploadActiveStamps();
            if (_dust!=null) _dust.Clear();
            Shader.SetGlobalVector("_DevlogGrassCursor",Vector4.zero);
        }
        public void Dispose()
        {
            WaterRippleCreated=null;
            Reset();
            if (_dust!=null) UnityEngine.Object.Destroy(_dust.gameObject);
            if (_dustMaterial!=null) UnityEngine.Object.Destroy(_dustMaterial);
            Shader.SetGlobalTexture("_DevlogHoverBiomes",_previousBiomes);
            Shader.SetGlobalFloat("_DevlogHoverTime",_previousTime);
            foreach(var pair in _previousVectors)Shader.SetGlobalVector(pair.Key,pair.Value);
            foreach(var pair in _previousCounts)Shader.SetGlobalInt(pair.Key,pair.Value);
            foreach(var pair in _previousArrays)if(pair.Value!=null && pair.Value.Length>0)Shader.SetGlobalVectorArray(pair.Key,pair.Value);
            foreach(var pair in _previousWeights)if(pair.Value!=null && pair.Value.Length>0)Shader.SetGlobalFloatArray(pair.Key,pair.Value);
            UnityEngine.Object.Destroy(_biomes);
        }
    }
}
