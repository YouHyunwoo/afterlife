using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool _showTools;private int _researchCategory;
        private static readonly Kind[] BuildKinds={Kind.House,Kind.Farm,Kind.Barracks,Kind.ArcheryRange,Kind.KnightHall,Kind.MageTower,Kind.Altar,Kind.ArrowTower,Kind.MagicTower,Kind.Wall};
        private bool DebugPanelBlocks(Vector2 screen)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _ready&&_showDebugPanel&&DebugPanelRect.Contains(new Vector2(screen.x,Screen.height-screen.y));
#else
            return false;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _showDebugPanel=false;
        private const float DebugPanelScale=2f;
        private const float DebugPanelHeight=220f;
        private Rect DebugPanelRect=>new Rect(Mathf.Max(0,Screen.width-270*DebugPanelScale-10),Mathf.Min(110,Mathf.Max(0,Screen.height-DebugPanelHeight*DebugPanelScale-10)),270*DebugPanelScale,DebugPanelHeight*DebugPanelScale);

        private void OnGUI()
        {
            if(!_ready||!_showDebugPanel)return;
            var previousFont=GUI.skin.font;
            var previousMatrix=GUI.matrix;
            int previousDepth=GUI.depth;
            bool previousEnabled=GUI.enabled;
            GUI.depth=-100;
            if(_font!=null)GUI.skin.font=_font;
            try
            {
                var panel=DebugPanelRect;
                GUI.matrix=Matrix4x4.TRS(new Vector3(panel.x,panel.y,0),Quaternion.identity,new Vector3(DebugPanelScale,DebugPanelScale,1));
                GUILayout.BeginArea(new Rect(0,0,270,DebugPanelHeight),GUI.skin.box);
                GUILayout.Label("디버그 패널 · F4 표시/숨김");
                GUILayout.Label("지도 시드: "+_model.Terrain.Seed);
                GUILayout.Label($"골드: {_model.Money:0.##}");
                if(GUILayout.Button("+100 골드",GUILayout.Height(30)))_model.DebugAddGold();
                _model.DebugCursorBoost=GUILayout.Toggle(_model.DebugCursorBoost,"커서 기여량 ×100",GUILayout.Height(28));
                _model.DebugCursorSpeedBoost=GUILayout.Toggle(_model.DebugCursorSpeedBoost,"커서 기여 속도 ×100",GUILayout.Height(28));
                string skipLabel=_model.Phase==Phase.Day?"시간 스킵: 낮 → 황혼":
                    _model.Phase==Phase.Dusk?"시간 스킵: 황혼 → 밤":
                    _model.Phase==Phase.Night?"시간 스킵: 밤 → 정산":"시간 스킵: 전환 불가";
                GUI.enabled=previousEnabled&&_model.CanDebugSkipPhase;
                if(GUILayout.Button(skipLabel,GUILayout.Height(30)))_model.DebugSkipPhase();
                GUI.enabled=previousEnabled;
                GUILayout.EndArea();
            }
            finally
            {
                GUI.skin.font=previousFont;
                GUI.matrix=previousMatrix;
                GUI.depth=previousDepth;
                GUI.enabled=previousEnabled;
            }
        }
#endif
    }
}
