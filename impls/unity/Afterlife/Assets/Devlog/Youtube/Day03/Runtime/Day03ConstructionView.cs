using System;
using System.Collections.Generic;
using Afterlife.Buildings;
using UnityEngine;
namespace Afterlife.Devlog.Day03
{
    // Temporary mode overlays only. Unbuilt actors have no site representation.
    public sealed class Day03ConstructionView : IDisposable
    {
        private readonly Transform _owner;
        private readonly Material _material;
        private readonly List<LineRenderer> _territory=new List<LineRenderer>();
        private readonly LineRenderer _preview;
        private readonly Mesh _fillMesh;
        private readonly MeshRenderer _fillRenderer;
        private readonly Material _fillMaterial;
        private readonly Func<float,float,float> _height;
        private readonly float _waterHeight;
        private readonly MaterialPropertyBlock _color=new MaterialPropertyBlock();
        public Day03ConstructionView(Transform owner,Func<float,float,float> height,float waterHeight)
        {
            _owner=owner;_height=height;_waterHeight=waterHeight;
            _material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));_material.name="Day03BuildOverlay";
            _preview=Line("PlacementPreview");
            _preview.widthMultiplier=.055f;
            _fillMaterial=new Material(_material);_fillMaterial.name="Day03PlacementFill";
            _fillMaterial.SetFloat("_Surface",1);_fillMaterial.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _fillMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _fillMaterial.SetFloat("_ZWrite",0);_fillMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");_fillMaterial.renderQueue=3000;
            var fill=new GameObject("PlacementTileFill",typeof(MeshFilter),typeof(MeshRenderer));fill.transform.SetParent(_owner,false);
            _fillMesh=new Mesh{name="Day03PlacementTile"};fill.GetComponent<MeshFilter>().sharedMesh=_fillMesh;
            _fillRenderer=fill.GetComponent<MeshRenderer>();_fillRenderer.sharedMaterial=_fillMaterial;
            _fillRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;_fillRenderer.receiveShadows=false;_fillRenderer.enabled=false;
        }
        private LineRenderer Line(string name)
        {
            var go=new GameObject(name);go.transform.SetParent(_owner,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=_material;line.useWorldSpace=true;line.loop=true;
            line.positionCount=4;line.widthMultiplier=.025f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;line.enabled=false;return line;
        }
        private void Box(LineRenderer line,BuildingFootprint area,Color color)
        {
            float x=area.X-.49f,z=area.Z-.49f,right=area.Right+.49f,top=area.Top+.49f;
            Vector3 Point(float a,float b)=>_owner.TransformPoint(new Vector3(a,Mathf.Max(_height(a,b),_waterHeight)+.045f,b));
            line.SetPositions(new[]{Point(x,z),Point(right,z),Point(right,top),Point(x,top)});
            _color.Clear();_color.SetColor("_BaseColor",color);line.SetPropertyBlock(_color);
            line.startColor=line.endColor=Color.white;line.enabled=true;
        }
        public void Present(VillageSession session,bool mode,BuildingFootprint? preview,bool valid)
        {
            int count=0;
            if(mode)foreach(var cell in session.Territory){
                if(count>=_territory.Count)_territory.Add(Line("TownTile"));
                Box(_territory[count++],new BuildingFootprint(cell.X,cell.Z,1,1),new Color(.9f,.74f,.27f,.65f));
            }
            for(int i=count;i<_territory.Count;i++)_territory[i].enabled=false;
            _preview.enabled=false;
            _fillRenderer.enabled=false;
            if(mode&&preview.HasValue){
                var tint=valid?new Color(.25f,1,.85f):new Color(1,.25f,.18f);
                Box(_preview,preview.Value,tint);
                var points=new Vector3[4];_preview.GetPositions(points);
                for(int i=0;i<points.Length;i++)points[i]=_owner.InverseTransformPoint(points[i]);
                _fillMesh.vertices=points;_fillMesh.triangles=new[]{0,2,1,0,3,2};_fillMesh.RecalculateBounds();
                tint.a=.25f;_color.SetColor("_BaseColor",tint);_fillRenderer.SetPropertyBlock(_color);_fillRenderer.enabled=true;
            }
        }
        public void Dispose()
        {
            foreach(var line in _territory)if(line!=null)UnityEngine.Object.Destroy(line.gameObject);
            if(_preview!=null)UnityEngine.Object.Destroy(_preview.gameObject);
            if(_fillRenderer!=null)UnityEngine.Object.Destroy(_fillRenderer.gameObject);
            UnityEngine.Object.Destroy(_fillMesh);UnityEngine.Object.Destroy(_fillMaterial);
            UnityEngine.Object.Destroy(_material);
        }
        public void Clear()
        {
            foreach(var line in _territory)line.enabled=false;
            _preview.enabled=false;_fillRenderer.enabled=false;
        }
    }
}
