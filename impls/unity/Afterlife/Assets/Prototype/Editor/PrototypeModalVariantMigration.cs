using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Prototype.Editor
{
    // 요청된 여섯 모달만 한 번 이전한다. 완료된 Variant와 사용자 편집은 유지한다.
    [InitializeOnLoad]
    public static class PrototypeModalVariantMigration
    {
        private static readonly string[] Names={"Barracks","Altar","RebirthShop","Result","Settlement","Research"};
        private const string Folder="Assets/Prototype/Prefabs/Modal/";
        private const string Source="Assets/Prototype/Prefabs/Common/Titled View.prefab";

        static PrototypeModalVariantMigration()
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
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<PrototypeCanvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(canvas.gameObject.scene.name!=PrototypeScene.SceneName)continue;
                foreach(var name in Names)
                {
                    var old=canvas.Bindings.FirstOrDefault(b=>b!=null&&b.Id=="Modal."+name)?.Rect;
                    if(old==null||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject)==Folder+name+".prefab")continue;
                    try{Replace(canvas,old,name);}
                    catch(Exception error){Debug.LogException(error,canvas);}
                }
            }
        }

        private static void Replace(PrototypeCanvas canvas,RectTransform old,string name)
        {
            string path=Folder+name+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path)??Create(canvas,old,name,path);
            if(PrefabUtility.GetPrefabAssetType(prefab)!=PrefabAssetType.Variant||prefab.GetComponent<PrototypeModalPanel>()==null)
                throw new InvalidOperationException(path+"의 Variant 및 바인딩을 확인하세요. 기존 에셋은 덮어쓰지 않습니다.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName(name+" 모달 Variant 교체");
            try
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,old.parent);
                Undo.RegisterCreatedObjectUndo(instance,name+" Variant 생성");
                instance.transform.SetSiblingIndex(old.GetSiblingIndex());instance.SetActive(old.gameObject.activeSelf);
                var panel=instance.GetComponent<PrototypeModalPanel>();
                var expected=canvas.Bindings.Where(b=>b!=null&&b.Rect!=null&&(b.Rect==old||b.Rect.IsChildOf(old))).ToArray();
                foreach(var binding in expected)
                    if(!panel.Bindings.Any(b=>b.Id==binding.Id&&b.Rect!=null))
                        throw new InvalidOperationException(name+" 바인딩 누락: "+binding.Id);
                Undo.RecordObject(canvas,name+" UI 참조 연결");
                canvas.Bindings.RemoveAll(b=>expected.Contains(b));panel.Connect(canvas);
                bool guarded=canvas.CameraGuards.Any(r=>r!=null&&(r==old||r.IsChildOf(old)));
                canvas.CameraGuards.RemoveAll(r=>r==null||r==old||r.IsChildOf(old));
                if(guarded)canvas.CameraGuards.Add((RectTransform)instance.transform);
                Undo.DestroyObjectImmediate(old.gameObject);
                EditorUtility.SetDirty(canvas);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
                Undo.CollapseUndoOperations(group);
                Debug.Log(name+"를 Titled View Variant로 교체했습니다. 씬을 저장하세요.",canvas);
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }

        private static GameObject Create(PrototypeCanvas canvas,RectTransform old,string name,string path)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            var font=canvas.ResourcesTmpFont!=null?canvas.ResourcesTmpFont:AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Prototype/Resources/Fonts/Mona12TextKR SDF.asset");
            if(source==null||font==null)throw new InvalidOperationException("Titled View 또는 TMP 폰트가 없습니다.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.name=name;var rect=(RectTransform)root.transform;
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                rect.anchoredPosition=Vector2.zero;rect.sizeDelta=name=="Result"?new Vector2(600,360):new Vector2(660,550);
                var surface=root.AddComponent<PrototypeModalSurface>();
                var previous=old.GetComponent<PrototypeModalSurface>();
                if(previous!=null)EditorUtility.CopySerialized(previous,surface);
                surface.Presentation=ModalPresentation.Window;
                var title=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Title Text");
                var oldTitle=old.Find(name=="Result"?"Result.Title":"Title");
                title.text=oldTitle?.GetComponent<TMP_Text>()?.text??oldTitle?.GetComponent<Text>()?.text??name;
                var icon=root.GetComponentsInChildren<Image>(true).Single(i=>i.name=="Icon Image");
                var oldIcon=old.Find("Icon")?.GetComponent<Image>();
                icon.gameObject.SetActive(oldIcon!=null);
                if(oldIcon!=null){icon.sprite=oldIcon.sprite;icon.color=oldIcon.color;icon.preserveAspect=true;}
                var close=root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Close Button");
                close.gameObject.SetActive(name!="Result");
                var content=root.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Content");
                var scroll=root.GetComponentInChildren<ScrollRect>(true);
                bool scrolling=name=="Research"||name=="RebirthShop";
                scroll.enabled=scrolling;scroll.horizontal=false;scroll.vertical=scrolling;
                scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
                scroll.verticalScrollbar.gameObject.SetActive(scrolling);
                scroll.viewport.anchorMin=Vector2.zero;scroll.viewport.anchorMax=Vector2.one;
                scroll.viewport.offsetMin=Vector2.zero;scroll.viewport.offsetMax=new Vector2(scrolling?-20:0,0);
                content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
                content.anchoredPosition=new Vector2(0,-12);
                var map=new Dictionary<Transform,Transform>{{old,root.transform}};
                if(oldTitle!=null)map.Add(oldTitle,title.transform);
                var oldClose=old.Find(name+".Close");
                if(oldClose!=null)
                {
                    map.Add(oldClose,close.transform);
                    var oldCaption=oldClose.GetComponentInChildren<Text>(true);
                    if(oldCaption!=null)map[oldCaption.transform]=close.GetComponentInChildren<TMP_Text>(true).transform;
                }
                float height;
                if(name=="Result")
                {
                    foreach(string child in new[]{"Result.Body","Result.Restart"})
                    {
                        var original=old.Find(child) as RectTransform;
                        if(original==null)throw new InvalidOperationException(child+"가 없습니다.");
                        var copy=Clone(original,content,map);
                        copy.anchoredPosition=new Vector2(0,original.anchoredPosition.y+98);
                    }
                    height=232;
                    title.rectTransform.anchorMin=Vector2.zero;title.rectTransform.anchorMax=Vector2.one;
                    title.rectTransform.offsetMin=new Vector2(12,0);title.rectTransform.offsetMax=new Vector2(-12,0);
                }
                else
                {
                    var original=old.GetComponentInChildren<ScrollRect>(true)?.content;
                    if(original==null)throw new InvalidOperationException(name+"의 기존 Content가 없습니다.");
                    map[original]=content;
                    height=original.rect.height;
                    foreach(RectTransform child in original)
                    {
                        Clone(child,content,map);
                        height=Mathf.Max(height,-child.anchoredPosition.y+child.rect.height);
                    }
                }
                content.sizeDelta=new Vector2(name=="Result"?476:578,height+12);
                ConvertText(root,font);
                var panel=root.AddComponent<PrototypeModalPanel>();
                foreach(var binding in canvas.Bindings.Where(b=>b!=null&&b.Rect!=null&&(b.Rect==old||b.Rect.IsChildOf(old))))
                {
                    var text=Mapped(binding.Text,map);
                    var tmp=Mapped(binding.TmpText,map);
                    if(binding.Text!=null&&tmp==null)tmp=map[binding.Text.transform].GetComponent<TMP_Text>();
                    panel.Bindings.Add(new PrototypeUiBinding
                    {
                        Id=binding.Id,Rect=Mapped(binding.Rect,map),Text=text,TmpText=tmp,
                        Button=Mapped(binding.Button,map),Slider=Mapped(binding.Slider,map),Image=Mapped(binding.Image,map)
                    });
                }
                if(panel.Bindings.Any(b=>b.Rect==null))throw new InvalidOperationException(name+"의 UI 참조 이전에 실패했습니다.");
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path);
                if(saved==null)throw new InvalidOperationException(path+" 저장에 실패했습니다.");
                return saved;
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        private static RectTransform Clone(RectTransform original,Transform parent,Dictionary<Transform,Transform> map)
        {
            var copy=UnityEngine.Object.Instantiate(original,parent,false);copy.name=original.name;
            MapTree(original,copy,map);return copy;
        }

        private static void MapTree(Transform original,Transform copy,Dictionary<Transform,Transform> map)
        {
            map.Add(original,copy);
            for(int i=0;i<original.childCount;i++)MapTree(original.GetChild(i),copy.GetChild(i),map);
        }

        private static T Mapped<T>(T component,Dictionary<Transform,Transform> map) where T:Component
        {
            if(component==null)return null;
            if(!map.TryGetValue(component.transform,out var target))throw new InvalidOperationException("UI 참조 이전 대상 누락: "+component.name);
            var result=target.GetComponent<T>();
            if(result==null&&typeof(T)!=typeof(Text))throw new InvalidOperationException("UI 컴포넌트 이전 실패: "+component.name+" / "+typeof(T).Name);
            return result;
        }

        private static void ConvertText(GameObject root,TMP_FontAsset font)
        {
            var alignments=new[]{TextAlignmentOptions.TopLeft,TextAlignmentOptions.Top,TextAlignmentOptions.TopRight,TextAlignmentOptions.Left,TextAlignmentOptions.Center,TextAlignmentOptions.Right,TextAlignmentOptions.BottomLeft,TextAlignmentOptions.Bottom,TextAlignmentOptions.BottomRight};
            foreach(var old in root.GetComponentsInChildren<Text>(true))
            {
                var go=old.gameObject;string value=old.text;var color=old.color;int size=old.fontSize;
                var alignment=alignments[(int)old.alignment];bool rich=old.supportRichText,enabled=old.enabled;
                var style=old.fontStyle;bool wrap=old.horizontalOverflow==HorizontalWrapMode.Wrap;
                UnityEngine.Object.DestroyImmediate(old);
                var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.color=color;text.fontSize=size;
                text.alignment=alignment;text.richText=rich;text.enabled=enabled;text.raycastTarget=false;
                text.textWrappingMode=wrap?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
                text.fontStyle=(style==FontStyle.Bold||style==FontStyle.BoldAndItalic?FontStyles.Bold:FontStyles.Normal)
                    |(style==FontStyle.Italic||style==FontStyle.BoldAndItalic?FontStyles.Italic:FontStyles.Normal);
            }
        }
    }
}
