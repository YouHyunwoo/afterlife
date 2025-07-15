using UnityEngine;

namespace Afterlife.Core
{
    public class EffectManager : ManagerBase
    {
        [SerializeField] GameObject[] effectPrefabs;

        public void PlayGFX(string name, Vector3 position)
        {
            var prefab = System.Array.Find(effectPrefabs, e => e.name == name);
            if (prefab == null) { return; }

            var instance = Instantiate(prefab, position, Quaternion.identity);
            Destroy(instance, 2f);
        }
    }
}