#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private IEnumerator UguiCommandQa()
        {
            if(_sceneUi==null){QaCheck("scene canvas exists",false);yield break;}
            int count=_sceneUi.GetComponentsInChildren<RectTransform>(true).Length;
            QaCheck("serialized Canvas and EventSystem",_sceneUi.Canvas!=null&&EventSystem.current!=null);
            QaCheck("font assigned to every uGUI text",_sceneUi.GetComponentsInChildren<Text>(true).All(t=>t.font==_sceneUi.Font));
            QaCheck("all bindings unique",_sceneUi.Bindings.Select(b=>b.Id).Distinct().Count()==_sceneUi.Bindings.Count);
            _sceneUi.Item("HUD.Menu").Button.onClick.Invoke();yield return null;QaCheck("uGUI menu button opens menu",_modal==Modal.Menu);
            _sceneUi.Item("Menu.Resume").Button.onClick.Invoke();yield return null;QaCheck("uGUI close returns to game",_modal==Modal.None);
            _modal=Modal.Barracks;_modalBody=_model.Bodies.First(b=>b.Kind==Kind.Barracks).Id;yield return null;
            var b=_model.Find(_modalBody);int orders=b.Orders.Count;_sceneUi.Item("Recruit.Warrior").Button.onClick.Invoke();QaCheck("uGUI recruits warrior",b.Orders.Count==orders+1);
            _sceneUi.Item("Barracks.Cancel").Button.onClick.Invoke();QaCheck("uGUI cancels production",b.Orders.Count==orders);
            _modal=Modal.Research;_modalBody=_model.Bodies.First(x=>x.Kind==Kind.Research).Id;yield return null;
            int level=_model.Level(Upgrade.Pulse);_sceneUi.Item("Upgrade.Pulse").Button.onClick.Invoke();QaCheck("uGUI research purchases upgrade",_model.Level(Upgrade.Pulse)==level+1);
            _sceneUi.Item("Research.Tab.1").Button.onClick.Invoke();yield return null;QaCheck("uGUI research tabs",_researchCategory==1);yield return Shot("00-research-defense");
            _modal=Modal.Tower;_modalBody=_model.Bodies.First(x=>x.Kind==Kind.ArrowTower).Id;yield return null;b=_model.Find(_modalBody);level=b.TowerLevel;_sceneUi.Item("Tower.Upgrade").Button.onClick.Invoke();QaCheck("uGUI tower upgrade",b.TowerLevel==level+1);yield return Shot("00-tower");
            QaCheck("opening UI creates no UI objects",count==_sceneUi.GetComponentsInChildren<RectTransform>(true).Length);
            _model.QaSeed();RebuildVillageView();_researchCategory=0;_noticeTime=-10;yield return null;
        }
    }
}
#endif
