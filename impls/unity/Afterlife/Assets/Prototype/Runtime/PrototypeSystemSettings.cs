using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Settings/System")]
    public sealed class PrototypeSystemSettings : ScriptableObject
    {
        [Header("6일 MVP · 본편 전용")]
        public bool MvpSlots;
        [Min(1)] public int InitialSlots = 1;
        [Range(1,3)] public int MaximumSlots = 3;
        [Min(.1f)] public float SlotRechargeSeconds = 10;
        [Min(1)] public float FirstDaySeconds = 90;
        [Min(1)] public float SecondDaySeconds = 60;
        [HideInInspector] public float FirstPreparation = 60;
        [HideInInspector] public float Maintenance = 45;
        public float CursorRadius = .65f;
        public float HouseRadius = 3.8f;
        public float WoodPerSecond = 2;
        public float StonePerSecond = 1;
        public float CursorDamage = 8;
        public float RepairPerSecond = 12;
        public float WarriorSeconds = 10;
        [HideInInspector] public int WarriorMoney = 10;
        public float DaySeconds = 35;
        public float DuskSeconds = 5;
        public float NightSeconds = 35;
        public float ContributionSeconds = 1;
        public float ContributionPower = 1;
        public float DivinePowerMaximum = 100;
        public float DivineFieldCost = 30;
        public float DivinePowerRegeneration = 3;
        public float DivineFieldDuration = 8;
        public float CursorVision = 4;
        public float VillageVision = 9;
        public float VisionTrailSeconds = 1.4f;
        public float AutoConstruction = .25f;
        public int PrototypeDays = 6;
    }
}
