using System.Collections.Generic;

namespace Afterlife.Model
{

    public class Player
    {
        public float AttackPower;
        public float AttackSpeed;
        public float AttackRange;
        public float AttackCount;
        public float CriticalRate;
        public float CriticalDamageMultiplier;

        public Dictionary<string, int> Inventory = new();
    }
}