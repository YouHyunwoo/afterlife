using UnityEngine;

public class Shadow : MonoBehaviour
{
    [SerializeField] private GlobalLightSystem _globalLightSystem;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _objectHeight = 1f;

    private Material _material;

    void Awake()
    {
        _spriteRenderer.material = _material = Instantiate(_spriteRenderer.material);
    }
    
    void Update()
    {
        var lightDirection = _globalLightSystem.transform.forward;
        var lightUp = _globalLightSystem.transform.up;
        if (lightDirection.y >= 0)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        var y_range = Mathf.Sqrt(1 - lightUp.y * lightUp.y);
        float normalizedY = lightDirection.y;
        float timeOfDay = normalizedY / y_range;

        var horizontalDirection = new Vector3(lightDirection.x, 0, lightDirection.z);
        var shadowLength = _objectHeight * horizontalDirection.magnitude / Mathf.Abs(lightDirection.y);
        transform.localScale = new Vector3(1, shadowLength, 1);

        _material.SetFloat("_Blurring_Ratio", Mathf.Clamp01(timeOfDay + 1f));

        var shadowDirection = horizontalDirection.normalized;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(shadowDirection.x, shadowDirection.z, 0));
        var color = new Color(0, 0, 0, Mathf.Lerp(0, 0.5f, 1 - Mathf.Clamp01(timeOfDay + 1f)));
        _spriteRenderer.color = color;
    }
}