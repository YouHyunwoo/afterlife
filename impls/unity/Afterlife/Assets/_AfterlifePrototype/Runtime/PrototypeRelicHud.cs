using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private readonly Dictionary<RelicKind,RectTransform> _relicItems=new Dictionary<RelicKind,RectTransform>();
        private readonly List<RelicKind> _removedRelics=new List<RelicKind>();
        private PrototypeRelicCatalog _relicCatalog;
        private RectTransform _relicRoot;
        private void InitializeRelicHud()
        {
            _model.RelicsChanged-=SyncRelicHud;_model.RelicsChanged+=SyncRelicHud;
            _relicCatalog=_sceneUi.RelicCatalog!=null?_sceneUi.RelicCatalog:Resources.Load<PrototypeRelicCatalog>(PrototypeRelicCatalog.ResourcePath);
            _relicRoot=_sceneUi.RelicRoot;
            // 빈 목록도 연결할 수 있어야 한다. 삭제 가능한 옛 아이콘에 의존하지 않는다.
            if(_relicRoot==null)
            {
                foreach(var rect in _sceneUi.GetComponentsInChildren<RectTransform>(true))
                {
                    if(rect.parent==null||rect.parent.name!="Header"||(rect.name!="Relics"&&rect.name!="Relic"))continue;
                    if(_relicRoot!=null)
                    {
                        Debug.LogError("유물 HUD: Header 아래 유물 컨테이너가 여러 개입니다. PrototypeCanvas의 Relic Root를 지정하세요.",_sceneUi);
                        _relicRoot=null;return;
                    }
                    _relicRoot=rect;
                }
            }
            foreach(var binding in _sceneUi.Bindings)
            {
                if(binding==null||binding.Id==null||!binding.Id.StartsWith("Relic.")||binding.Rect==null)continue;
                if(_relicRoot==null)_relicRoot=binding.Rect.parent as RectTransform;
                binding.Rect.gameObject.SetActive(false);
            }
            if(_relicCatalog==null)Debug.LogError("유물 HUD: Relic Catalog가 없습니다. Resources/Data/RelicCatalog 또는 PrototypeCanvas 참조를 확인하세요.",_sceneUi);
            else if(_relicCatalog.ListItemPrefab==null)Debug.LogError("유물 HUD: RelicCatalog의 List Item Prefab이 비어 있습니다.",_relicCatalog);
            if(_relicRoot==null)Debug.LogError("유물 HUD: Relic Root가 없습니다. PrototypeCanvas에 지정하거나 Header 아래 Relics 컨테이너를 두세요.",_sceneUi);
            SyncRelicHud();
        }
        private void SyncRelicHud()
        {
            if(_relicCatalog==null||_relicCatalog.ListItemPrefab==null||_relicRoot==null)return;
            _removedRelics.Clear();
            foreach(var pair in _relicItems)if(!_model.Relics.Contains(pair.Key))_removedRelics.Add(pair.Key);
            foreach(var id in _removedRelics)
            {
                var item=_relicItems[id];if(item!=null){item.gameObject.SetActive(false);Destroy(item.gameObject);}
                _relicItems.Remove(id);
            }
            foreach(var id in _model.Relics)
            {
                if(_relicItems.ContainsKey(id))continue;
                var data=_relicCatalog.Find(id);if(data==null)continue;
                var item=Instantiate(_relicCatalog.ListItemPrefab,_relicRoot,false);item.name="Relic "+id;
                var icon=item.GetComponentInChildren<Image>(true);
                if(icon!=null){icon.sprite=data.Icon;icon.raycastTarget=true;}
                item.gameObject.SetActive(true);_relicItems.Add(id,item);
            }
        }
        private string RelicTooltip(Vector2 mouse)
        {
            foreach(var pair in _relicItems)
            {
                if(pair.Value==null||!pair.Value.gameObject.activeInHierarchy||!_model.Relics.Contains(pair.Key))continue;
                if(RectTransformUtility.RectangleContainsScreenPoint(pair.Value,mouse))
                    return _relicCatalog.Find(pair.Key)?.Tooltip(_model.RelicLevel(pair.Key),_model.Rules.Relics)??"";
            }
            return "";
        }
    }
}
