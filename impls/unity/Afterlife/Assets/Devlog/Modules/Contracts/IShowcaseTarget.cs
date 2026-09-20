using UnityEngine;

namespace Afterlife.Devlog.Contracts
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
