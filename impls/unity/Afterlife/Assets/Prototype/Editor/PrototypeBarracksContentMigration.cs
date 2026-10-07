using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeBarracksContentMigration
    {
        private const string Path="Assets/Prototype/Prefabs/Modal/Barracks.prefab";
        private static readonly Color Ink=new Color(.96f,.95f,.88f),Gold=new Color(.75f,.57f,.27f),Navy=new Color(.09f,.14f,.20f);

        static PrototypeBarracksContentMigration()
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
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if(prefab==null)return;
            try
            {
                var panel=prefab.GetComponent<PrototypeModalPanel>();
                if(panel==null)throw new InvalidOperationException("Barracks의 모달 바인딩이 없습니다.");
                if(!panel.Bindings.Any(b=>b.Id=="Barracks.Progress"))
                {
                    var root=PrefabUtility.LoadPrefabContents(Path);
                    try{Build(root);PrefabUtility.SaveAsPrefabAsset(root,Path);}
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                foreach(var canvas in UnityEngine.Object.FindObjectsByType<PrototypeCanvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(canvas.gameObject.scene.name!=PrototypeScene.SceneName)continue;
                    var instance=canvas.GetComponentsInChildren<PrototypeModalPanel>(true)
                        .FirstOrDefault(p=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject)==Path);
                    if(instance==null)continue;
                    bool ready=instance.Bindings.All(b=>canvas.Bindings.Any(c=>c.Id==b.Id&&c.Rect==b.Rect&&c.TmpText==b.TmpText&&c.Image==b.Image&&c.Button==b.Button))
                        &&!canvas.Bindings.Any(b=>instance.RetiredBindingIds.Contains(b.Id));
                    if(ready)continue;
                    Undo.RecordObject(canvas,"병영 생산 UI 참조 연결");instance.Connect(canvas);
                    EditorUtility.SetDirty(canvas);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
                    Debug.Log("병영 Content: 현재 생산/진행률, 예약 10칸, 병종 정보 연결 완료. 씬을 저장하세요.",canvas);
                }
            }
            catch(Exception error){Debug.LogException(error);}
        }

        private static void Build(GameObject root)
        {
            var panel=root.GetComponent<PrototypeModalPanel>();
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Prototype/Resources/Fonts/Mona12TextKR SDF.asset");
            if(font==null)throw new InvalidOperationException("병영 UI 폰트가 없습니다.");
            var content=root.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Content");
            foreach(Transform child in content.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            panel.Bindings.RemoveAll(b=>b.Id!="Modal.Barracks"&&b.Id!="Barracks.Close");
            foreach(var kind in new[]{Kind.Warrior,Kind.Archer,Kind.Mage,Kind.Knight})
                if(!panel.RetiredBindingIds.Contains("Recruit.Lock."+kind))panel.RetiredBindingIds.Add("Recruit.Lock."+kind);
            content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,1);
            content.anchoredPosition=new Vector2(0,-12);content.sizeDelta=new Vector2(578,442);
            var scroll=root.GetComponentInChildren<ScrollRect>(true);scroll.enabled=false;
            if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.gameObject.SetActive(false);
            scroll.viewport.anchorMin=Vector2.zero;scroll.viewport.anchorMax=Vector2.one;
            scroll.viewport.offsetMin=scroll.viewport.offsetMax=Vector2.zero;

            Label(panel,content,"Barracks.Current","생산 대기",0,0,430,32,font,21);
            Button(panel,content,"Barracks.Cancel","현재 생산 취소",454,0,124,32,font,14);
            var track=Node(content,"Production Track",0,40,470,14);Graphic(track,new Color(.025f,.04f,.055f));
            var fill=Node(track,"Barracks.Progress",0,0,470,14);var progress=Graphic(fill,new Color(.24f,.82f,.73f));
            progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;progress.fillOrigin=0;progress.fillAmount=0;
            // Filled Image는 흰색 스프라이트를 사용해야 진행률에 맞춰 메시가 잘린다.
            progress.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panel.Bindings.Add(new PrototypeUiBinding{Id="Barracks.Progress",Rect=fill,Image=progress});
            Label(panel,content,"Barracks.Percent","0%",482,34,96,28,font,19).alignment=TextAlignmentOptions.Right;
            Label(panel,content,"Barracks.Status","병종을 선택하면 생산을 시작합니다.",0,62,578,26,font,14);
            Label(panel,content,"Barracks.Queue","생산 예약 0 / 10",0,96,578,25,font,16);
            for(int i=0;i<VillageModel.MaxQueuedOrders;i++)
                Button(panel,content,"Barracks.Slot."+i,$"{i+1:00}\n—",i*58,126,54,50,font,12);
            Label(null,content,"Queue Help","예약 클릭: 취소 · 전액 환급 / 현재 생산 취소: 미진행 비율 환급",0,181,578,23,font,12);
            Kind[] kinds={Kind.Warrior,Kind.Archer,Kind.Mage,Kind.Knight};
            for(int i=0;i<kinds.Length;i++)
            {
                var card=Node(content,"Troop "+kinds[i],(i%2)*294,210+(i/2)*116,284,108);
                Graphic(card,new Color(.045f,.075f,.105f));
                Label(panel,card,"Barracks.Info."+kinds[i],VillageModel.Name(kinds[i]),10,6,264,60,font,13);
                Button(panel,card,"Recruit."+kinds[i],"생산 예약",10,72,264,30,font,14);
            }
        }

        private static RectTransform Node(Transform parent,string name,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }

        private static Image Graphic(RectTransform rect,Color color)
        {
            var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }

        private static TMP_Text Label(PrototypeModalPanel panel,Transform parent,string id,string text,float x,float y,float width,float height,TMP_FontAsset font,int size)
        {
            var rect=Node(parent,id,x,y,width,height);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.fontSize=size;label.color=Ink;label.text=text;label.raycastTarget=false;
            if(panel!=null)panel.Bindings.Add(new PrototypeUiBinding{Id=id,Rect=rect,TmpText=label});return label;
        }

        private static void Button(PrototypeModalPanel panel,Transform parent,string id,string text,float x,float y,float width,float height,TMP_FontAsset font,int size)
        {
            var rect=Node(parent,id,x,y,width,height);var image=Graphic(rect,Navy);image.raycastTarget=true;
            var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(1,-1);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=ColorBlock.defaultColorBlock;colors.highlightedColor=new Color(.48f,.9f,.86f);colors.pressedColor=new Color(.24f,.82f,.73f);
            colors.disabledColor=new Color(.4f,.4f,.4f,.65f);colors.fadeDuration=.1f;button.colors=colors;
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            var label=Label(null,rect,"Label Text",text,2,2,width-4,height-4,font,size);label.alignment=TextAlignmentOptions.Center;
            panel.Bindings.Add(new PrototypeUiBinding{Id=id,Rect=rect,Button=button,TmpText=label});
        }
    }
}
