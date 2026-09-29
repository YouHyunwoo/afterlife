using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool _tileBuildMenu;
        private float _buildMenuProgress;
        private const float BuildPanelSeconds=.24f;
        private CanvasGroup _buildMenuGroup;
        private Kind? _selectedBuildKind;
        [SerializeField] private GameObject _buildTileMarker;
        private Mesh _buildTileMarkerMesh;
        private MeshRenderer _buildTileMarkerRenderer;
        private Point _buildTile;
        private Vector2 _buildMenuPreferredSize;
        private static void BuildRect(RectTransform r,float x,float y,float width,float height)
        {if(r==null)return;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        private void UpdateTileBuildMenu()
        {
            bool visible=_model.Mode==InputMode.Build&&_modal==Modal.None&&!_model.Ended&&!_model.Frozen;
            if(visible&&!_tileBuildMenu)
            {
                var panel=_sceneUi?.Item("Build")?.Rect;
                if(panel!=null)
                {
                    _buildMenuPreferredSize=panel.rect.size;
                    if(_buildMenuPreferredSize.x<=0||_buildMenuPreferredSize.y<=0)_buildMenuPreferredSize=panel.sizeDelta;
                    _buildMenuGroup=panel.GetComponent<CanvasGroup>();
                    if(_buildMenuGroup==null)return;
                    _tileBuildMenu=true;
                }
            }
            _buildMenuProgress=Mathf.MoveTowards(_buildMenuProgress,visible?1:0,Time.unscaledDeltaTime/BuildPanelSeconds);
            if(_tileBuildMenu)
            {
                var panel=_sceneUi.Item("Build").Rect;
                var parent=panel.parent as RectTransform;
                if(parent!=null)
                {
                    float width=Mathf.Min(_buildMenuPreferredSize.x,Mathf.Max(1,parent.rect.width-16));
                    float height=Mathf.Min(_buildMenuPreferredSize.y,Mathf.Max(1,parent.rect.height-16));
                    float progress=Mathf.SmoothStep(0,1,_buildMenuProgress);
                    BuildRect(panel,Mathf.Lerp(-width-16,8,progress),Mathf.Clamp(158,8,Mathf.Max(8,parent.rect.height-height-8)),width,height);
                }
                _buildMenuGroup.alpha=1;
                _buildMenuGroup.interactable=visible&&_buildMenuProgress>=.99f;
                _buildMenuGroup.blocksRaycasts=_buildMenuProgress>.01f;
                if(!visible&&_buildMenuProgress<=0)_tileBuildMenu=false;
            }
            _showTools=_tileBuildMenu;
            bool markerVisible=visible&&_tileHoverValid&&_modal==Modal.None&&!_dragging;
            if(markerVisible)
            {
                int x=Mathf.RoundToInt(_pointer.X),z=Mathf.RoundToInt(_pointer.Z);
                markerVisible=_selectedBuildKind==Kind.House||_model.Territory.Contains(VillageModel.TileKey(x,z));
                if(markerVisible)_buildTile=new Point(x,z);
            }
            if(markerVisible)
            {
                EnsureBuildTileMarker();
                _buildTileMarker.transform.position=V(_buildTile,.45f);
                _buildTileMarker.transform.Rotate(Vector3.up,100*Time.unscaledDeltaTime,Space.World);
                _block.Clear();_block.SetColor("_BaseColor",_gold);_block.SetFloat("_Overlay",.6f);
                _buildTileMarkerRenderer.SetPropertyBlock(_block);
            }
            if(_buildTileMarker!=null)_buildTileMarker.SetActive(markerVisible);
        }
        private void EnsureBuildTileMarker()
        {
            if(_buildTileMarkerMesh!=null)return;
            // 면별 정점을 분리해 사각뿔 모서리를 유지한다. 월드 선택을 방해하지 않도록 콜라이더는 없다.
            var tip=Vector3.zero;
            var a=new Vector3(-.22f,.42f,-.22f);var b=new Vector3(.22f,.42f,-.22f);
            var c=new Vector3(.22f,.42f,.22f);var d=new Vector3(-.22f,.42f,.22f);
            _buildTileMarkerMesh=new Mesh{name="Downward square pyramid"};
            _buildTileMarkerMesh.vertices=new[]{tip,b,a,tip,c,b,tip,d,c,tip,a,d,a,b,c,a,c,d};
            _buildTileMarkerMesh.triangles=new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17};
            _buildTileMarkerMesh.RecalculateNormals();_buildTileMarkerMesh.RecalculateBounds();
            _buildTileMarker.GetComponent<MeshFilter>().sharedMesh=_buildTileMarkerMesh;
            _buildTileMarkerRenderer=_buildTileMarker.GetComponent<MeshRenderer>();_buildTileMarkerRenderer.sharedMaterial=_surface;
            _buildTileMarkerRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            _buildTileMarkerRenderer.receiveShadows=false;
        }
        private void DisposeBuildTileMenu()
        {
            CloseTileBuildMenu();
            if(_buildTileMarker!=null)_buildTileMarker.SetActive(false);
            if(_buildTileMarker!=null)_buildTileMarker.GetComponent<MeshFilter>().sharedMesh=null;
            if(_buildTileMarkerMesh!=null)Destroy(_buildTileMarkerMesh);
        }
        private void CloseTileBuildMenu()
        {
            _buildMenuProgress=0;
            if(_buildTileMarker!=null)_buildTileMarker.SetActive(false);
            if(_hoverRing!=null)_hoverRing.enabled=false;
            _tileBuildMenu=false;
            _showTools=false;if(_sceneUi!=null)_sceneUi.Show("Build",false);_uiRefresh=0;
        }
    }
}
