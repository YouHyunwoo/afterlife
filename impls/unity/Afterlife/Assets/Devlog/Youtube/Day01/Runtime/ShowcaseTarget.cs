using UnityEngine;
using Afterlife.Modules.Terrain;
namespace Afterlife.Devlog.Day01
{
    // 기존 프리팹 직렬화와 Day01의 채집 시연 규칙을 보존하는 어댑터.
    public sealed class ShowcaseTarget : Afterlife.Modules.SceneFeatures.FeedbackTarget
    {
        private int _demoHits;
        protected override void BindEnvironment()
        {
            var showcase=GetComponentInParent<Day01Showcase>();
            if(showcase==null || showcase.StageRoots==null)return;
            for(int i=0;i<showcase.StageRoots.Length;i++)
            {
                var stage=showcase.StageRoots[i];
                if(stage==null || !transform.IsChildOf(stage))continue;
                bool model=i>0;
                BiomeQuery=world=> { var pos=stage.InverseTransformPoint(world); return model ? ShowcaseBiomeMap.At(Mathf.FloorToInt(pos.x),Mathf.FloorToInt(pos.z)) : TerrainBiome.Grass; };
                GroundHeightQuery=world=> {
                    var pos=stage.InverseTransformPoint(world);int x=Mathf.FloorToInt(pos.x),z=Mathf.FloorToInt(pos.z);
                    var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
                    if(grid!=null) foreach(Transform tile in grid) {
                        if(Mathf.FloorToInt(tile.localPosition.x)!=x || Mathf.FloorToInt(tile.localPosition.z)!=z)continue;
                        var filter=tile.GetComponent<MeshFilter>();
                        if(filter!=null && filter.sharedMesh!=null && filter.sharedMesh.name.EndsWith("_Flat"))return stage.position.y;
                    }
                    return stage.position.y+(float)TerrainTileVariants.Height(x,z,pos.x-x,pos.z-z);
                };
                break;
            }
            var crowns=new System.Collections.Generic.List<Renderer>();
            foreach(var r in Visual.GetComponentsInChildren<Renderer>()) if(r.name.StartsWith("Tree1_Mesh"))crowns.Add(r);
            PineCrowns=crowns.ToArray();
        }
        protected override void OnHitAccepted(Vector3 point)
        {
            base.OnHitAccepted(point);
            if(Juicy && ++_demoHits >= (DestructionSettings!=null ? Mathf.Max(1,DestructionSettings.HitsToBreak) : 4))
                Break(Vector3.ProjectOnPlane(transform.position-point,Vector3.up).normalized);
        }
        public override void ResetTake() { _demoHits=0; base.ResetTake(); }
    }
}
