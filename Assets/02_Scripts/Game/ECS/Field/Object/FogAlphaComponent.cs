using UnityEngine;

namespace Afterlife.Dev.Game
{
    public struct FogAlphaComponent
    {
        public SpriteRenderer[] SpriteRenderers;
        public float[] OriginalAlphas;

        public readonly void SetFogAlpha(float fogAlpha)
        {
            if (SpriteRenderers == null) return;
            for (var i = 0; i < SpriteRenderers.Length; i++)
            {
                var sr = SpriteRenderers[i];
                if (sr == null) continue;
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, (1f - fogAlpha) * OriginalAlphas[i]);
            }
        }
    }
}
