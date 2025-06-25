using UnityEngine;
using System.Collections.Generic;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임의 기본 데이터(맵, 몬스터, 오브젝트 등)를 관리하는 매니저
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public Data.Game GameData;
        public Data.Skill[] SkillDataArray;
        public Data.Item[] ItemDataArray;
        public string[] CraftableItemIds;
        public Data.Reward[] RewardDataArray;

        // [Header("Config Data (ScriptableObject)")]
        // public MapConfig mapConfig;
        // public List<MonsterConfig> monsterConfigs;
        // public List<ObjectConfig> objectConfigs;
        // 필요에 따라 추가

        public Dictionary<string, Data.Skill> SkillDataDictionary;
        public Dictionary<string, Data.Item> ItemDataDictionary;
        public Dictionary<string, Data.Reward> RewardDataDictionary;

        void Awake()
        {
            InitializeSkillDataDictionary();
            InitializeItemDataDictionary();
            InitializeRewardDataDictionary();
        }

        void InitializeSkillDataDictionary()
        {
            SkillDataDictionary = new();
            foreach (var skill in SkillDataArray)
            {
                SkillDataDictionary.Add(skill.Id, skill);
            }
        }

        void InitializeItemDataDictionary()
        {
            ItemDataDictionary = new();
            foreach (var item in ItemDataArray)
            {
                ItemDataDictionary.Add(item.Id, item);
            }
        }

        void InitializeRewardDataDictionary()
        {
            RewardDataDictionary = new();
            foreach (var reward in RewardDataArray)
            {
                RewardDataDictionary.Add(reward.Id, reward);
            }
        }

        public void LoadAllData()
        {
            // ScriptableObject, JSON 등에서 데이터 로드
            // (ScriptableObject는 에디터에서 직접 할당, JSON 등은 Resources/Addressable 등에서 로드)
        }

        // 예시: 특정 몬스터/오브젝트 데이터 가져오기
        // public MonsterConfig GetMonsterConfig(string id)
        // {
        //     return monsterConfigs.Find(m => m.id == id);
        // }
        // public ObjectConfig GetObjectConfig(string id)
        // {
        //     return objectConfigs.Find(o => o.id == id);
        // }
    }
}
