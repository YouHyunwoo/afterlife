using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private void RebuildMap()
        {
            DisposeHover();DisposeVegetation();
            _terrainSurface?.Dispose();_waterSurface?.Dispose();
            if(_mapRoot!=null){_mapRoot.SetActive(false);Destroy(_mapRoot);}
            foreach(var mesh in _decorationMeshes){_environmentMeshes.Remove(mesh);Destroy(mesh);}_decorationMeshes.Clear();
            if(_decorationMaterial!=null){_materials.Remove(_decorationMaterial);Destroy(_decorationMaterial);}
            if(_terrainTiles!=null)Destroy(_terrainTiles);
            BuildGround();UploadTerrainTiles();BuildTerrainDetails();InitializeVegetation();InitializeHover();
            Debug.Log("Prototype map seed: "+_model.Terrain.Seed,this);
        }
    }
}
