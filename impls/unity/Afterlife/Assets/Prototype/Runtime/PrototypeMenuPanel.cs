using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed class PrototypeMenuPanel : MonoBehaviour
    {
        [HideInInspector] public int ButtonStyleVersion;
        public List<PrototypeUiBinding> Bindings=new List<PrototypeUiBinding>();
        public void Connect(PrototypeCanvas canvas)
        {
            foreach(var binding in Bindings)
            {
                canvas.Bindings.RemoveAll(b=>b!=null&&b.Id==binding.Id);
                canvas.Bindings.Add(new PrototypeUiBinding{Id=binding.Id,Rect=binding.Rect,Button=binding.Button,TmpText=binding.TmpText});
            }
        }
    }
}
