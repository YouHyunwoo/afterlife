using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Modules.SceneFeatures
{
    public interface IImpactReceiver
    {
        bool CanHit { get; } bool IsBroken { get; } bool Active { get; }
        bool IsWood { get; } bool UseJuice { get; } Transform Root { get; }
        List<Vector3> ShedSnow(Vector3 point);
        void RecordDamageMark(Vector3 point,Vector3 normal,Color color);
        void Hit(Vector3 point,Vector3 incoming);
    }
}
