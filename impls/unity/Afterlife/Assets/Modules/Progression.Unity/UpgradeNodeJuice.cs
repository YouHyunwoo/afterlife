using UnityEngine;

namespace Afterlife.Modules.Progression
{
    public sealed class UpgradeNodeJuice : MonoBehaviour
    {
        private float _elapsed = 1;
        private Vector3 _start = Vector3.one;
        public void Play() { _start = transform.localScale; _elapsed = 0; enabled = true; }
        private void Awake() { enabled = false; }
        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / .30f);
            float envelope = Mathf.Pow(1 - t, 2);
            float expand = .24f * Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-2 * t);
            float jelly = .25f * Mathf.Sin(t * Mathf.PI * 5) * envelope;
            Vector3 target = new Vector3(1 + expand + jelly, 1 + expand - jelly * .8f, 1);
            transform.localScale = target + (_start - Vector3.one) * envelope;
            if (t >= 1) { transform.localScale = Vector3.one; enabled = false; }
        }
        private void OnDisable() { transform.localScale = Vector3.one; }
    }
}
