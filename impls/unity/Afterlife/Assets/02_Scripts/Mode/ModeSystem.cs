using UnityEngine;

namespace Afterlife.Dev.Mode
{
    public class ModeSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private Mode[] _modes;

        private Mode _currentMode;

        public Mode CurrentMode => _currentMode;

        protected override void OnInitialize()
        {
            foreach (var mode in _modes)
            {
                mode.Initialize();
                mode.enabled = false;
            }
        }

        public void Select<TMode, TParam>(TParam exitParam, TParam enterParam) where TMode : Mode where TParam : ModeParam
        {
            foreach (var mode in _modes)
            {
                if (mode is TMode)
                {
                    if (_currentMode != null)
                        _currentMode.Exit(exitParam);
                    _currentMode = mode;
                    if (_currentMode != null)
                        _currentMode.Enter(enterParam);
                    break;
                }
            }
        }

        public void Select<TMode>() where TMode : Mode
            => Select<TMode, ModeParam>(null, null);
    }
}