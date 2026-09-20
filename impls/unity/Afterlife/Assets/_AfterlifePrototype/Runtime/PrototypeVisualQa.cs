#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Afterlife.Prototype
{
    // 명시적 --ui-qa 실행에서만 사용하는 실제 플레이어 화면 검증 도구. 일반 플레이에는 진입하지 않는다.
    public sealed partial class VillageModel
    {
        public void QaSeed()
        {
            Reset();Money=2345;Etheron=36;Wave=3;SegmentCleared=2;SegmentKills=87;Remaining=Settings.DaySeconds;
            Bodies.RemoveAll(b=>!b.Building&&Math.Abs(b.Position.X)<7&&Math.Abs(b.Position.Z)<7);
            Kind[] kinds={Kind.Barracks,Kind.Research,Kind.Altar,Kind.ArrowTower,Kind.MagicTower,Kind.Farm,Kind.Wall};
            for(int i=0;i<kinds.Length;i++)AddBuilding(kinds[i],(i%4)*2-3,(i/4)*3-4,true);
            foreach(RelicKind r in Enum.GetValues(typeof(RelicKind)))Relics.Add(r);
            for(int i=0;i<4;i++)Add(Kind.Warrior,new Point(i-2,3),90);
            foreach(LootKind loot in Enum.GetValues(typeof(LootKind)))Loot[loot]=12+(int)loot*3;Valuables=7;
            LastLootSummary="슬라임 진액 23 · 몽둥이 12 · 뼈 18 · 갑주 7 · 지팡이 4 · 팔보호대 3 · 리자드 꼬리 2";LastValuables=17;LastSettlement=356;
            Recalculate();TopologyRevision++;
        }
        public void QaPhase(Phase phase){Phase=phase;Remaining=phase==Phase.Night?Settings.NightSeconds:Settings.DaySeconds;if(Ended)LastResult=new RunResult{Victory=phase==Phase.Victory,BestCleared=phase==Phase.Victory?Settings.Days:3,Rebirths=2,Kills=287};}
        public void QaFunds(float value){Money=value;}
        public void QaClearValuables(){Valuables=0;}
        public int QaAddHeaderProbe(Point position){var b=AddBuilding(Kind.House,(int)Math.Round(position.X),(int)Math.Round(position.Z),true);Recalculate();TopologyRevision++;return b.Id;}
    }
    public sealed partial class PrototypeView
    {
        private bool _qaActive;
        private string _qaFolder;
        private readonly List<string> _qaChecks=new List<string>();
        private void Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--ui-qa");
            int combat=Array.IndexOf(args,"--combat-ui-qa");if(combat>=0&&combat+1<args.Length){_qaFolder=Path.GetFullPath(args[combat+1]);Directory.CreateDirectory(_qaFolder);_qaActive=true;StartCoroutine(CombatUiQa());return;}
            int world=Array.IndexOf(args,"--world-qa");if(world>=0&&world+1<args.Length){_qaFolder=Path.GetFullPath(args[world+1]);Directory.CreateDirectory(_qaFolder);_qaActive=true;StartCoroutine(WorldArtQa());return;}
            int live=Array.IndexOf(args,"--ui-qa-live");if(live>=0&&live+1<args.Length){_qaFolder=Path.GetFullPath(args[live+1]);Directory.CreateDirectory(_qaFolder);StartCoroutine(LiveQa());return;}
            if(i<0||i+1>=args.Length)return;_qaFolder=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(_qaFolder);_qaActive=true;StartCoroutine(VisualQa());
        }
        private IEnumerator LiveQa()
        {
            while(!_ready)yield return null;yield return null;_model.QaSeed();RebuildVillageView();_noticeTime=-10;
            if(MouseWorld(new Vector2(565,Screen.height-148),out var probe))_model.QaAddHeaderProbe(probe);
            _model.Settings.DaySeconds=3600;_model.QaPhase(Phase.Day);
            while(true)
            {
                string request=Path.Combine(_qaFolder,"capture.txt");
                if(File.Exists(request))
                {
                    string name=File.ReadAllText(request).Trim();File.Delete(request);
                    yield return Shot(name);
                    File.WriteAllText(Path.Combine(_qaFolder,name+".txt"),$"modal={_modal}\ntools={_showTools}\nmoney={_model.Money}\nremaining={_model.Remaining}\nselected={string.Join(",",_model.Selected)}\ninspected={_inspected}\nmode={_model.Mode}\nuiSize={_uiSize}\nmoving={_model.Bodies.Count(b=>b.MovingOut)}\ncameraOrbiting={_orbiting}\ncamera={_camera.transform.position}\nmodalScroll={_modalScroll}\nbuildScroll={_buildScroll}\nloot={_model.PendingLootMoney+_model.Valuables*5}\n");
                }
                yield return new WaitForSecondsRealtime(.15f);
            }
        }
        private void QaCheck(string name,bool pass){_qaChecks.Add((pass?"PASS ":"FAIL ")+name);Debug.Log("UI_QA "+_qaChecks.Last());}
        private IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(_qaFolder,name+".png"),texture.EncodeToPNG());Destroy(texture);
        }
        private IEnumerator VisualQa()
        {
            yield return null;while(!_ready)yield return null;_uiSize=1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);
            _model.QaSeed();RebuildVillageView();_noticeTime=-10;yield return Shot("01-hud-1080");
            QaCheck("header information passes world input",!OverUi(new Vector2(180,50)*HudScale));
            QaCheck("footer world input is free",!OverUi(new Vector2(HudWidth/2,HudHeight-10)*HudScale));
            QaCheck("actual menu button blocks world input",OverUi(MenuButton.center*HudScale));
            var building=_model.Bodies.First(b=>b.Kind==Kind.Research);_pressBody=building.Id;_dragging=false;_inspected=0;ReleasePointer(Vector2.zero);QaCheck("single click opens research without prior selection",_modal==Modal.Research);CloseModal();
            _showTools=true;QaCheck("open build panel blocks world input",OverUi(ToolsRect.center*HudScale));yield return Shot("02-construction");_buildScroll=new Vector2(0,700);yield return Shot("03-construction-bottom");_buildScroll=Vector2.zero;_showTools=false;
            QaCheck("construction never overlaps side navigation",ToolsRect.xMin>BuildToggle.xMax&&ToolsRect.xMin>LootToggle.xMax);
            QaCheck("loot icon blocks world input",OverUi(LootToggle.center*HudScale));
            QaCheck("closed construction area passes world input",!OverUi(ToolsRect.center*HudScale));
            Modal[] modals={Modal.Loot,Modal.Research,Modal.Barracks,Modal.Research,Modal.Tower,Modal.Altar,Modal.Menu};
            foreach(var m in modals)
            {
                _modal=m;Kind kind=m==Modal.Barracks?Kind.Barracks:m==Modal.Research?Kind.Research:m==Modal.Tower?Kind.ArrowTower:Kind.Altar;_modalBody=_model.Bodies.First(b=>b.Kind==kind).Id;
                yield return Shot("04-"+m);
                _modalScroll=new Vector2(0,2000);yield return Shot("05-"+m+"-bottom");_modalScroll=Vector2.zero;
                if(m==Modal.Research){_researchCategory=1;yield return Shot("06-research-defense");_researchCategory=2;yield return Shot("07-research-fire");_researchCategory=0;}
            }
            _model.Loot.Clear();_modal=Modal.Loot;_modalBody=0;yield return Shot("07-loot-valuables-only");
            _model.QaClearValuables();yield return Shot("07-loot-empty");
            _modal=Modal.Barracks;_modalBody=_model.Bodies.First(b=>b.Kind==Kind.Barracks).Id;_model.QaFunds(0);yield return Shot("08-unaffordable");_model.QaFunds(2345);
            foreach(var b in _model.Bodies.Where(b=>b.Kind==Kind.Altar||b.Kind==Kind.Research))b.Health=0;
            yield return Shot("08-locked-recruit");
            _model.QaSeed();RebuildVillageView();_modal=Modal.Research;_modalBody=_model.Bodies.First(b=>b.Kind==Kind.Research).Id;_model.QaFunds(50000);
            var upgrade=VillageModel.ResearchUpgrades.First();for(int level=0;level<5;level++)_model.BuyUpgrade(upgrade);
            QaCheck("research maximum level state",_model.Level(upgrade)==_model.UpgradeMax(upgrade));yield return Shot("08-research-max");_model.QaFunds(2345);
            _model.QaPhase(Phase.Settlement);_modal=Modal.Settlement;yield return Shot("09-settlement");
            _model.QaPhase(Phase.Rebirth);_modal=Modal.RebirthShop;yield return Shot("10-rebirth");_modalScroll=new Vector2(0,2000);yield return Shot("11-rebirth-bottom");
            _model.QaPhase(Phase.Victory);_modal=Modal.None;yield return Shot("12-victory");_model.QaPhase(Phase.Defeat);yield return Shot("13-defeat");
            _model.QaPhase(Phase.Night);_modal=Modal.None;_model.UpdatePointer(new Point(8,3),true,false);yield return Shot("14-night");
            _model.QaPhase(Phase.Day);_modal=Modal.None;yield return Shot("15-day-alpha");
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2560,1440),new Vector2Int(2560,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);_modal=Modal.None;_showTools=true;yield return Shot("16-hud-"+size.x+"x"+size.y);_showTools=false;
                _modal=Modal.Research;_modalBody=_model.Bodies.First(b=>b.Kind==Kind.Research).Id;yield return Shot("17-research-"+size.x+"x"+size.y);_modalScroll=new Vector2(0,2000);yield return Shot("18-research-bottom-"+size.x+"x"+size.y);
            }
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);
            foreach(float scale in new[]{.8f,1.2f})
            {_uiSize=scale;_modal=Modal.Menu;yield return Shot("19-menu-scale-"+scale.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));_modal=Modal.None;_showTools=true;yield return Shot("20-construction-scale-"+scale.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}
            _uiSize=1;
            _showTools=false;_modal=Modal.None;yield return Shot("21-transition-before");
            _modal=Modal.Menu;_modalBody=0;
            var frames=new List<Texture2D>();var times=new List<string>();float start=Time.realtimeSinceStartup;
            for(int f=0;f<12;f++){yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add($"{f}: {Time.realtimeSinceStartup-start:0.000}s");}
            for(int f=0;f<frames.Count;f++){File.WriteAllBytes(Path.Combine(_qaFolder,$"22-transition-{f:00}.png"),frames[f].EncodeToPNG());Destroy(frames[f]);}
            File.WriteAllLines(Path.Combine(_qaFolder,"transition-times.txt"),times);
            _modal=Modal.None;_showTools=false;Vector3 before=_camera.transform.position;UpdateCamera(new Vector2(Screen.width*.20f,Screen.height*.5f),false,true);QaCheck("camera moves inside 25 percent edge band",Vector3.Distance(before,_camera.transform.position)>0);
            before=_camera.transform.position;UpdateCamera(new Vector2(Screen.width*.5f,Screen.height*.5f),false,true);QaCheck("camera stops in central rectangle",Vector3.Distance(before,_camera.transform.position)<.0001f);
            File.WriteAllLines(Path.Combine(_qaFolder,"checks.txt"),_qaChecks);File.WriteAllText(Path.Combine(_qaFolder,"complete.txt"),"Rendered actual standalone player UI screenshots. "+DateTime.UtcNow.ToString("O"));Application.Quit(_qaChecks.Any(x=>x.StartsWith("FAIL"))?1:0);
        }
    }
}
#endif
