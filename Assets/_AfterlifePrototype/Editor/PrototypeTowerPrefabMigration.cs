using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Prototype.Editor
{
    // 별도 Tools 메뉴 없이 기존 Tower를 한 번만 Variant로 이전한다.
    [InitializeOnLoad]
    public static class PrototypeTowerPrefabMigration
    {
        private const string PrefabPath="Assets/_AfterlifePrototype/Prefabs/Modal/Tower.prefab";
        private const string SourcePath="Assets/_AfterlifePrototype/Prefabs/Common/Titled View.prefab";

        static PrototypeTowerPrefabMigration()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneOpened+=(_,__)=>EditorApplication.delayCall+=Apply;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Apply;
            };
        }

        private static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
            foreach(var ui in UnityEngine.Object.FindObjectsByType<PrototypeCanvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(ui.gameObject.scene.name!=PrototypeScene.SceneName)continue;
                var old=ui.Bindings.FirstOrDefault(b=>b!=null&&b.Id=="Modal.Tower")?.Rect;
                if(old==null||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject)==PrefabPath)continue;
                try{Replace(ui,old);}
                catch(Exception error){Debug.LogException(error,ui);}
            }
        }

        private static void Replace(PrototypeCanvas ui,RectTransform old)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)??CreatePrefab(ui,old);
            if(prefab==null)return;
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Tower를 Titled View Variant로 교체");
            try
            {
                var replacement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,old.parent);
                Undo.RegisterCreatedObjectUndo(replacement,"Tower Variant 생성");
                replacement.transform.SetSiblingIndex(old.GetSiblingIndex());
                replacement.SetActive(old.gameObject.activeSelf);
                var rect=(RectTransform)replacement.transform;
                var info=replacement.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Tower.Info");
                var upgrade=replacement.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Tower.Upgrade");
                var close=replacement.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Close Button");
                Undo.RecordObject(ui,"Tower 바인딩 교체");
                ui.Bindings.RemoveAll(b=>b!=null&&((b.Id!=null&&(b.Id=="Modal.Tower"||b.Id.StartsWith("Tower.")))||(b.Rect!=null&&(b.Rect==old||b.Rect.IsChildOf(old)))));
                ui.Bindings.Add(new PrototypeUiBinding{Id="Modal.Tower",Rect=rect});
                ui.Bindings.Add(new PrototypeUiBinding{Id="Tower.Info",Rect=info.rectTransform,TmpText=info});
                ui.Bindings.Add(new PrototypeUiBinding{Id="Tower.Upgrade",Rect=(RectTransform)upgrade.transform,Button=upgrade,TmpText=upgrade.GetComponentInChildren<TMP_Text>(true)});
                ui.Bindings.Add(new PrototypeUiBinding{Id="Tower.Close",Rect=(RectTransform)close.transform,Button=close});
                bool guarded=ui.CameraGuards.Any(r=>r!=null&&(r==old||r.IsChildOf(old)));
                ui.CameraGuards.RemoveAll(r=>r==null||r==old||r.IsChildOf(old));
                if(guarded)ui.CameraGuards.Add(rect);
                Undo.DestroyObjectImmediate(old.gameObject);
                EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                Undo.CollapseUndoOperations(group);
                Debug.Log("Tower를 스크롤 없는 Titled View Variant로 교체했습니다. 씬을 저장하세요.",ui);
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }

        private static GameObject CreatePrefab(PrototypeCanvas ui,RectTransform old)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var font=ui.ResourcesTmpFont!=null?ui.ResourcesTmpFont:AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset");
            if(source==null||font==null)throw new InvalidOperationException("Tower 생성에 필요한 Titled View 또는 TMP 폰트가 없습니다.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.name="Tower";
                var rect=(RectTransform)root.transform;
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(660,430);
                var surface=root.GetComponent<PrototypeModalSurface>()??root.AddComponent<PrototypeModalSurface>();
                var oldSurface=old.GetComponent<PrototypeModalSurface>();
                surface.Presentation=ModalPresentation.Window;
                if(oldSurface!=null)surface.PauseGameplay=oldSurface.PauseGameplay;
                root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Title Text").text="마을의 파수꾼";
                var oldIcon=old.Find("Icon")?.GetComponent<Image>();
                var icon=root.GetComponentsInChildren<Image>(true).Single(i=>i.name=="Icon Image");
                if(oldIcon!=null){icon.sprite=oldIcon.sprite;icon.color=oldIcon.color;icon.preserveAspect=true;}
                var content=root.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Content");
                foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))scroll.enabled=false;
                foreach(var scrollbar in root.GetComponentsInChildren<Scrollbar>(true))scrollbar.gameObject.SetActive(false);
                var viewport=(RectTransform)content.parent;
                Stretch(viewport);Stretch(content);
                content.offsetMin=new Vector2(20,20);content.offsetMax=new Vector2(-20,-20);
                Label(content,"Tower.Info","타워",0,120,font,18);
                Label(content,"Help","단계마다 기본 피해 +20%, 사거리 +0.35\n연구소 강화는 같은 종류의 모든 타워에 적용됩니다.",140,72,font,14);
                var original=ui.Bindings.FirstOrDefault(b=>b!=null&&b.Id=="Tower.Upgrade")?.Button;
                if(original==null)throw new InvalidOperationException("Tower.Upgrade 버튼 바인딩이 없습니다.");
                var button=UnityEngine.Object.Instantiate(original,content,false);
                button.name="Tower.Upgrade";
                foreach(var legacy in button.GetComponentsInChildren<Text>(true))
                {
                    var target=legacy.gameObject;var color=legacy.color;int size=legacy.fontSize;
                    UnityEngine.Object.DestroyImmediate(legacy);
                    var label=target.AddComponent<TextMeshProUGUI>();
                    label.font=font;label.color=color;label.fontSize=size;
                    label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
                }
                button.onClick=new Button.ButtonClickedEvent();button.interactable=true;
                Place((RectTransform)button.transform,235,44);
                var caption=button.GetComponentInChildren<TMP_Text>(true);
                if(caption==null)throw new InvalidOperationException("Tower 강화 버튼의 TMP 글자가 없습니다.");
                caption.text="타워 강화";
                // 기존 고정 너비의 레이블도 새 Content 폭에 맞춰 늘린다.
                Stretch(caption.rectTransform);caption.rectTransform.offsetMin=new Vector2(12,4);caption.rectTransform.offsetMax=new Vector2(-12,-4);
                var saved=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                if(saved==null)throw new InvalidOperationException("Tower Variant 저장에 실패했습니다.");
                return saved;
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        private static void Label(Transform parent,string name,string value,float y,float height,TMP_FontAsset font,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=value;
            text.color=new Color(.96f,.95f,.88f,1);text.raycastTarget=false;
            Place(text.rectTransform,y,height);
        }

        private static void Place(RectTransform rect,float y,float height)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-y);rect.sizeDelta=new Vector2(0,height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
