using UnityEngine;

namespace Afterlife.View
{
    public class RangeMonster : Monster
    {
        public float ProjectileSpeed = 5f;
        public Projectile ProjectilePrefab;

        public override void AttackStep()
        {
            var projectile = Instantiate(ProjectilePrefab, transform.position + new Vector3(0.5f, 0.5f), Quaternion.identity);
            projectile.Owner = this;
            projectile.Target = targetTransform.GetComponent<Object>();
            projectile.TargetPosition = targetTransform.position + new Vector3(0.5f, 0.5f);
            projectile.Speed = ProjectileSpeed;
        }
    }
}