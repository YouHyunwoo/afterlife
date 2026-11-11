using UnityEngine;
namespace Afterlife.Devlog.Day03
{
    public sealed class Day03Site : MonoBehaviour
    {
        public VillageTarget Target;
        public Transform Model, Crop;
        public GameObject Foundation;
        private Vector3 _modelScale, _cropScale;
        private void Awake() { if(Model!=null)_modelScale=Model.localScale;if(Crop!=null)_cropScale=Crop.localScale; }
        public void Present(bool purchased, float progress, float production)
        {
            if(Model!=null) {
                Model.gameObject.SetActive(purchased);
                Model.localScale=new Vector3(_modelScale.x,_modelScale.y*Mathf.Lerp(.12f,1,progress),_modelScale.z);
            }
            if(Foundation!=null)Foundation.SetActive(purchased&&progress<1);
            var collider=GetComponent<Collider>();if(collider!=null)collider.enabled=purchased;
            if(Crop!=null) {
                Crop.gameObject.SetActive(purchased&&progress>=1);
                Crop.localScale=new Vector3(_cropScale.x,_cropScale.y*Mathf.Lerp(.35f,1,production),_cropScale.z);
            }
        }
    }
}
