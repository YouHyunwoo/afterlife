using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public class EventSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager cm)
        {
            var currentTime = cm.Game.Time;

            for (var i = cm.PendingEvents.Count - 1; i >= 0; i--)
            {
                if (cm.PendingEvents[i].TriggerTime <= currentTime)
                {
                    var e = cm.PendingEvents[i];
                    cm.PendingEvents.RemoveAt(i);
                    e.Start();
                    cm.ActiveEvents.Add(e);
                }
            }

            for (var i = cm.ActiveEvents.Count - 1; i >= 0; i--)
            {
                cm.ActiveEvents[i].Update(deltaTime, currentTime);
                if (cm.ActiveEvents[i].IsComplete)
                    cm.ActiveEvents.RemoveAt(i);
            }
        }
    }
}
