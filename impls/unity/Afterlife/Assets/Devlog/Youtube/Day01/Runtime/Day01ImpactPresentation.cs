using UnityEngine;
namespace Afterlife.Devlog.Day01
{
    internal sealed class Day01ImpactPresentation : Afterlife.Modules.SceneFeatures.ImpactPresentation
    {
        public Day01ImpactPresentation(Transform owner, Camera camera, GameObject axe, GameObject pick, GameObject chip, Material material, Afterlife.Modules.WorldAudio.WorldAudioPlayer sound)
            : base(owner,camera,axe,pick,chip,material,sound,Contact(axe),Contact(pick)) { }
        private static Transform Contact(GameObject tool)
        {
            if(tool!=null) foreach(var child in tool.GetComponentsInChildren<Transform>(true))
                if(child.name=="ImpactPoint" || child.name.StartsWith("ImpactPoint.")) return child;
            return null;
        }
    }
}
