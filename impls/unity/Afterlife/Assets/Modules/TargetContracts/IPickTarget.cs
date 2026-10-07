using UnityEngine;

namespace Afterlife.Modules.TargetContracts
{
    public interface IPickTarget
    {
        bool IsBroken { get; }
        bool SnowVisible(Vector3 world);
    }

    public interface ISnowTarget : IPickTarget
    {
        Transform Visual { get; }
        bool HasSnow { get; }
        float SnowRemaining(Vector3 world);
    }
}
