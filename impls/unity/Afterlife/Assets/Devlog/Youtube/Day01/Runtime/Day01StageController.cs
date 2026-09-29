using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // Keeps the six filmed roots and their stage-specific window mask together.
    internal sealed class Day01StageController
    {
        private readonly Transform[] _roots;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

        public int Index { get; private set; }
        public Transform Current => _roots[Index];

        public Day01StageController(Transform[] roots) { _roots = roots; }

        public void Select(int index)
        {
            Index = Mathf.Clamp(index, 0, _roots.Length - 1);
            for (int i = 0; i < _roots.Length; i++) _roots[i].gameObject.SetActive(i == Index);
            foreach (var renderer in Current.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("House_Mesh")) continue;
                _properties.Clear();
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat("_HouseWindows", Index == 5 ? 1 : 0);
                renderer.SetPropertyBlock(_properties);
            }
        }
    }
}
