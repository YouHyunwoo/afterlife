using System;

namespace Afterlife.Dev.Game
{
    [Serializable]
    public struct LightComponent
    {
        public float Range;
        public float Intensity;
        public bool IsActive;
    }
}
