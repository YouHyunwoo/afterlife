// * ComponentManager.Event

using System.Collections.Generic;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager
    {
        public readonly List<Event> PendingEvents = new();
        public readonly List<Event> ActiveEvents = new();

        public void RegisterEvent(Event fieldEvent) => PendingEvents.Add(fieldEvent);
    }
}
