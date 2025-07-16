using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Title
{
    public class Fog : View
    {
        public int state;
        public float alpha;
        Image image;
        RectTransform rectTransform;

        void Awake()
        {
            image = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
        }

        void Start()
        {
            state = 0;
            StartCoroutine(FogRoutine());
        }

        IEnumerator FogRoutine()
        {
            while (true)
            {
                yield return MoveRoutine();
            }
        }

        IEnumerator FadeIn()
        {
            var elapsedTime = 0f;
            var duration = 1f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                image.color = new Color(1, 1, 1, Mathf.Lerp(0, alpha, elapsedTime / duration));
                yield return null;
            }
        }

        IEnumerator FadeOut()
        {
            var elapsedTime = 0f;
            var duration = 1f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                image.color = new Color(1, 1, 1, Mathf.Lerp(alpha, 0, elapsedTime / duration));
                yield return null;
            }
        }

        IEnumerator MoveRoutine()
        {
            var elapsedTime = 0f;
            var duration = Random.Range(20f, 30f);
            rectTransform.anchoredPosition = new Vector3(Random.Range(-500, 500), rectTransform.anchoredPosition.y, 0);
            var originalPosition = rectTransform.anchoredPosition.x;
            var targetPosition = rectTransform.anchoredPosition.x < 0 ? Random.Range(300, 500) : Random.Range(-500, -300);
            var isFadeOut = false;
            StartCoroutine(FadeIn());
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                if (!isFadeOut && elapsedTime > duration - 2f)
                {
                    StartCoroutine(FadeOut());
                    isFadeOut = true;
                }
                rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(originalPosition, targetPosition, elapsedTime / duration), rectTransform.anchoredPosition.y);
                yield return null;
            }
        }
    }
}