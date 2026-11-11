using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Prototype
{
    // Variant 내부 참조를 보관해 이름 변경 후에도 UI 연결을 유지한다.
    public sealed class PrototypeModalPanel : MonoBehaviour
    {
        public List<PrototypeUiBinding> Bindings=new List<PrototypeUiBinding>();
        [HideInInspector] public List<string> RetiredBindingIds=new List<string>();

        public void Connect(PrototypeCanvas canvas)
        {
            canvas.Bindings.RemoveAll(b=>b!=null&&RetiredBindingIds.Contains(b.Id));
            foreach(var binding in Bindings)
            {
                canvas.Bindings.RemoveAll(b=>b!=null&&b.Id==binding.Id);
                canvas.Bindings.Add(new PrototypeUiBinding
                {
                    Id=binding.Id,Rect=binding.Rect,Text=binding.Text,TmpText=binding.TmpText,
                    Button=binding.Button,Slider=binding.Slider,Image=binding.Image
                });
            }
        }
    }
}
