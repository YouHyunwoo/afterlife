using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private sealed class BurnView {public GameObject Root;public Transform[] Flames;public int Body;public bool Used;}
        private readonly List<BurnView> _burnViews=new List<BurnView>();
        private void UpdateGrowthEffects()
        {
            foreach(var effect in _burnViews)effect.Used=false;
            foreach(var body in _model.Bodies)
            {
                if(!body.Alive||body.BurnRemaining<=0)continue;
                BurnView view=null;foreach(var existing in _burnViews)if(existing.Body==body.Id){view=existing;break;}
                if(view==null)
                {
                    foreach(var existing in _burnViews)if(existing.Body==0){view=existing;break;}
                    if(view==null)
                    {
                        var root=new GameObject("Burning enemy");root.transform.SetParent(transform,false);view=new BurnView{Root=root,Flames=new Transform[3]};
                        for(int f=0;f<3;f++)view.Flames[f]=Cone(root.transform,"Flame",new Vector3((f-1)*.17f,.18f,0),new Vector3(.23f,.6f,.23f),f==1?new Color(1,.83f,.18f):new Color(1,.3f,.07f)).transform;
                        _burnViews.Add(view);
                    }
                    view.Body=body.Id;
                }
                view.Used=true;view.Root.SetActive(true);view.Root.transform.position=V(body.Position);
                for(int f=0;f<view.Flames.Length;f++)view.Flames[f].localScale=new Vector3(.20f,.45f+Mathf.Sin(_effectTime*14+f*2+body.Id)*.16f,.20f);
            }
            foreach(var effect in _burnViews)if(!effect.Used){effect.Body=0;effect.Root.SetActive(false);}
        }
        private void ResetGrowthEffects()
        {
            foreach(var effect in _burnViews){effect.Body=0;effect.Root.SetActive(false);}
        }
    }
}
