using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool _buildItemsInitialized;
        private readonly List<GameObject> _runtimeBuildItems=new List<GameObject>();
        private void InitializeBuildItems()
        {
            if(_buildItemsInitialized)return;
            var ui=_sceneUi;
            var catalog=ui.BuildCatalog!=null?ui.BuildCatalog:Resources.Load<PrototypeBuildCatalog>(PrototypeBuildCatalog.ResourcePath);
            var panel=ui.Bindings.FirstOrDefault(b=>b!=null&&b.Id=="Build")?.Rect;
            var scroll=panel!=null?panel.GetComponentInChildren<ScrollRect>(true):null;
            if(catalog==null||catalog.ItemPrefab==null||scroll==null||scroll.content==null)
                throw new InvalidOperationException("건설 목록: BuildCatalog/ItemPrefab/Buildings Content 연결이 필요합니다.");
            var prefab=catalog.ItemPrefab;
            if(prefab.NameText==null||prefab.CostText==null||prefab.Icon==null||prefab.Button==null)
                throw new InvalidOperationException("Build.Item의 TMP Name Text/Cost Text/Icon/Button 참조를 확인하세요.");
            var ids=new HashSet<Kind>();
            foreach(var data in catalog.Buildings)
                if(data==null||!ids.Add(data.Id)||data.GoldCost<0||!VillageModel.IsBuildingKind(data.Id)||data.Id==Kind.Market)
                    throw new InvalidOperationException("BuildCatalog에 누락/중복/지원하지 않는 건설 ID 또는 잘못된 가격이 있습니다.");
            // 이전 씬에 저장된 항목도 런타임에서만 교체한다. 버튼 연결 전에 목록을 완성한다.
            var oldItems=ui.Bindings.Where(b=>b!=null&&b.Id!=null&&b.Id.StartsWith("Build.Item.")&&b.Rect!=null&&b.Rect.IsChildOf(scroll.content))
                .Select(b=>b.Rect.gameObject).Concat(scroll.content.GetComponentsInChildren<PrototypeBuildItem>(true).Select(item=>item.gameObject)).Distinct().ToArray();
            ui.Bindings.RemoveAll(b=>b!=null&&b.Id!=null&&(b.Id.StartsWith("Build.Item.")||b.Id.StartsWith("Build.Cost.")));
            foreach(var old in oldItems)
            {
                ui.CameraGuards.RemoveAll(r=>r==null||r==old.transform||r.IsChildOf(old.transform));
                old.SetActive(false);Destroy(old);
            }
            bool layout=scroll.content.GetComponent<LayoutGroup>()!=null;float y=0;
            foreach(var data in catalog.Buildings)
            {
                var item=Instantiate(prefab,scroll.content,false);var go=item.gameObject;
                _runtimeBuildItems.Add(go);go.name="Build.Item."+data.Id;item.Data=data;
                item.NameText.text=data.DisplayName;item.CostText.text=$"골드 {data.GoldCost}";item.Icon.sprite=data.Icon;
                var rect=(RectTransform)item.transform;
                if(!layout){rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-y);y+=rect.rect.height+8;}
                go.SetActive(true);
                ui.Bindings.Add(new PrototypeUiBinding{Id="Build.Item."+data.Id,Rect=rect,Button=item.Button});
                ui.Bindings.Add(new PrototypeUiBinding{Id="Build.Cost."+data.Id,Rect=item.CostText.rectTransform,TmpText=item.CostText});
            }
            if(!layout&&y>scroll.content.rect.height)scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y);
            _buildItemsInitialized=true;
        }
        private void DisposeBuildItems()
        {
            foreach(var item in _runtimeBuildItems)if(item!=null)Destroy(item);
            _runtimeBuildItems.Clear();_buildItemsInitialized=false;
        }
    }
}
