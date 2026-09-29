namespace Afterlife.Prototype
{
    public enum Kind { House, Farm, Market, Barracks, LargeTest, Research, Altar, Tree, Rock, Warrior, Archer, Mage, Knight, Slime, Animal, Treasure, Goblin, GoblinWarrior, GoblinShaman, Orc, Lizard, ArrowTower, MagicTower, Wall, Chest, AbandonedAltar, Ruins, TownHall, Sanctuary, ArcheryRange, KnightHall, MageTower }
    public enum ProductionKind { None, Gold, Troop, Etheron }
    public enum Upgrade { Pulse, Residents, Construction, CriticalChance, CriticalPower, WallHealth, ArrowDamage, ArrowRange, MagicDamage, MagicRange, MultiTarget, Ignite, FireImpact, BurnDamage, BurnDuration }
    public enum LootKind { SlimeGel,Club,Bone,Armor,Staff,Bracer,Tail,Meat,Fur }
    public enum RelicKind { Wood,Stone,Critical,Repair,Trade,ResourceCapacity,ResourceRegrowth,FieldRadius,SlotRecharge,FieldSlot }
    public enum ChestGrade { Common,Rare,Ancient }
    public enum MemoryUpgrade { Work, House, Army }
    [System.Serializable]
    public sealed class Balance
    {
        public PrototypeRules Rules = new PrototypeRules();
        public bool MvpSlots;
        public int InitialSlots=1, MaximumSlots=3;
        public float SlotRechargeSeconds=10, FirstDaySeconds=90, SecondDaySeconds=60;
        public float DayDuration(int day) => MvpSlots ? (day == 1 ? FirstDaySeconds : day == 2 ? SecondDaySeconds : DaySeconds) : DaySeconds;
        public float FirstPreparation=60, Maintenance=45, CursorRadius=.65f, HouseRadius=3.8f;
        public float WoodRate=2, StoneRate=1, CursorDamage=8, RepairRate=12, WarriorSeconds=10;
        public int WarriorMoney=10;
        public float DaySeconds=35, DuskSeconds=5, NightSeconds=35, PulseSeconds=1, ContributionPower=1;
        public float DivinePowerMaximum=100, DivineFieldCost=30, DivinePowerRegeneration=3, DivineFieldDuration=8;
        public float CursorVision=4, VillageVision=9, VisionTrailSeconds=1.4f;
        public float AutoConstruction=.25f;
        public int MapSeed=0;
        public int MapRadius=45, Days=6,TreeLimit=780,RockLimit=480;public float ResourceSpawnSeconds=12;
        public int FertileSiteCount=2, LeylineSiteCount=2, FertileSiteSize=3, LeylineSiteSize=3, EconomicSiteMinimumDistance=8;
    }
}
