using ECS.Dev;

namespace Afterlife.Dev.Game
{
    // * SelectCommand를 검사해서 선택/해제 처리
    public class SelectSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasSelectCommand[i] &&
                    cm.HasEntity[i])
                    UpdateSelect(ref cm.SelectCommands[i], i, cm);
            }
        }

        private void UpdateSelect(ref SelectCommand selectCommand, int entityId, ComponentManager cm)
        {
            if (selectCommand.Type == SelectType.Select)
            {
                HandleSelection(entityId, cm);
                selectCommand.Type = SelectType.None;
            }
            else if (selectCommand.Type == SelectType.Deselect)
            {
                HandleDeselection(entityId, cm);
                selectCommand.Type = SelectType.None;
            }
        }

        private void HandleSelection(int entityId, ComponentManager cm)
        {
            cm.HasSelectedTag[entityId] = true;
            var entity = cm.Entities[entityId];
            if (entity.TryGetComponent(out SelectionIndicator selectionIndicator))
                selectionIndicator.ShowIndicator();
        }

        private void HandleDeselection(int entityId, ComponentManager cm)
        {
            cm.HasSelectedTag[entityId] = false;
            var entity = cm.Entities[entityId];
            if (entity.TryGetComponent(out SelectionIndicator selectionIndicator))
                selectionIndicator.HideIndicator();
        }
    }
}