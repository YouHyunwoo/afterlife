// * ComponentManager.Object

using System.Linq;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public Entity[] Entities = new Entity[MAX_ENTITIES];
        public HealthComponent[] Healths = new HealthComponent[MAX_ENTITIES];
        public DamageCommand[] DamageCommands = new DamageCommand[MAX_ENTITIES];
        public SelectCommand[] SelectCommands = new SelectCommand[MAX_ENTITIES];
        public LightComponent[] Lights = new LightComponent[MAX_ENTITIES];
        public FogAlphaComponent[] FogAlphas = new FogAlphaComponent[MAX_ENTITIES];

        public bool[] HasEntity = new bool[MAX_ENTITIES];
        public bool[] HasHealth = new bool[MAX_ENTITIES];
        public bool[] HasDamageCommand = new bool[MAX_ENTITIES];
        public bool[] HasDiedTag = new bool[MAX_ENTITIES];
        public bool[] HasSelectCommand = new bool[MAX_ENTITIES];
        public bool[] HasSelectedTag = new bool[MAX_ENTITIES];
        public bool[] HasLight = new bool[MAX_ENTITIES];
        public bool[] HasFogAlpha = new bool[MAX_ENTITIES];

        public void AddEntity(int entityId, Entity entity)
        {
            Entities[entityId] = entity;
            HasEntity[entityId] = true;
        }

        private void RemoveEntity(int entityId)
        {
            HasEntity[entityId] = false;
            Entities[entityId] = null;
        }

        public void AddHealth(int entityId, float health)
        {
            Healths[entityId].Current = Healths[entityId].Max = health;
            HasHealth[entityId] = true;
        }

        private void RemoveHealth(int entityId)
        {
            HasHealth[entityId] = false;
        }

        public void AddDamageCommand(int entityId)
        {
            DamageCommands[entityId].Amount = 0f;
            DamageCommands[entityId].Attacker = null;
            DamageCommands[entityId].IsPending = false;
            HasDamageCommand[entityId] = true;
        }

        private void RemoveDamageCommand(int entityId)
        {
            HasDamageCommand[entityId] = false;
        }

        public void AddSelectCommand(int entityId)
        {
            SelectCommands[entityId].Type = SelectType.None;
            HasSelectCommand[entityId] = true;
        }

        private void RemoveSelectCommand(int entityId)
        {
            HasSelectCommand[entityId] = false;
        }

        public void AddLight(int entityId, LightComponent light)
        {
            Lights[entityId].Range = light.Range;
            Lights[entityId].Intensity = light.Intensity;
            Lights[entityId].IsActive = light.IsActive;
            HasLight[entityId] = true;
        }

        internal void RemoveLight(int entityId)
        {
            HasLight[entityId] = false;
        }

        public void AddFogAlpha(int entityId, SpriteRenderer[] renderers)
        {
            FogAlphas[entityId].SpriteRenderers = renderers;
            FogAlphas[entityId].OriginalAlphas = renderers.Select(r => r.color.a).ToArray();
            HasFogAlpha[entityId] = true;
        }

        internal void RemoveFogAlpha(int entityId)
        {
            HasFogAlpha[entityId] = false;
            FogAlphas[entityId].SpriteRenderers = null;
            FogAlphas[entityId].OriginalAlphas = null;
        }
    }
}