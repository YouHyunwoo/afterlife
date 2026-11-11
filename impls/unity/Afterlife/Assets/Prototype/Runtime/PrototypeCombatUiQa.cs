#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        public void QaEnemyRules(Action<string,bool> check)
        {
            Bodies.Clear();var enemy=Add(Kind.Goblin,new Point(0,0),36);enemy.Raider=true;
            var house=AddBuilding(Kind.House,1,0,true);var farWall=AddBuilding(Kind.Wall,15,0,true);
            var tower=AddBuilding(Kind.ArrowTower,5,0,true);
            check("visible tower outranks closer house and unseen wall",EnemyTarget(enemy)==tower);
            var troop=Add(Kind.Warrior,new Point(2,0),90);
            check("nearest high priority troop selected",EnemyTarget(enemy)==troop);
            troop.Health=0;tower.Health=0;
            check("visible ordinary building is fallback",EnemyTarget(enemy)==house);
            house.Health=0;check("out of vision targets ignored",EnemyTarget(enemy)==null);
            Money=0;Loot.Clear();ApplyDamage(enemy,100);
            check("kill gives immediate coins and both goblin items",Money==3&&Loot[LootKind.Club]==1&&Loot[LootKind.Bone]==1);
            ApplyDamage(enemy,100);check("dead target cannot grant duplicate reward",Money==3&&Loot[LootKind.Club]==1);
            QaSeed();
        }
    }
    public sealed partial class PrototypeView
    {
        private IEnumerator CombatUiQa()
        {
            while(!_ready)yield return null;yield return null;
            _model.QaEnemyRules(QaCheck);RebuildVillageView();_noticeTime=-10;
            yield return UguiCommandQa();yield return Shot("01-day-territory");
            _showTools=true;yield return Shot("02-construction-icons");_buildScroll=new Vector2(0,650);if(_sceneUi!=null)_sceneUi.Item("Build").Rect.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;yield return Shot("02-defense-icons");_buildScroll=Vector2.zero;_showTools=false;
            foreach(var m in new[]{Modal.Loot,Modal.Barracks,Modal.Research,Modal.Menu,Modal.Altar,Modal.Settlement,Modal.RebirthShop})
            {
                _modal=m;_modalBody=_model.Bodies.First(b=>b.Kind==(m==Modal.Barracks?Kind.Barracks:Kind.Research)).Id;
                yield return Shot("03-"+m);
                if(_sceneUi!=null){foreach(var sc in _sceneUi.Item("Modal."+m).Rect.GetComponentsInChildren<UnityEngine.UI.ScrollRect>())sc.verticalNormalizedPosition=0;yield return Shot("03-"+m+"-bottom");}
            }
            _modal=Modal.None;_model.QaPhase(Phase.Night);yield return Shot("04-night-moon");
            _model.QaPhase(Phase.Victory);yield return Shot("05-result");_model.QaPhase(Phase.Day);yield return new WaitForEndOfFrame();
            foreach(var p in new[]{new Vector2(BuildToggle.xMax+20,BuildToggle.center.y),new Vector2(100,120),new Vector2(MenuButton.xMin-20,40)})
            {
                var screen=new Vector2(p.x*HudScale,Screen.height-p.y*HudScale);var before=_camera.transform.position;
                UpdateCamera(screen,false,true);QaCheck("camera stationary near UI "+p,Vector3.Distance(before,_camera.transform.position)<.0001f);
            }
            var position=_camera.transform.position;UpdateCamera(new Vector2(8,Screen.height*.3f),false,true);
            QaCheck("camera still scrolls outside UI guard",Vector3.Distance(position,_camera.transform.position)>0);
            QaCheck("information HUD keeps world click through",!OverUi(new Vector2(180,50)*HudScale));
            File.WriteAllLines(Path.Combine(_qaFolder,"checks.txt"),_qaChecks);
            File.WriteAllText(Path.Combine(_qaFolder,"complete.txt"),DateTime.UtcNow.ToString("O"));
            Application.Quit(_qaChecks.Any(x=>x.StartsWith("FAIL"))?1:0);
        }
    }
}
#endif
