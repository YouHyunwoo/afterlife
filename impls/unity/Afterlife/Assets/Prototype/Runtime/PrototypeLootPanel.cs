using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    // 프리팹 내부 참조를 보관하므로 인스턴스 생성 후에도 이름 검색 없이 연결한다.
    public sealed class PrototypeLootPanel : MonoBehaviour
    {
        public List<PrototypeUiBinding> Bindings=new List<PrototypeUiBinding>();
        public void Connect(PrototypeCanvas canvas)
        {
            canvas.Bindings.RemoveAll(b=>b!=null&&b.Id!=null&&(b.Id=="Modal.Loot"||b.Id.StartsWith("Loot.")));
            foreach(var b in Bindings)canvas.Bindings.Add(new PrototypeUiBinding{Id=b.Id,Rect=b.Rect,Text=b.Text,TmpText=b.TmpText,Button=b.Button,Slider=b.Slider,Image=b.Image});
        }
    }
}
