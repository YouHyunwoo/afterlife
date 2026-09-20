using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool _showTools;private int _researchCategory;
        private static readonly Kind[] BuildKinds={Kind.House,Kind.Farm,Kind.Barracks,Kind.Research,Kind.Altar,Kind.ArrowTower,Kind.MagicTower,Kind.Wall};
        private bool DebugPanelBlocks(Vector2 screen)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _ready&&_showDebugPanel&&DebugPanelRect.Contains(new Vector2(screen.x,Screen.height-screen.y));
#else
            return false;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _showDebugPanel=true;
        private Rect DebugPanelRect=>new Rect(Mathf.Max(0,Screen.width-280),Mathf.Min(110,Mathf.Max(0,Screen.height-190)),270,180);

        private void OnGUI()
        {
            if(!_ready||!_showDebugPanel)return;
            var previousFont=GUI.skin.font;
            int previousDepth=GUI.depth;
            GUI.depth=-100;
            if(_font!=null)GUI.skin.font=_font;
            try
            {
                GUILayout.BeginArea(DebugPanelRect,GUI.skin.box);
                GUILayout.Label("디버그 패널 · F4 표시/숨김");
                GUILayout.Label($"골드: {_model.Money:0.##}");
                if(GUILayout.Button("+100 골드",GUILayout.Height(30)))_model.DebugAddGold();
                _model.DebugCursorBoost=GUILayout.Toggle(_model.DebugCursorBoost,"커서 기여량 ×100",GUILayout.Height(28));
                _model.DebugCursorSpeedBoost=GUILayout.Toggle(_model.DebugCursorSpeedBoost,"커서 기여 속도 ×100",GUILayout.Height(28));
                GUILayout.EndArea();
            }
            finally
            {
                GUI.skin.font=previousFont;
                GUI.depth=previousDepth;
            }
        }
#endif
    }
}
