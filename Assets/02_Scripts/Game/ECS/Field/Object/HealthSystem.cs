using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class HealthSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasDiedTag[i]) continue;
                if (!cm.HasHealth[i]) continue;
                if (!cm.HasDamageCommand[i]) continue;

                if (cm.DamageCommands[i].IsPending)
                {
                    var cmd = cm.DamageCommands[i];
                    var health = cm.Healths[i];

                    var actualDamage = Mathf.Clamp(cmd.Amount, 0f, health.Current);
                    health.Current -= actualDamage;

                    cm.Healths[i] = health;
                    cm.DamageCommands[i].IsPending = false;

                    if (health.Current <= 0f)
                    {
                        health.Current = 0f;
                        HandleDeath(i, cmd.Attacker, cm);
                    }
                    else
                    {
                        // TriggerDamagedEffect(i, actualDamage);
                        // cm.HasHurtVisualTag[i] = true;
                        // -> VFXSystem에서 체크해서 재생
                    }
                }
            }
        }

        private void HandleDeath(int entityId, object attacker, ComponentManager componentManager)
        {
            componentManager.HasDiedTag[entityId] = true;
            componentManager.HasDespawnTag[entityId] = true;
        }
    }
}