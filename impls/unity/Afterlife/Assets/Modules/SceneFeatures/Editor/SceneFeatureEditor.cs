using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Afterlife.Modules.SceneFeatures.Editor
{
    [CustomEditor(typeof(SceneFeatureProfile))]
    public sealed class SceneFeatureProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() { serializedObject.Update();EditorGUILayout.HelpBox("그룹을 꺼도 개별 기능 설정은 유지됩니다. 변경 후 씬 Host에서 적용하세요.",MessageType.Info);DrawPropertiesExcluding(serializedObject,"m_Script");serializedObject.ApplyModifiedProperties(); }
    }
    [CustomPropertyDrawer(typeof(FeatureGroup))]
    public sealed class FeatureGroupDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,GUIContent label)=>EditorGUIUtility.singleLineHeight+4+(property.isExpanded?(property.FindPropertyRelative("Features").arraySize+1)*(EditorGUIUtility.singleLineHeight+2):0);
        public override void OnGUI(Rect rect,SerializedProperty property,GUIContent label)
        {
            var line=new Rect(rect.x,rect.y,rect.width,EditorGUIUtility.singleLineHeight);
            string title=property.name=="MapEnvironment"?"맵 환경":property.name=="InteractionFeedback"?"상호작용 피드백":"분위기·화면 표현";
            property.isExpanded=EditorGUI.Foldout(line,property.isExpanded,title,true);if(!property.isExpanded)return;
            EditorGUI.indentLevel++;line.y+=line.height+2;
            var enabled=property.FindPropertyRelative("Enabled");EditorGUI.PropertyField(line,enabled,new GUIContent("그룹 활성화"));
            var list=property.FindPropertyRelative("Features");
            using(new EditorGUI.DisabledScope(!enabled.boolValue))for(int i=0;i<list.arraySize;i++) {
                line.y+=line.height+2;var item=list.GetArrayElementAtIndex(i);var feature=item.FindPropertyRelative("Feature");
                EditorGUI.PropertyField(line,item.FindPropertyRelative("Enabled"),new GUIContent(((SceneFeature)feature.enumValueIndex).ToString()));
            }
            EditorGUI.indentLevel--;
        }
    }
    [CustomEditor(typeof(SceneFeatureHost))]
    public sealed class SceneFeatureHostEditor : UnityEditor.Editor
    {
        private bool _preview;
        private double _last;
        private void OnEnable(){EditorApplication.update+=PreviewTick;_last=EditorApplication.timeSinceStartup;}
        private void OnDisable(){EditorApplication.update-=PreviewTick;}
        private void PreviewTick() {
            double now=EditorApplication.timeSinceStartup;float delta=(float)Math.Min(.05,now-_last);_last=now;
            var host=target as SceneFeatureHost;if(!_preview || Application.isPlaying || host==null || host.Session==null)return;
            host.Session.Tick(delta);SceneView.RepaintAll();
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var host=(SceneFeatureHost)target;
            EditorGUILayout.HelpBox("설정 변경만으로 맵을 다시 만들지 않습니다. 적용은 생성 결과를 교체하고, 저장은 별도 미리보기 에셋을 만듭니다.",MessageType.Info);
            if(GUILayout.Button("적용 / 미리보기 갱신"))host.Apply();
            _preview=EditorGUILayout.Toggle("미리보기 시간 재생",_preview);
            if(host.Session!=null)foreach(var message in host.Session.Diagnostics)EditorGUILayout.HelpBox(message,MessageType.Warning);
            using(new EditorGUI.DisabledScope(Application.isPlaying||host.Session==null))if(GUILayout.Button("생성 결과 저장")) {
                string folder=EditorUtility.SaveFolderPanel("생성 에셋 저장 위치",Application.dataPath,"");
                if(!string.IsNullOrEmpty(folder)){if(!folder.Replace('\\','/').StartsWith(Application.dataPath.Replace('\\','/')+"/",StringComparison.OrdinalIgnoreCase))EditorUtility.DisplayDialog("저장 위치","Assets 내부 폴더를 선택하세요.","확인");else SavePreview(host,"Assets"+folder.Substring(Application.dataPath.Length));}
            }
            if(GUILayout.Button("생성 결과 정리")) {
                host.Clear();if(host.BakedPreview!=null){Undo.DestroyObjectImmediate(host.BakedPreview);Undo.RecordObject(host,"Clear feature preview");host.BakedPreview=null;}
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            }
        }
        public static void SavePreview(SceneFeatureHost host,string folder)
        {
            if(host.Session==null)throw new InvalidOperationException("Apply before saving.");
            System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var preview=new GameObject("SceneFeatureBakedPreview");preview.transform.SetParent(host.transform,false);Undo.RegisterCreatedObjectUndo(preview,"Save feature preview");
            var meshes=new Dictionary<Mesh,Mesh>();var materials=new Dictionary<Material,Material>();
            var filters=new List<MeshFilter>(host.Session.GeneratedRoot.GetComponentsInChildren<MeshFilter>(true));
            foreach(var go in host.Session.ExternalGenerated)if(go!=null)filters.AddRange(go.GetComponents<MeshFilter>());
            foreach(var filter in filters) {
                var source=filter.GetComponent<MeshRenderer>();if(source==null || !source.enabled || filter.sharedMesh==null)continue;
                var go=new GameObject(filter.name);go.transform.SetParent(preview.transform,false);go.transform.position=filter.transform.position;go.transform.rotation=filter.transform.rotation;
                var scale=filter.transform.lossyScale;var parent=preview.transform.lossyScale;go.transform.localScale=new Vector3(scale.x/parent.x,scale.y/parent.y,scale.z/parent.z);
                if(!meshes.TryGetValue(filter.sharedMesh,out var mesh)){mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(folder+"/Mesh.asset"));meshes.Add(filter.sharedMesh,mesh);}
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();var copied=new List<Material>();
                foreach(var material in source.sharedMaterials){if(material==null){copied.Add(null);continue;}if(!materials.TryGetValue(material,out var saved)){saved=UnityEngine.Object.Instantiate(material);AssetDatabase.CreateAsset(saved,AssetDatabase.GenerateUniqueAssetPath(folder+"/Material.mat"));materials.Add(material,saved);}copied.Add(saved);}
                renderer.sharedMaterials=copied.ToArray();renderer.shadowCastingMode=source.shadowCastingMode;renderer.receiveShadows=source.receiveShadows;
            }
            Undo.RecordObject(host,"Save feature preview");if(host.BakedPreview!=null)Undo.DestroyObjectImmediate(host.BakedPreview);host.BakedPreview=preview;
            host.Clear();EditorUtility.SetDirty(host);EditorSceneManager.MarkSceneDirty(host.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
