using UnityEngine;
namespace Afterlife.Devlog.Day02
{
    public sealed class Day02Resource : MonoBehaviour
    {
        public bool Stone;
        public Transform Art;
        public GameObject Tool;
        private float _hit;
        public void Work(){_hit=.32f;}
        public void TickTool(float delta)
        {
            _hit=Mathf.Max(0,_hit-delta);
            if(Tool!=null){Tool.SetActive(_hit>0);Tool.transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-60,55,1-_hit/.32f));}
        }
        public void ResetResource(){_hit=0;Art.localRotation=Quaternion.identity;Art.localScale=Vector3.one;Art.gameObject.SetActive(true);if(Tool!=null)Tool.SetActive(false);}
        public void Tick(float delta,bool depleted)
        {
            _hit=Mathf.Max(0,_hit-delta);
            float t=1-_hit/.32f;
            Art.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*4)*_hit*15);
            Art.localScale=Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*_hit*.15f);
            if(Tool!=null){Tool.SetActive(_hit>0);Tool.transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-60,55,t));}
            if(depleted && _hit<=0)Art.gameObject.SetActive(false);
        }
    }
}
