#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        public Body QaArtBody(Kind kind,Point point,int variant=0)
        {var b=Add(kind,point,100);b.Variant=variant;b.Stock=100;b.ChestGrade=(ChestGrade)(variant%3);if(kind==Kind.LargeTest)b.Width=2;TopologyRevision++;return b;}
        public void QaArtClear(){Bodies.Clear();Selected.Clear();TopologyRevision++;}
        public void QaOpenChest(Body body){Contributed?.Invoke(body);WorkChest(body,body.Health,FeedbackKind.Normal);}
        public void QaArtContribute(Body body){bool changed=false;ContributeBody(body,ref changed);}
    }
    public sealed partial class PrototypeView
    {
        private IEnumerator WorldArtQa()
        {
            while(!_ready)yield return null;yield return null;_showTools=false;_noticeTime=-10;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);
            _model.QaSeed();RebuildVillageView();_noticeTime=-10;
            yield return Shot("01-village-day");_model.QaPhase(Phase.Night);yield return Shot("02-village-night");_model.QaPhase(Phase.Day);
            _model.QaArtClear();RebuildVillageView();
            Kind[] buildings={Kind.House,Kind.Barracks,Kind.Research,Kind.Farm,Kind.LargeTest,Kind.Altar,Kind.Ruins,Kind.ArrowTower,Kind.MagicTower};
            for(int i=0;i<buildings.Length;i++)_model.QaArtBody(buildings[i],new Point(i%5*2.1f-4.2f,i/5*3.5f-1.75f));
            foreach(var p in new[]{new Point(-2,5),new Point(-1,5),new Point(0,5),new Point(0,4)})_model.QaArtBody(Kind.Wall,p);
            _model.UpdatePointer(new Point(0,0),true,false);_model.Settings.CursorVision=18;UpdateFog();Sync();
            _camera.orthographicSize=5.4f;yield return Shot("03-architecture");
            _camera.orthographicSize=2.7f;_camera.transform.position=new Vector3(10,15,-14)+new Vector3(-3,0,-1.75f);yield return Shot("04-house-market-detail");
            _camera.transform.position=new Vector3(10,15,-14)+new Vector3(2.8f,0,1.75f);yield return Shot("05-tower-detail");
            _camera.transform.position=new Vector3(10,15,-14)+new Vector3(-1,0,5);yield return Shot("05-wall-connections");
            QaCheck("wall corner mesh and arms present",_objects.Values.SelectMany(o=>o.GetComponentsInChildren<Transform>()).Any(t=>t.name=="WallCorner")&&_objects.Values.SelectMany(o=>o.GetComponentsInChildren<Transform>()).Count(t=>t.name=="WallArm")>=4);
            _model.QaArtClear();RebuildVillageView();
            Kind[] units={Kind.Warrior,Kind.Archer,Kind.Mage,Kind.Knight,Kind.Slime,Kind.Goblin,Kind.GoblinWarrior,Kind.GoblinShaman,Kind.Orc,Kind.Lizard};
            var actors=new List<Body>();for(int i=0;i<units.Length;i++)actors.Add(_model.QaArtBody(units[i],new Point(i%5*1.65f-3.3f,i/5*2.5f-1.25f)));
            _model.UpdatePointer(default,true,false);_camera.transform.position=new Vector3(-3,9,13);_camera.transform.LookAt(new Vector3(0,.35f,0));_camera.orthographicSize=3.5f;
            yield return Shot("06-unit-silhouettes");
            float nextCapture=Time.realtimeSinceStartup;int walkFrame=0;
            for(int frame=0;frame<90;frame++)
            {
                foreach(var b in actors){b.Position.Z+=Time.deltaTime*.60f;b.Facing=new Point(0,1);}
                yield return new WaitForEndOfFrame();
                if(Time.realtimeSinceStartup>=nextCapture&&walkFrame<12){var frameImage=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(_qaFolder,$"07-walk-{walkFrame++:00}.png"),frameImage.EncodeToPNG());Destroy(frameImage);nextCapture=Time.realtimeSinceStartup+.08f;}
            }
            foreach(var b in actors){b.Facing=new Point(0,1);b.PendingTarget=999;b.AttackWindup=.18f;}
            yield return Shot("08-attack-ready");foreach(var b in actors)b.AttackWindup=.09f;yield return Shot("09-attack-windup");
            foreach(var b in actors){b.PendingTarget=0;b.AttackRecovery=.18f;}yield return Shot("10-attack-impact");
            foreach(var b in actors)b.AttackRecovery=.09f;yield return Shot("11-attack-recovery");
            foreach(var b in actors){b.AttackRecovery=0;OnDamaged(b);}yield return Shot("12-hit-reaction");
            QaCheck("all new fighter pivots imported",actors.Where(b=>b.Kind!=Kind.Slime).All(b=>_worldRigs.TryGetValue(b.Id,out var rig)&&rig.Find("Grip")!=null&&rig.Find("LeftLeg")!=null));
            _model.QaArtClear();RebuildVillageView();
            for(int i=0;i<8;i++)_model.QaArtBody(i<4?Kind.Tree:Kind.Rock,new Point(i%4*1.8f-2.7f,i/4*2.2f-1.1f),i%4);
            _model.UpdatePointer(default,true,false);_camera.transform.position=new Vector3(-3,9,13);_camera.transform.LookAt(Vector3.up*.35f);_camera.orthographicSize=3.5f;yield return Shot("13-resource-variants");
            _model.QaArtClear();RebuildVillageView();var workTargets=new List<Body>();
            Kind[] workKinds={Kind.Tree,Kind.Rock,Kind.House,Kind.Slime,Kind.Treasure};
            for(int i=0;i<workKinds.Length;i++){var b=_model.QaArtBody(workKinds[i],new Point((i-2)*1.5f,0));if(b.Kind==Kind.House)b.Health=40;workTargets.Add(b);}
            _model.UpdatePointer(default,true,false);_camera.transform.position=new Vector3(-3,9,13);_camera.transform.LookAt(Vector3.up*.35f);yield return Shot("13-work-before");foreach(var b in workTargets)_model.QaArtContribute(b);
            for(int f=0;f<4;f++){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(_qaFolder,$"13-work-impact-{f:00}.png"),texture.EncodeToPNG());Destroy(texture);yield return new WaitForSecondsRealtime(.025f);}
            QaCheck("Blender tools linked to contribution",_tools.Count(t=>t.Root.GetComponentsInChildren<Transform>(true).Any(n=>n.name.EndsWith("Tool")))>=5);
            _model.QaArtClear();RebuildVillageView();for(int i=0;i<4;i++)_model.QaArtBody(Kind.Animal,new Point((i-1.5f)*1.6f,0),i);
            _model.UpdatePointer(default,true,false);_camera.transform.position=new Vector3(-3,7,11);_camera.transform.LookAt(Vector3.up*.3f);_camera.orthographicSize=2.6f;yield return Shot("13-animals");
            QaCheck("animal face and tail articulation",_animals.Values.Where(r=>r!=null).Count(r=>r.Head!=null&&r.Tail!=null)==4);
            _model.QaArtClear();RebuildVillageView();
            var chests=new List<Body>();for(int i=0;i<3;i++)chests.Add(_model.QaArtBody(Kind.Chest,new Point((i-1)*1.3f,0),i));
            _camera.transform.position=new Vector3(-2,5,8);_camera.transform.LookAt(new Vector3(0,.2f,0));_camera.orthographicSize=2.1f;_model.UpdatePointer(default,true,false);
            yield return Shot("14-chest-grades");foreach(var b in chests)_model.QaOpenChest(b);
            // Capture actual consecutive animation frames with known realtime offsets.
            var frames=new List<Texture2D>();var times=new List<string>();float start=Time.realtimeSinceStartup;
            for(int f=0;f<20;f++){yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add($"{f}: {Time.realtimeSinceStartup-start:0.000}");yield return new WaitForSecondsRealtime(.045f);}
            for(int f=0;f<frames.Count;f++){File.WriteAllBytes(Path.Combine(_qaFolder,$"15-chest-opening-{f:00}.png"),frames[f].EncodeToPNG());Destroy(frames[f]);}
            File.WriteAllLines(Path.Combine(_qaFolder,"chest-times.txt"),times);
            QaCheck("chests paid once and exhausted",chests.All(b=>b.ChestOpened&&!b.Alive));
            _model.QaSeed();RebuildVillageView();_model.Settings.CursorVision=9;_camera.orthographicSize=5;
            foreach(var place in new[]{new Point(-12,-9),new Point(10,-6),new Point(29,0),new Point(0,30)})
            {_model.UpdatePointer(place,true,false);_camera.transform.position=V(place)+new Vector3(10,15,-14);_camera.transform.LookAt(V(place));yield return Shot($"16-biome-{place.X}-{place.Z}");}
            QaCheck("no resource spawned on water",_model.Bodies.Where(b=>b.Kind==Kind.Tree||b.Kind==Kind.Rock).All(b=>_model.Terrain.Land(b.Position)));
            QaCheck("biome chunks contain geometry",_environmentMeshes.Count(m=>m!=null&&m.name.StartsWith("Biome")&&m.vertexCount>0)>10);
            // Real model tick / target selection / projectiles, not the static pose catalogue above.
            _model.QaSeed();RebuildVillageView();_noticeTime=-10;_camera.orthographicSize=5.8f;_camera.transform.position=new Vector3(10,15,-14);_camera.transform.LookAt(Vector3.zero);
            _model.Settings.DaySeconds=3600;_model.Settings.CursorVision=7;_model.QaPhase(Phase.Day);_modal=Modal.None;
            foreach(var kind in new[]{Kind.Archer,Kind.Mage,Kind.Knight})_model.QaArtBody(kind,new Point((int)kind-11,2));
            foreach(var kind in new[]{Kind.Slime,Kind.Goblin,Kind.GoblinWarrior,Kind.GoblinShaman,Kind.Orc,Kind.Lizard}){var b=_model.QaArtBody(kind,new Point(((int)kind%3-1)*2,6+(int)kind%2));b.Health=b.MaxHealth=220;}
            _qaActive=false;
            for(int frame=0;frame<14;frame++){yield return new WaitForSecondsRealtime(.40f);yield return Shot($"17-live-combat-{frame:00}");}
            _qaActive=true;
            QaCheck("combat causes damage",_model.Bodies.Any(b=>b.Unit&&b.Health<b.MaxHealth)||_model.Bodies.Count(b=>b.Hostile)<6);
            File.WriteAllLines(Path.Combine(_qaFolder,"checks.txt"),_qaChecks);File.WriteAllText(Path.Combine(_qaFolder,"complete.txt"),DateTime.UtcNow.ToString("O"));Application.Quit(_qaChecks.Any(c=>c.StartsWith("FAIL"))?1:0);
        }
    }
}
#endif
