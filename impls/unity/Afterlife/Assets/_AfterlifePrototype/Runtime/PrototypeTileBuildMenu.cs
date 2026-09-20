using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool _tileBuildMenu;
        private bool _buildMenuClosing;
        private float _buildMenuTime;
        private const float BuildMarkerSeconds=.13f,BuildPanelSeconds=.12f;
        private CanvasGroup _buildMenuGroup;
        private Color _buildWorldOriginalColor;
        [SerializeField,Range(0,1)] private float _buildBackgroundDim=.45f;
        private GameObject _buildTileMarker;
        private Mesh _buildTileMarkerMesh;
        private MeshRenderer _buildTileMarkerRenderer;
        private Point _buildTile;
        private Vector2 _buildMenuScreen;
        private Vector2 _buildMenuPreferredSize;
        private ScrollRect _tileBuildScroll;
        private readonly List<System.Action> _restoreBuildLayout=new List<System.Action>();
        private void RememberBuildLayout(RectTransform panel)
        {
            var r=panel;var min=r.anchorMin;var max=r.anchorMax;var pivot=r.pivot;var pos=r.anchoredPosition;var size=r.sizeDelta;bool active=r.gameObject.activeSelf;
            _restoreBuildLayout.Add(()=>{if(r==null)return;r.anchorMin=min;r.anchorMax=max;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;r.gameObject.SetActive(active);});
        }
        private static void BuildRect(RectTransform r,float x,float y,float width,float height)
        {if(r==null)return;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        private void OpenTileBuildMenu(Vector2 screen)
        {
            CloseTileBuildMenu(true);
            int tileX=Mathf.RoundToInt(_pressWorld.X),tileZ=Mathf.RoundToInt(_pressWorld.Z);
            if(_model.BuildTileError(tileX,tileZ)!=null){PlaySound(SoundCue.Error);return;}
            var panel=_sceneUi?.Item("Build")?.Rect;if(panel==null)return;
            _tileBuildScroll=panel.GetComponentInChildren<ScrollRect>(true);if(_tileBuildScroll==null)return;
            RememberBuildLayout(panel);
            _buildMenuPreferredSize=panel.rect.size;
            var worldDisplay=_sceneUi.WorldDisplay;
            if(worldDisplay!=null)
            {
                _buildWorldOriginalColor=worldDisplay.color;
                Color original=_buildWorldOriginalColor;
                _restoreBuildLayout.Add(()=>{if(worldDisplay!=null)worldDisplay.color=original;});
            }
            _buildMenuGroup=panel.GetComponent<CanvasGroup>();
            if(_buildMenuGroup==null)_buildMenuGroup=panel.gameObject.AddComponent<CanvasGroup>();
            var group=_buildMenuGroup;float alpha=group.alpha;bool interactable=group.interactable,blocks=group.blocksRaycasts;
            _restoreBuildLayout.Add(()=>{if(group==null)return;group.alpha=alpha;group.interactable=interactable;group.blocksRaycasts=blocks;});
            _buildMenuTime=0;_buildMenuClosing=false;group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
            EnsureBuildTileMarker();
            var scroll=_tileBuildScroll;
            _restoreBuildLayout.Add(()=>{if(scroll!=null)scroll.StopMovement();});
            _buildTile=new Point(tileX,tileZ);
            _buildMenuScreen=screen;_tileBuildMenu=true;_showTools=true;scroll.StopMovement();
            PlaySound(SoundCue.Open);
            UpdateTileBuildMenu(false);scroll.verticalNormalizedPosition=1;_uiRefresh=0;
        }
        private void UpdateTileBuildMenu(bool advance=true)
        {
            if(!_tileBuildMenu)return;
            if(_modal!=Modal.None||_model.Ended||_model.Frozen){CloseTileBuildMenu(true);return;}
            if(advance)_buildMenuTime=Mathf.MoveTowards(_buildMenuTime,_buildMenuClosing?0:BuildMarkerSeconds+BuildPanelSeconds,Time.unscaledDeltaTime);
            if(_buildMenuClosing&&_buildMenuTime<=0){CloseTileBuildMenu(true);return;}
            float markerProgress=Mathf.SmoothStep(0,1,Mathf.Clamp01(_buildMenuTime/BuildMarkerSeconds));
            float menuProgress=Mathf.SmoothStep(0,1,Mathf.Clamp01((_buildMenuTime-BuildMarkerSeconds)/BuildPanelSeconds));
            float backgroundProgress=Mathf.SmoothStep(0,1,Mathf.Clamp01(_buildMenuTime/(BuildMarkerSeconds+BuildPanelSeconds)));
            if(_sceneUi.WorldDisplay!=null)
            {
                float brightness=1-_buildBackgroundDim*backgroundProgress;
                var original=_buildWorldOriginalColor;
                _sceneUi.WorldDisplay.color=new Color(original.r*brightness,original.g*brightness,original.b*brightness,original.a);
            }
            _buildMenuGroup.alpha=menuProgress;
            _buildMenuGroup.interactable=!_buildMenuClosing&&menuProgress>=.99f;
            _buildMenuGroup.blocksRaycasts=menuProgress>0;
            _buildTileMarker.SetActive(markerProgress>0);
            _buildTileMarker.transform.position=V(_buildTile,Mathf.Lerp(.04f,.48f,markerProgress));
            // 표시가 완료되면 계속 회전하되 취소 중에는 같은 방향을 역으로 되돌린다.
            if(advance)_buildTileMarker.transform.Rotate(Vector3.up,100*Time.unscaledDeltaTime*(_buildMenuClosing?-1:1),Space.World);
            _block.Clear();_block.SetColor("_BaseColor",new Color(_gold.r,_gold.g,_gold.b,markerProgress));_block.SetFloat("_Overlay",.6f);
            _buildTileMarkerRenderer.SetPropertyBlock(_block);
            var tileScreen=_camera.WorldToViewportPoint(V(_buildTile,.75f));
            _buildMenuScreen=new Vector2(tileScreen.x*Screen.width,tileScreen.y*Screen.height);
            var panel=_sceneUi.Item("Build").Rect;var parent=panel.parent as RectTransform;
            var canvas=_sceneUi.Canvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,_buildMenuScreen,camera,out var local);
            // 자식 배치는 씬/프리팹의 앵커와 레이아웃이 관리한다. 루트만 화면 안으로 이동한다.
            float width=Mathf.Min(_buildMenuPreferredSize.x,Mathf.Max(1,parent.rect.width-16)),height=Mathf.Min(_buildMenuPreferredSize.y,Mathf.Max(1,parent.rect.height-16));
            float x=Mathf.Clamp(local.x-parent.rect.xMin-width*.5f,8,Mathf.Max(8,parent.rect.width-width-8));
            float y=Mathf.Clamp(parent.rect.yMax-local.y-height-12,8,Mathf.Max(8,parent.rect.height-height-8));
            BuildRect(panel,x,Mathf.Min(y+(1-menuProgress)*18,parent.rect.height-height-8),width,height);
        }
        private void EnsureBuildTileMarker()
        {
            if(_buildTileMarker!=null){_buildTileMarker.transform.rotation=Quaternion.identity;return;}
            _buildTileMarker=new GameObject("Build tile marker");_buildTileMarker.transform.SetParent(transform,false);
            // 면별 정점을 분리해 사각뿔 모서리를 유지한다. 월드 선택을 방해하지 않도록 콜라이더는 없다.
            var tip=Vector3.zero;
            var a=new Vector3(-.22f,.42f,-.22f);var b=new Vector3(.22f,.42f,-.22f);
            var c=new Vector3(.22f,.42f,.22f);var d=new Vector3(-.22f,.42f,.22f);
            _buildTileMarkerMesh=new Mesh{name="Downward square pyramid"};
            _buildTileMarkerMesh.vertices=new[]{tip,b,a,tip,c,b,tip,d,c,tip,a,d,a,b,c,a,c,d};
            _buildTileMarkerMesh.triangles=new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17};
            _buildTileMarkerMesh.RecalculateNormals();_buildTileMarkerMesh.RecalculateBounds();
            _buildTileMarker.AddComponent<MeshFilter>().sharedMesh=_buildTileMarkerMesh;
            _buildTileMarkerRenderer=_buildTileMarker.AddComponent<MeshRenderer>();_buildTileMarkerRenderer.sharedMaterial=_surface;
            _buildTileMarkerRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            _buildTileMarkerRenderer.receiveShadows=false;
        }
        private void DisposeBuildTileMenu()
        {
            CloseTileBuildMenu(true);
            if(_buildTileMarker!=null)Destroy(_buildTileMarker);
            if(_buildTileMarkerMesh!=null)Destroy(_buildTileMarkerMesh);
        }
        private void CloseTileBuildMenu(bool immediate=false,bool sound=true)
        {
            if(!_tileBuildMenu)return;
            if(!immediate&&!_buildMenuClosing&&sound)PlaySound(SoundCue.Cancel);
            if(!immediate){_buildMenuClosing=true;_buildMenuGroup.interactable=false;return;}
            _buildMenuClosing=false;_buildMenuTime=0;
            if(_buildTileMarker!=null)_buildTileMarker.SetActive(false);
            if(_hoverRing!=null)_hoverRing.enabled=false;
            _tileBuildMenu=false;foreach(var restore in _restoreBuildLayout)restore();_restoreBuildLayout.Clear();
            _showTools=false;if(_sceneUi!=null)_sceneUi.Show("Build",false);_uiRefresh=0;
        }
    }
}
