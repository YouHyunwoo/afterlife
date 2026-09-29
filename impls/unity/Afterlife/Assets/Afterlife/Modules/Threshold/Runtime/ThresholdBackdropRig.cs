using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Renders the sample world on its own layer and never takes over a game's camera target.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdBackdropRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _environment;
        [SerializeField] private ThresholdCameraPose[] _poses;
        [SerializeField] private Material _veilTemplate;
        [SerializeField] private ThresholdProfile _profile;
        [SerializeField] private ThresholdModalEffect _modalEffect;
        private UnityEngine.UI.RawImage _display;
        private RenderTexture _texture;
        private Material _material;
        private Texture _previousTexture;
        private Material _previousMaterial;
        private int _width;
        private int _height;
        private ThresholdScreen _screen;
        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private float _fromFieldOfView;
        private float _travel;
        private bool _modal;
        private bool _enteringPortal;
        private float _portalProgress;
        private Vector3 _portalStartPosition;
        private Quaternion _portalStartRotation;
        private float _portalStartFieldOfView;
        private Transform _portalEntry;
        private ThresholdPortal _portal;
        private float _portalVisibility;
        private float _fromPortalVisibility;

        public bool PortalEntryComplete => _enteringPortal && _portalProgress >= ThresholdPresentationSystem.PortalCutover;

        public void SetModal(bool visible)
        {
            _modal = visible;
            if (_modalEffect == null) _modalEffect = GetComponent<ThresholdModalEffect>();
            if (_modalEffect != null) _modalEffect.SetOpen(visible);
        }

        public void Configure(Camera viewCamera, GameObject environment, ThresholdCameraPose[] poses, Material veilTemplate, ThresholdProfile profile)
        {
            _camera = viewCamera;
            _environment = environment;
            _poses = poses;
            _veilTemplate = veilTemplate;
            _profile = profile;
            _modalEffect = GetComponent<ThresholdModalEffect>();
        }

        public void Bind(UnityEngine.UI.RawImage display)
        {
            if (_display == display && _material != null) return;
            RestoreDisplay();
            _display = display;
            if (_display == null) return;
            _previousTexture = _display.texture;
            _previousMaterial = _display.material;
            if (_veilTemplate != null) _display.material = _material = new Material(_veilTemplate);
            if (_texture != null) _display.texture = _texture;
        }

        public void Show(ThresholdScreen screen)
        {
            _enteringPortal = false;
            _portalProgress = 0;
            var previous = _screen;
            _screen = screen;
            if (_portal == null && _environment != null)
                _portal = _environment.GetComponentInChildren<ThresholdPortal>(true);
            if (screen == ThresholdScreen.Title || previous == ThresholdScreen.Playing)
                _portalVisibility = screen == ThresholdScreen.Title ? 0 : 1;
            _fromPortalVisibility = _portalVisibility;
            if (_portal != null) _portal.SetVisibility(_portalVisibility);
            bool visible = screen != ThresholdScreen.Playing;
            if (_environment != null) _environment.SetActive(visible);
            if (_camera != null) _camera.enabled = visible;
            if (_display != null) _display.enabled = visible;
            if (!visible) { ReleaseTexture(); return; }
            if (_camera == null) return;
            _fromPosition = _camera.transform.position;
            _fromRotation = _camera.transform.rotation;
            _fromFieldOfView = _camera.fieldOfView;
            _travel = 0;
            if (previous == ThresholdScreen.Playing)
            {
                var target = FindPose(screen);
                if (target != null)
                {
                    _camera.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
                    _camera.fieldOfView = target.FieldOfView;
                    _travel = 1;
                }
            }
            Resize();
        }

        public bool BeginPortalEntry()
        {
            if (_camera == null || !_camera.enabled || _screen != ThresholdScreen.Lobby) return false;
            var portal = _environment != null ? _environment.GetComponentInChildren<ThresholdPortal>(true) : null;
            if (portal == null || portal.Surface == null) return false;
            _portalEntry = portal.Surface.transform;
            var lobby = FindPose(ThresholdScreen.Lobby);
            _portalStartPosition = lobby != null ? lobby.transform.position : _camera.transform.position;
            _portalStartRotation = lobby != null ? lobby.transform.rotation : _camera.transform.rotation;
            _portalStartFieldOfView = lobby != null ? lobby.FieldOfView : _camera.fieldOfView;
            _portalProgress = 0;
            _enteringPortal = true;
            return true;
        }

        private void LateUpdate()
        {
            if (_camera == null || !_camera.enabled) return;
            Resize();
            ThresholdCameraPose target = FindPose(_screen);
            if (_enteringPortal)
            {
                float duration = _profile != null ? Mathf.Max(.5f, _profile.WorldTransitionSeconds) : 1.8f;
                _portalProgress = Mathf.MoveTowards(_portalProgress, ThresholdPresentationSystem.PortalCutover, Time.unscaledDeltaTime / duration);
                ThresholdPresentationSystem.PortalApproach(_camera, _portalStartPosition, _portalStartRotation,
                    _portalStartFieldOfView, _portalEntry, _portalProgress);
            }
            else if (target != null)
            {
                _travel = Mathf.MoveTowards(_travel, 1, Time.unscaledDeltaTime / .95f);
                float t = _travel * _travel * (3 - 2 * _travel);
                _camera.transform.SetPositionAndRotation(Vector3.Lerp(_fromPosition, target.transform.position, t), Quaternion.Slerp(_fromRotation, target.transform.rotation, t));
                _camera.fieldOfView = Mathf.Lerp(_fromFieldOfView, target.FieldOfView, t);
            }
            if (!_enteringPortal)
            {
                float reveal = Mathf.SmoothStep(0, 1, _travel);
                _portalVisibility = _screen == ThresholdScreen.Title ? 0 : Mathf.Lerp(_fromPortalVisibility, 1, reveal);
                if (_portal != null) _portal.SetVisibility(_portalVisibility);
            }
            if (_material == null) return;
            bool result = _screen == ThresholdScreen.GameOver || _screen == ThresholdScreen.Clear;
            _material.SetFloat("_Cover", _enteringPortal && _portalProgress >= ThresholdPresentationSystem.PortalArrival ? 1 : 0);
            float modalAmount = _modalEffect != null ? _modalEffect.Amount : (_modal ? 1 : 0);
            _material.SetFloat("_Modal", modalAmount);
            _material.SetFloat("_Result", result ? 1 : 0);
            _material.SetFloat("_Blur", modalAmount * .65f);
            _material.SetFloat("_Motion", 1);
            _material.SetFloat("_Clock", Time.unscaledTime);
            _material.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1, Screen.height));
            _material.SetFloat("_Darkness", 0);
        }

        private ThresholdCameraPose FindPose(ThresholdScreen screen)
        {
            if (_poses == null || _poses.Length == 0) _poses = GetComponentsInChildren<ThresholdCameraPose>(true);
            foreach (var pose in _poses)
                if (pose != null && pose.Screen == screen) return pose;
            return null;
        }

        private void Resize()
        {
            int height = 320;
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)Screen.width / Mathf.Max(1, Screen.height)));
            if (_texture != null && width == _width && height == _height) return;
            ReleaseTexture();
            _width = width;
            _height = height;
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Threshold Backdrop",
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.Create();
            _camera.aspect = (float)width / height;
            _camera.targetTexture = _texture;
            if (_display != null) _display.texture = _texture;
        }

        private void ReleaseTexture()
        {
            if (_texture == null) return;
            if (_camera != null && _camera.targetTexture == _texture) _camera.targetTexture = null;
            if (_display != null && _display.texture == _texture) _display.texture = null;
            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }

        private void RestoreDisplay()
        {
            if (_display != null) { _display.texture = _previousTexture; _display.material = _previousMaterial; }
            if (_material != null) Destroy(_material);
            _material = null;
        }

        private void OnDisable()
        {
            ReleaseTexture();
            RestoreDisplay();
        }
    }
}
