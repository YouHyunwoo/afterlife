using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class Soul : View
    {
        [SerializeField] Image image;

        public RectTransform center;
        public float radius = 100f;
        public float angularSpeed = 90f; // 평균 회전 속도
        public float noiseLerpSpeed = 0.1f; // 변화 속도
        float currentAngularSpeed;
        float orbitRotation = 0f; // 궤도 회전 각도(도)

        RectTransform rectTransform;
        float angle;
        Material material;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            material = image.material;
        }

        void Start()
        {
            angle = 0f;
            orbitRotation = Random.Range(-30f, 30f);
            currentAngularSpeed = angularSpeed;
            StartCoroutine(MoveRoutine());
        }

        IEnumerator MoveRoutine()
        {
            angle = Random.value * 360f; // 초기 각도 랜덤 설정
            rectTransform.anchoredPosition = GetAnchoredPositionByAngle(angle);

            var i = 30;
            var targetPosition = rectTransform.anchoredPosition;
            float adaptiveTargetSpeed = angularSpeed;

            while (true)
            {
                adaptiveTargetSpeed += Random.Range(-5f, 5f);

                currentAngularSpeed = Mathf.Lerp(currentAngularSpeed, adaptiveTargetSpeed, noiseLerpSpeed);
                adaptiveTargetSpeed = Mathf.Lerp(adaptiveTargetSpeed, angularSpeed, noiseLerpSpeed);

                angle += currentAngularSpeed * Time.deltaTime;
                if (angle > 360f) { angle -= 360f; }

                image.material = 0 < angle && angle < 180f ? material : null;

                if (i > 10)
                {
                    targetPosition = GetAnchoredPositionByAngle(angle) + GenerateRandomNoiseOffset();
                    i = 0;
                }

                rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPosition, 0.05f);
                image.rectTransform.localScale = GetScaleByAngle(angle);

                i++;

                yield return null;
            }
        }

        Vector3 GetAnchoredPositionByAngle(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            var radiusX = radius;
            var radiusY = radius * Mathf.Sin(30f * Mathf.Deg2Rad);
            float x = Mathf.Cos(rad) * radiusX;
            float y = Mathf.Sin(rad) * radiusY;

            // 궤도 자체의 회전 적용
            float orbitRad = orbitRotation * Mathf.Deg2Rad;
            float rotatedX = x * Mathf.Cos(orbitRad) - y * Mathf.Sin(orbitRad);
            float rotatedY = x * Mathf.Sin(orbitRad) + y * Mathf.Cos(orbitRad);
            Vector2 offset = new(rotatedX, rotatedY);

            // 중심점의 anchoredPosition 기준으로 offset 적용
            Vector2 centerAnchored = center.anchoredPosition;
            return centerAnchored + offset;
        }

        Vector3 GenerateRandomNoiseOffset()
        {
            float noiseX = Random.Range(-0.1f, 0.1f);
            float noiseY = Random.Range(-0.1f, 0.1f);
            return new Vector3(noiseX, noiseY, 0);
        }

        Vector3 GetScaleByAngle(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            // scaleFactor는 -1에서 1 사이의 값을 가지며, 0에 가까울수록 작아짐
            var scaleFactor = Mathf.Pow(1.2f, -1f * Mathf.Sin(rad));
            return new Vector3(scaleFactor, scaleFactor, 1f);
        }
    }
}