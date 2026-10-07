using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Shares the puddle coordinate frame with its water and reflected window.</summary>
    public sealed class ThresholdPuddleSurface : MonoBehaviour
    {
        public Renderer[] Surfaces;
        private MaterialPropertyBlock[] _original;
        private MaterialPropertyBlock _block;
        private void OnEnable()
        {
            if (Surfaces == null) return;
            _block = new MaterialPropertyBlock();
            _original = new MaterialPropertyBlock[Surfaces.Length];
            for (int i = 0; i < Surfaces.Length; i++)
            {
                _original[i] = new MaterialPropertyBlock();
                if (Surfaces[i] != null) Surfaces[i].GetPropertyBlock(_original[i]);
            }
            LateUpdate();
        }
        private void LateUpdate()
        {
            if (_block == null || Surfaces == null) return;
            foreach (var surface in Surfaces)
            {
                if (surface == null) continue;
                surface.GetPropertyBlock(_block);
                _block.SetMatrix("_PuddleWorldToLocal", transform.worldToLocalMatrix);
                _block.SetFloat("_UsePuddleTransform", 1);
                surface.SetPropertyBlock(_block);
            }
        }
        private void OnDisable()
        {
            if (_original == null || Surfaces == null) return;
            for (int i = 0; i < Mathf.Min(Surfaces.Length, _original.Length); i++)
                if (Surfaces[i] != null) Surfaces[i].SetPropertyBlock(_original[i]);
            _original = null; _block = null;
        }
    }
}
