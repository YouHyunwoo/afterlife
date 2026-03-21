using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class MonsterSpawnSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private GameObject _monsterPrefab;

        public bool SpawnMonster(Vector3 position)
        {
            if (_townAreaSystem.IsPositionInAnyInfluence(position))
            {
                Debug.Log("몬스터 스폰 실패: 타운 영향력 내");
                return false;
            }

            Instantiate(_monsterPrefab, position, Quaternion.identity);
            Debug.Log("몬스터 스폰 성공!");
            return true;
        }
    }
}