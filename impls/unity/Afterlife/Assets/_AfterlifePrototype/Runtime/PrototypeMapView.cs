using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        internal static void MeasureStartup(string stage,System.Action action)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var timer=System.Diagnostics.Stopwatch.StartNew();
            UnityEngine.Profiling.Profiler.BeginSample("Prototype.Startup."+stage);
            try { action(); }
            finally
            {
                UnityEngine.Profiling.Profiler.EndSample();
                Debug.Log($"[Prototype.Startup] {stage}: {timer.Elapsed.TotalMilliseconds:F1} ms");
            }
#else
            action();
#endif
        }

        private void RebuildMap()
        {
            DisposeHover();DisposeVegetation();
            _terrainSurface?.Dispose();_waterSurface?.Dispose();
            if(_mapRoot!=null){_mapRoot.SetActive(false);Destroy(_mapRoot);}
            foreach(var mesh in _decorationMeshes){_environmentMeshes.Remove(mesh);Destroy(mesh);}_decorationMeshes.Clear();
            if(_decorationMaterial!=null){_materials.Remove(_decorationMaterial);Destroy(_decorationMaterial);}
            if(_terrainTiles!=null)Destroy(_terrainTiles);
            MeasureStartup("Ground",BuildGround);
            InitializePresentation();
            Debug.Log("Prototype map seed: "+_model.Terrain.Seed,this);
        }
    }
}
