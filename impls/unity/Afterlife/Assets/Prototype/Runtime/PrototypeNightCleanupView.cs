using System.Collections.Generic;
using Afterlife.Modules.NightCleanup.Unity;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private RemainingEnemyMarkers _nightCleanupMarkers;
        private readonly List<EnemyMarkerTarget> _nightCleanupTargets = new List<EnemyMarkerTarget>();
        private void UpdateNightCleanupMarkers()
        {
            if (_nightCleanupMarkers == null)
                _nightCleanupMarkers = GetComponent<RemainingEnemyMarkers>() ?? gameObject.AddComponent<RemainingEnemyMarkers>();
            _nightCleanupTargets.Clear();
            if (_model.NightOvertime && _model.NightCleanup.ShowDirection)
                foreach (var enemy in _model.Bodies)
                    if (enemy.Alive && enemy.Raider)
                        _nightCleanupTargets.Add(new EnemyMarkerTarget(enemy.Id, GroundPosition(enemy.Position)));
            _nightCleanupMarkers.Present(_camera, _nightCleanupTargets,
                _model.NightOvertime && _model.NightCleanup.ShowDirection,
                _model.NightCleanup.ShowBeacon);
        }
    }
}
