using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    internal static class Day01ContinuousTerrainSetup
    {
        private const string Root=Day01SceneBuilder.Root;
        private const string Geometry=Root+"/Geometry/ContinuousTerrain";
        private static bool _running;
        private sealed class Placement {public Transform Transform; public Vector3 Position; public Quaternion Rotation; public float OldHeight;}
        [Serializable] private sealed class Report
        {
            public string savedAtUtc;
            public int stages,tiles,placements,sharedBoundaryNodes;
            public float maximumBoundaryError,minimumHeight,maximumHeight;
            public string note="Map coordinates are shared across showcase copies; Before/Shape, water and original mesh assets preserved.";
        }
        static Day01ContinuousTerrainSetup()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneSaved+=_=>EditorApplication.delayCall+=Apply;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall+=Apply;};
        }
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static void Apply()
        {
            if(_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) {EditorApplication.delayCall+=Apply;return;}
            if(!File.Exists(Day01SceneBuilder.ScenePath)) return;
            var scene=SceneManager.GetSceneByPath(Day01SceneBuilder.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(!opened && scene.isDirty) return;
            _running=true; var previous=SceneManager.GetActiveScene();
            try
            {
                if(opened) scene=EditorSceneManager.OpenScene(Day01SceneBuilder.ScenePath,OpenSceneMode.Additive);
                var showcase=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day01Showcase>(true)).FirstOrDefault();
                if(showcase==null || !showcase.HasPresentationStages || showcase.ContinuousTerrainVersion>=1) return;
                Folder(Geometry); Folder(Root+"/Prefabs/Stages/ContinuousTerrain"); Folder(Root+"/Scenes/Archive");
                var backup=Root+"/Scenes/Archive/Devlog_Day01_BeforeContinuousTerrain.unity";
                if(!File.Exists(backup) && !AssetDatabase.CopyAsset(Day01SceneBuilder.ScenePath,backup)) throw new IOException("Terrain backup failed.");
                var report=new Report {savedAtUtc=DateTime.UtcNow.ToString("O"),minimumHeight=float.PositiveInfinity,maximumHeight=float.NegativeInfinity};
                for(int phase=2;phase<6;phase++)
                {
                    var stage=showcase.StageRoots[phase]; var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
                    if(grid==null) throw new InvalidOperationException("Missing terrain grid: "+stage.name);
                    var tiles=grid.Cast<Transform>().Where(t=>t.name.StartsWith("Tile_") && !t.name.EndsWith("_Water") && t.gameObject.activeSelf && t.GetComponent<MeshFilter>()!=null).ToArray();
                    var original=tiles.ToDictionary(t=>new Vector2Int(Mathf.FloorToInt(t.localPosition.x),Mathf.FloorToInt(t.localPosition.z)),t=>t.GetComponent<MeshFilter>().sharedMesh);
                    var placements=new List<Placement>();
                    void Capture(Transform item)
                    {
                        var pos=stage.InverseTransformPoint(item.position); int x=Mathf.FloorToInt(pos.x),z=Mathf.FloorToInt(pos.z);
                        if(!original.TryGetValue(new Vector2Int(x,z),out var mesh)) return;
                        float oldHeight=mesh.name.StartsWith("ContinuousTerrain_") ? (float)ShowcaseTerrainField.Height(pos.x,pos.z)
                            : mesh.name.EndsWith("_Flat") ? 0 : (float)ShowcaseTileVariants.LegacyHeight(x,z,pos.x-x,pos.z-z);
                        placements.Add(new Placement {Transform=item,Position=item.position,Rotation=item.rotation,OldHeight=oldHeight});
                    }
                    foreach(string name in new[]{"ClickableResources","Environment","GroundContactDetails","GroundDetails"})
                    {
                        var group=stage.Find(name); if(group==null) continue;
                        foreach(Transform child in group) Capture(child);
                    }
                    foreach(var tile in tiles) foreach(Transform child in tile) Capture(child);
                    var edgeNodes=new Dictionary<Vector2Int,float>();
                    foreach(var tile in tiles)
                    {
                        int x=Mathf.FloorToInt(tile.localPosition.x),z=Mathf.FloorToInt(tile.localPosition.z);
                        var filter=tile.GetComponent<MeshFilter>();
                        var mesh=BuildMesh(x,z);
                        Undo.RecordObjects(new Object[]{tile,filter},"연속 Perlin 지형 적용");
                        tile.localPosition=new Vector3(x+.5f,0,z+.5f); tile.localRotation=Quaternion.identity; tile.localScale=Vector3.one;
                        filter.sharedMesh=mesh;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(tile); PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                        report.tiles++;
                        // 저장할 실제 상면 정점에서 타일 간 공유 경계 높이를 확인한다.
                        var visited=new HashSet<Vector2Int>();
                        var vertices=mesh.vertices;
                        foreach(int index in mesh.GetTriangles(0))
                        {
                            var p0=vertices[index];
                            report.minimumHeight=Mathf.Min(report.minimumHeight,p0.y); report.maximumHeight=Mathf.Max(report.maximumHeight,p0.y);
                            if(Mathf.Abs(Mathf.Abs(p0.x)-.5f)>.00001f && Mathf.Abs(Mathf.Abs(p0.z)-.5f)>.00001f) continue;
                            var key=new Vector2Int(Mathf.RoundToInt((x+.5f+p0.x)*4),Mathf.RoundToInt((z+.5f+p0.z)*4));
                            if(!visited.Add(key)) continue;
                            if(edgeNodes.TryGetValue(key,out float old)) {report.sharedBoundaryNodes++;report.maximumBoundaryError=Mathf.Max(report.maximumBoundaryError,Mathf.Abs(old-p0.y));}
                            else edgeNodes.Add(key,p0.y);
                        }
                    }
                    foreach(var placement in placements)
                    {
                        var pos=stage.InverseTransformPoint(placement.Position);
                        float height=(float)ShowcaseTerrainField.Height(pos.x,pos.z);
                        Undo.RecordObject(placement.Transform,"지형 접지 높이 갱신");
                        placement.Transform.SetPositionAndRotation(placement.Position+stage.TransformVector(Vector3.up*(height-placement.OldHeight)),placement.Rotation);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(placement.Transform); report.placements++;
                    }
                    if(report.maximumBoundaryError>.00001f) throw new InvalidOperationException("Terrain boundary mismatch.");
                    var copy=Object.Instantiate(stage.gameObject);
                    try
                    {
                        if(PrefabUtility.IsPartOfPrefabInstance(copy)) PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        if(PrefabUtility.SaveAsPrefabAsset(copy,Root+"/Prefabs/Stages/ContinuousTerrain/Phase"+phase+".prefab")==null) throw new IOException("Terrain stage prefab save failed.");
                    }
                    finally {Object.DestroyImmediate(copy);}
                    report.stages++;
                }
                showcase.ContinuousTerrainVersion=1; EditorUtility.SetDirty(showcase); PrefabUtility.RecordPrefabInstancePropertyModifications(showcase);
                if(PrefabUtility.SaveAsPrefabAsset(showcase.gameObject,Root+"/Prefabs/Composition/Day01Progression.prefab")==null) throw new IOException("Terrain composition save failed.");
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Terrain scene save failed.");
                File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../devlog/youtube/records/continuous-terrain-unity.json")),JsonUtility.ToJson(report,true));
                Debug.Log("Devlog continuous terrain saved: "+report.tiles+" tiles / shared boundary error "+report.maximumBoundaryError);
            }
            catch(Exception e) {Debug.LogException(e);}
            finally
            {
                if(opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                _running=false;
            }
        }
        private static Mesh BuildMesh(int tileX,int tileZ)
        {
            string name="ContinuousTerrain_"+tileX+"_"+tileZ;
            string path=Geometry+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(mesh!=null) return mesh;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var colors=new List<Color>();
            var top=new List<int>(); var sides=new List<int>();
            Vector3 Node(int x,int z) => new Vector3(x/4f-.5f,(float)ShowcaseTerrainField.Node(tileX+x/4.0,tileZ+z/4.0),z/4f-.5f);
            void Tri(Vector3 a,Vector3 b,Vector3 c,List<int> indices)
            {
                int start=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);
                var normal=Vector3.Cross(b-a,c-a).normalized;
                for(int i=0;i<3;i++) {normals.Add(normal);colors.Add(Color.white);indices.Add(start+i);}
            }
            for(int z=0;z<4;z++) for(int x=0;x<4;x++)
            {
                if(ShowcaseTerrainField.Alternate(tileX*4+x,tileZ*4+z))
                {Tri(Node(x,z),Node(x,z+1),Node(x+1,z),top);Tri(Node(x+1,z),Node(x,z+1),Node(x+1,z+1),top);}
                else {Tri(Node(x,z),Node(x,z+1),Node(x+1,z+1),top);Tri(Node(x,z),Node(x+1,z+1),Node(x+1,z),top);}
            }
            void Wall(Vector3 a,Vector3 b)
            {
                var c=new Vector3(b.x,-.8f,b.z);var d=new Vector3(a.x,-.8f,a.z);
                Tri(b,a,d,sides);Tri(b,d,c,sides);
            }
            for(int i=0;i<4;i++)
            {
                Wall(Node(0,i),Node(0,i+1));Wall(Node(i,4),Node(i+1,4));
                Wall(Node(4,4-i),Node(4,3-i));Wall(Node(4-i,0),Node(3-i,0));
            }
            Tri(new Vector3(-.5f,-.8f,-.5f),new Vector3(.5f,-.8f,.5f),new Vector3(-.5f,-.8f,.5f),sides);
            Tri(new Vector3(-.5f,-.8f,-.5f),new Vector3(.5f,-.8f,-.5f),new Vector3(.5f,-.8f,.5f),sides);
            mesh=new Mesh {name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);
            mesh.subMeshCount=2;mesh.SetTriangles(top,0);mesh.SetTriangles(sides,1);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
