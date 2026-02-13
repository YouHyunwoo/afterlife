using System.Collections.Generic;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임의 기본 데이터(맵, 몬스터, 오브젝트 등)를 관리하는 매니저
    /// </summary>
    public class DataManager : ManagerBase
    {
        public Data.Game GameData;
        public Data.Skill[] SkillDataArray;
        public Data.Item[] ItemDataArray;
        public string[] CraftableItemIds;
        public Data.Reward[] RewardDataArray;
        public Data.Upgrade[] UpgradeDataArray;

        public Dictionary<string, Data.Skill> SkillDataDictionary;
        public Dictionary<string, Data.Item> ItemDataDictionary;
        public Dictionary<string, Data.Reward> RewardDataDictionary;
        public Dictionary<string, Data.Upgrade> UpgradeDataDictionary;

        void Awake()
        {
            InitializeSkillDataDictionary();
            InitializeItemDataDictionary();
            InitializeRewardDataDictionary();
            InitializeUpgradeDataDictionary();
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

        void InitializeUpgradeDataDictionary()
        {
            UpgradeDataDictionary = new();
            foreach (var upgrade in UpgradeDataArray)
            {
                UpgradeDataDictionary.Add(upgrade.Id, upgrade);
            }
        }

        public Data.Item FindItemData(string itemId)
        {
            if (ItemDataDictionary.TryGetValue(itemId, out var itemData))
            {
                return itemData;
            }
            else
            {
                throw new System.Exception($"Item data for {itemId} not found.");
            }
        }
    }
}
