using System.Collections.Generic;

namespace Afterlife.Dev.Field
{
    public class EventSystem : Moonstone.Ore.Local.System
    {
        private TimeSystem _timeSystem;

        private readonly List<Event> _pendingEvents = new();
        private readonly List<Event> _activeEvents = new();

        public void Register(Event gameEvent) => _pendingEvents.Add(gameEvent);

        private void Update()
        {
            var currentTime = _timeSystem.ElapsedTime;

            for (var i = _pendingEvents.Count - 1; i >= 0; i--)
            {
                if (_pendingEvents[i].TriggerTime <= currentTime)
                {
                    var e = _pendingEvents[i];
                    _pendingEvents.RemoveAt(i);
                    e.Start();
                    _activeEvents.Add(e);
                }
            }

            for (var i = _activeEvents.Count - 1; i >= 0; i--)
            {
                _activeEvents[i].Update();
                if (_activeEvents[i].IsComplete)
                    _activeEvents.RemoveAt(i);
            }
        }
    }
}
