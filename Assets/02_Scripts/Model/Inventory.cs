using System.Collections;
using System.Collections.Generic;

namespace Afterlife.Model
{
    public class Inventory : IEnumerable<KeyValuePair<string, int>>
    {
        readonly Dictionary<string, int> items = new();

        public int this[string itemId]
        {
            get => items.ContainsKey(itemId) ? items[itemId] : 0;
            set => items[itemId] = value;
        }

        public void AddItem(string itemId, int amount)
        {
            if (items.ContainsKey(itemId))
            {
                items[itemId] += amount;
            }
            else
            {
                items[itemId] = amount;
            }
        }

        public bool RemoveItem(string itemId, int amount, out int removedAmount)
        {
            if (items.ContainsKey(itemId))
            {
                removedAmount = System.Math.Min(items[itemId], amount);
                items[itemId] -= removedAmount;
                if (items[itemId] <= 0) { items.Remove(itemId); }

                return true;
            }
            else
            {
                removedAmount = 0;
                return false;
            }
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return items.ContainsKey(itemId) && items[itemId] >= amount;
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}