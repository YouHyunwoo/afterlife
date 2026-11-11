using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Devlog.Day01
{
    internal sealed class Day01PixelOutput : Afterlife.Modules.SceneFeatures.CameraPresentation
    {
        public Day01PixelOutput(Transform owner,Camera camera,UniversalRenderPipelineAsset pipeline):base(owner,camera,pipeline) { }
    }
}
