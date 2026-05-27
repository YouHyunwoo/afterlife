using System;
using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public class GameResultSystem : ISystem<ComponentManager>
    {
        public Action OnGameClear;
        public Action OnGameOver;

        private bool _isResolved;

        public void Reset() => _isResolved = false;

        public void Update(float deltaTime, ComponentManager cm)
        {
            if (_isResolved) return;

            // 보스 사망 → 게임 클리어
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (cm.HasBossTag[i] && cm.HasDiedTag[i])
                {
                    _isResolved = true;
                    OnGameClear?.Invoke();
                    return;
                }
            }

            // 집이 모두 파괴되면 게임 오버
            var houseCount = 0;
            var deadHouseCount = 0;
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasHouseTag[i]) continue;
                houseCount++;
                if (cm.HasDiedTag[i]) deadHouseCount++;
            }

            if (houseCount > 0 && houseCount == deadHouseCount)
            {
                _isResolved = true;
                OnGameOver?.Invoke();
            }
        }
    }
}
