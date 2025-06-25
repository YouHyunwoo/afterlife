using System.Collections.Generic;

namespace Afterlife.Model
{
    public class Inventory
    {
        Dictionary<string, int> items = new();

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

        public void RemoveItem(string itemId, int amount)
        {
            if (items.ContainsKey(itemId))
            {
                items[itemId] -= amount;
                if (items[itemId] <= 0)
                {
                    items.Remove(itemId);
                }
            }
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return items.ContainsKey(itemId) && items[itemId] >= amount;
        }

        public int GetItemCount(string itemId)
        {
            return items.ContainsKey(itemId) ? items[itemId] : 0;
        }

        
    }
}