using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    public sealed class ProgressionButtonGate : MonoBehaviour
    {
        public Func<bool> Available;
        private Button _button;
        private void Awake() => _button = GetComponent<Button>();
        private void Update() { if (_button != null) _button.interactable = Available == null || Available(); }
    }
}
