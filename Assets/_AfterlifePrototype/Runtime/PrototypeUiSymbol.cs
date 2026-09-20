using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed class PrototypeUiSymbol : MaskableGraphic
    {
        public enum Shape { Triangle,Sun,Moon }
        public Shape Symbol;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;Vector2 c=r.center;float radius=Mathf.Min(r.width,r.height)*.45f;
            if(Symbol==Shape.Triangle){Add(vh,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(c.x,r.yMin));return;}
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                if(Symbol==Shape.Sun)Add(vh,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.65f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.65f);
                else
                {
                    float y0=Mathf.Lerp(-radius,radius,i/32f),y1=Mathf.Lerp(-radius,radius,(i+1)/32f);
                    float x0=Mathf.Sqrt(Mathf.Max(0,radius*radius-y0*y0)),x1=Mathf.Sqrt(Mathf.Max(0,radius*radius-y1*y1));
                    Quad(vh,c+new Vector2(-x0,y0),c+new Vector2(-x1,y1),c+new Vector2(-x1*.25f,y1),c+new Vector2(-x0*.25f,y0));
                }
            }
            if(Symbol==Shape.Sun)for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),n=new Vector2(-d.y,d.x)*radius*.10f;Quad(vh,c+d*radius*.8f-n,c+d*radius-n,c+d*radius+n,c+d*radius*.8f+n);}
        }
        private void Add(VertexHelper v,Vector2 a,Vector2 b,Vector2 c){int i=v.currentVertCount;v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);}
        private void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d){Add(v,a,b,c);Add(v,a,c,d);}
    }
}
