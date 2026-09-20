using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlobalLightSystem : MonoBehaviour
{
    [SerializeField] private Light2D _globalLight;
    [SerializeField] private Light2D _ambientLight;
    [SerializeField] private Vector3 _rotationAxis = Vector3.up; // 회전 축
    [SerializeField] private float _rotationSpeed = 30f; // 초당 회전 각도
    [SerializeField] private Gradient _lightColorGradient;
    [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _maxIntensity = 1.5f;
    [SerializeField] private float _minIntensity = 0f;

    void Awake()
    {
        InitializeDefaultGradient();
    }

    void Update()
    {
        // 지정된 축을 기준으로 시간에 따라 회전
        _globalLight.transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime, Space.World);

        var lightDirection = _globalLight.transform.forward;
        var lightUp = _globalLight.transform.up;
        var y_range = Mathf.Sqrt(1 - lightUp.y * lightUp.y);

        float normalizedY = -lightDirection.y;
        float timeOfDay = (normalizedY + y_range) / 2f / y_range;
        float curveValue = _intensityCurve.Evaluate(timeOfDay);

        float lightIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, curveValue);
        Color lightColor = _lightColorGradient.Evaluate(timeOfDay);

        _globalLight.intensity = lightIntensity * 0.8f;
        _globalLight.color = lightColor;

        var horizontalDirection = new Vector3(lightDirection.x, lightDirection.z, 0);
        var shadowDirection = horizontalDirection.normalized;
        var ambientLightPosition = Camera.main.transform.position + shadowDirection * -10f;
        ambientLightPosition.z = 0;
        _ambientLight.transform.position = ambientLightPosition;
        _ambientLight.intensity = lightIntensity * 2f;
        _ambientLight.color = lightColor;
    }

    private void InitializeDefaultGradient()
    {
        if (_lightColorGradient == null || _lightColorGradient.colorKeys.Length == 2)
        {
            _lightColorGradient = new Gradient();
            
            // 기본 색상 설정
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.0f);    // 한밤중 - 어두운 파랑
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.25f);     // 일출 - 주황
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);        // 정오 - 밝은 흰색
            // colorKeys[3] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.75f);     // 일몰 - 주황/빨강
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            _lightColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
}