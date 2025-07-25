using System;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Core
{
    public class Inventory
    {
        private Dictionary<int, int> items = new Dictionary<int, int>();

        public event Action<int, int> ItemAdded;
        public event Action<int, int> ItemRemoved;

        public void AddItem(int itemId, int quantity)
        {
            if (quantity <= 0) return;

            if (items.ContainsKey(itemId))
            {
                items[itemId] += quantity;
            }
            else
            {
                items[itemId] = quantity;
            }

            ItemAdded?.Invoke(itemId, quantity);
        }

        public bool RemoveItem(int itemId, int quantity)
        {
            if (quantity <= 0) return false;

            if (!items.ContainsKey(itemId) || items[itemId] < quantity)
            {
                return false;
            }

            items[itemId] -= quantity;
            
            if (items[itemId] == 0)
            {
                items.Remove(itemId);
            }

            ItemRemoved?.Invoke(itemId, quantity);
            return true;
        }

        public int GetItemCount(int itemId)
        {
            return items.ContainsKey(itemId) ? items[itemId] : 0;
        }

        public bool HasItem(int itemId)
        {
            return items.ContainsKey(itemId) && items[itemId] > 0;
        }

        public Dictionary<int, int> GetAllItems()
        {
            return new Dictionary<int, int>(items);
        }
    }
}