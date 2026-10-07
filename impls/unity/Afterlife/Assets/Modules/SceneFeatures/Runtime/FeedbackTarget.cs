using System;
using System.Collections.Generic;
using UnityEngine;
using Afterlife.Modules.TargetContracts;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.Destruction;
namespace Afterlife.Modules.SceneFeatures
{
    // 직렬화/Unity 수명 어댑터. 계산·연출 서비스는 ObjectFeedback을 직접 호출할 수 있다.
    public class FeedbackTarget : MonoBehaviour, ISnowTarget, IImpactReceiver
    {
        public bool IsWood=>Wood;
        public bool UseJuice=>Juicy;
        public Transform Root=>transform;
        public bool Active=>this!=null&&isActiveAndEnabled;
        public Transform Visual;
        Transform ISnowTarget.Visual=>Visual;
        public bool Juicy,Wood,EnableSnow=true;
        public DestructionProfile DestructionSettings;
        public Func<Vector3,float> GroundHeightQuery;
        public Func<Vector3,TerrainBiome> BiomeQuery;
        public Renderer[] PineCrowns=new Renderer[0];
        public bool ExternalTick,HitFeedbackEnabled=true,DestructionEnabled=true;
        public Func<bool> CanReceiveHit;
        public bool DamageMarksEnabled { get;set; }=true;
        public float FallSoundDuration { get;set; }=2.94f;
        public DisappearMode RemovalMode { get;set; }
        public TerrainBiome Biome=>_view!=null?_view.Biome:TerrainBiome.Grass;
        public bool HasSnow=>_view!=null&&_view.HasSnow;
        public bool IsBroken=>_view!=null&&_view.IsBroken;
        public bool CanHit=>_view!=null&&_view.CanHit;
        public int RenderVersion=>_view!=null?_view.RenderVersion:0;
        public event Action<FeedbackTarget,Vector3> Contact;
        private ObjectFeedback _view;
        protected virtual void BindEnvironment() { }
        protected virtual void OnHitAccepted(Vector3 point)=>Contact?.Invoke(this,point);
        protected virtual void Awake()=>Initialize();
        public void Initialize()
        {
            if(_view!=null)return;if(Visual==null)Visual=transform;BindEnvironment();
            _view=new ObjectFeedback(transform,Visual,this);Sync();_view.Contact+=OnHitAccepted;_view.Initialize();
        }
        private void Sync()
        {
            if(_view==null)return;_view.Juicy=Juicy;_view.Wood=Wood;_view.EnableSnow=EnableSnow;_view.DestructionSettings=DestructionSettings;
            _view.GroundHeightQuery=GroundHeightQuery;_view.BiomeQuery=BiomeQuery;_view.PineCrowns=PineCrowns;_view.CanReceiveHit=CanReceiveHit;
            _view.HitFeedbackEnabled=HitFeedbackEnabled;_view.DestructionEnabled=DestructionEnabled;_view.DamageMarksEnabled=DamageMarksEnabled;_view.RemovalMode=RemovalMode;
        }
        public void SetFeatureFlags(bool hit,bool marks,bool snow,bool destruction)
        {
            HitFeedbackEnabled=hit;DamageMarksEnabled=marks;EnableSnow=snow;DestructionEnabled=destruction;
            if(_view!=null){_view.SetFeatureFlags(hit,marks,snow,destruction);Sync();}
        }
        public float SnowRemaining(Vector3 world)=>_view!=null?_view.SnowRemaining(world):1;
        public bool SnowVisible(Vector3 world)=>_view==null||_view.SnowVisible(world);
        public List<Vector3> ShedSnow(Vector3 point) { Initialize();Sync();return _view.ShedSnow(point); }
        public void RecordDamageMark(Vector3 point,Vector3 normal,Color color) { Initialize();Sync();_view.RecordDamageMark(point,normal,color); }
        public void SetHover(bool value) { Initialize();_view.SetHover(value); }
        public void Hit(Vector3 point,Vector3 direction) { Initialize();Sync();_view.Hit(point,direction); }
        public void Break(Vector3 direction) { Initialize();Sync();_view.Break(direction); }
        public virtual void ResetTake() { Sync();_view?.ResetTake(); }
        public void Tick(float delta) { Initialize();Sync();_view.Tick(delta); }
        protected virtual void Update() { if(!ExternalTick)Tick(Time.unscaledDeltaTime); }
        protected virtual void OnDisable()=>ResetTake();
        public void ReleasePresentation() { var view=_view;_view=null;if(view!=null){view.Contact-=OnHitAccepted;view.Dispose();} }
        protected virtual void OnDestroy()=>ReleasePresentation();
    }
}
