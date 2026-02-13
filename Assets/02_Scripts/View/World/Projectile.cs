using System.Collections;
using UnityEngine;

namespace Afterlife.View
{
    public class Projectile : MonoBehaviour
    {
        public float Speed;
        public Vector2 TargetPosition;
        public float Damage;
        public Object Owner;
        public Object Target;

        void Start()
        {
            StartCoroutine(MoveRoutine());
        }

        IEnumerator MoveRoutine()
        {
            var startPosition = (Vector2)transform.position;
            var endPosition = TargetPosition;
            float duration = (endPosition - startPosition).magnitude / Speed;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
                yield return null;
            }

            Target.TakeDamage(Damage, Owner);

            Destroy(gameObject);
        }
    }
}