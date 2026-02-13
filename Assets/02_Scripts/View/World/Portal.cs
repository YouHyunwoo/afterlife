using System.Collections;
using Afterlife.Core;
using Afterlife.Data;
using UnityEngine;

namespace Afterlife.View
{
    public class Portal : Object
    {
        public ObjectSpawn ObjectSpawn;

        protected override void Start()
        {
            base.Start();
            StartCoroutine(MonsterGenerationRoutine());
        }

        IEnumerator MonsterGenerationRoutine()
        {
            var stage = ServiceLocator.Get<StageManager>().Stage;
            var fieldObjectSystem = ServiceLocator.Get<StageManager>().fieldObjectSystem;
            var offsets = new Vector2Int[]
            {
                new(-1, -1), new(-1, 0), new(-1, 1),
                new( 0, -1),             new( 0, 1),
                new(+1, -1), new(+1, 0), new(+1, 1),
            };

            while (true)
            {
                yield return new WaitForSeconds(ObjectSpawn.SpawnInterval);

                var currentLocation = new Vector2Int(
                    Mathf.RoundToInt(transform.position.x),
                    Mathf.RoundToInt(transform.position.y)
                );
                var offset = offsets[UnityEngine.Random.Range(0, offsets.Length)];
                var location = currentLocation + offset;
                if (!stage.Map.IsAvailable(location)) { continue; }

                var sampledPrefab = ObjectSpawn.Sample();
                if (sampledPrefab == null) { continue; }

                var spawnedGameObject = fieldObjectSystem.Spawn(sampledPrefab, location);
                spawnedGameObject.GetComponent<Object>();
            }
        }

        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Cut", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("sword");
            var isCriticalHit = UnityEngine.Random.value < player.CriticalRate;
            if (isCriticalHit) { ServiceLocator.Get<AudioManager>().PlaySFX("critical"); }
            var damage = player.AttackPower * (isCriticalHit ? player.CriticalDamageMultiplier : 1f);
            player.TakeExperience(damage / 10f);
            TakeDamage(damage, null);
            base.Interact(player);
        }

        public override void Die(Object attacker)
        {
            StopAllCoroutines();
            base.Die(attacker);
        }
    }
}