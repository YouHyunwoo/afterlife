using System;
using UnityEngine;
namespace Afterlife.Modules.SceneFeatures
{
    [Serializable] public sealed class SceneBindings
    {
        public Camera Camera;
        public Light Sun;
        public MapAsset Map;
        public FeedbackTarget[] Targets=new FeedbackTarget[0];
        public MeshFilter[] FoliageSurfaces=new MeshFilter[0];
        public Renderer[] Windows=new Renderer[0],BloomTargets=new Renderer[0];
        public Transform[] SmokeEmitters=new Transform[0];
        public Transform VisionSource;
        public GameObject Axe,Pick;
        public Transform AxeContact,PickContact;
        public Afterlife.Modules.Interaction.PickSurface[] PickSurfaces=new Afterlife.Modules.Interaction.PickSurface[0];
    }
}
