using System.Collections;
using UnityEngine;

namespace Afterlife.View
{
    public class Projectile : MonoBehaviour
    {
        public float Speed;
        public Vector2 TargetPosition;
        public Monster Owner;
        public Object Target;

        void Start()
        {
            StartCoroutine(MoveRoutine());
        }

        IEnumerator MoveRoutine()
        {
            var startPosition = transform.position;
            var endPosition = TargetPosition;
            float elapsedTime = 0f;
            float duration = 1f / Speed;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
                yield return null;
            }

            Target.TakeDamage(Owner.AttackPower, Owner);

            Destroy(gameObject);
        }
    }
}