using UnityEngine;
namespace Afterlife.Modules.SceneFeatures
{
    [DisallowMultipleComponent]
    public sealed class SceneFeatureHost : MonoBehaviour
    {
        public SceneFeatureProfile Profile;
        public SceneFeatureOverrides Overrides=new SceneFeatureOverrides();
        public SceneBindings Bindings=new SceneBindings();
        public bool ApplyOnStart=true,ActiveEnvironment=true,ExternalTick;
        public GameObject BakedPreview;
        public event System.Action<FeedbackTarget,Vector3> Contact;
        private void OnContact(FeedbackTarget target,Vector3 point)=>Contact?.Invoke(target,point);
        public SceneFeatureSession Session { get; private set; }
        public SceneFeatureSettings Resolve()=>Overrides.Resolve(Profile!=null?Profile.Settings:null);
        private void Start() { if(ApplyOnStart && Session==null)Apply(); }
        private void OnEnable() { if(Application.isPlaying && _started && ApplyOnStart)Apply(); }
        private bool _started;
        public void Apply()
        {
            Clear();_started=true;
            if(BakedPreview!=null)BakedPreview.SetActive(false);
            try { Session=new SceneFeatureSession(transform,Profile,Resolve(),Bindings,ActiveEnvironment);Session.Contact+=OnContact; }
            catch { if(BakedPreview!=null)BakedPreview.SetActive(true);throw; }
        }
        private void Update() { if(!ExternalTick)Session?.Tick(Time.unscaledDeltaTime); }
        public void Clear() { if(Session!=null){Session.Contact-=OnContact;Session.Dispose();}Session=null;if(BakedPreview!=null)BakedPreview.SetActive(true); }
        private void OnDisable()=>Clear();
        private void OnDestroy()=>Clear();
    }
}
