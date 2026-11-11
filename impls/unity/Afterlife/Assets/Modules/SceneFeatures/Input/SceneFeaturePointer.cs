using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace Afterlife.Modules.SceneFeatures
{
    [DefaultExecutionOrder(-10)]
    public sealed class SceneFeaturePointer : MonoBehaviour
    {
        public SceneFeatureHost Host;
        public bool BlockOverUI=true;
        private void Update()
        {
            if(Host==null||Host.Session==null)return;
            var mouse=Mouse.current;
            if(mouse==null||!Application.isFocused||(BlockOverUI&&EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())) { Host.Session.Pointer(null,false,false,out _,out _);return; }
            bool valid=Host.Session.ScreenRay(mouse.position.ReadValue(),out var ray);
            Host.Session.Pointer(valid?(Ray?)ray:null,mouse.delta.ReadValue().sqrMagnitude>.01f,mouse.leftButton.wasPressedThisFrame,out _,out _);
        }
        private void OnDisable()=>Host?.Session?.Pointer(null,false,false,out _,out _);
    }
}
