using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    [Serializable]
    public sealed class InventoryStack
    {
        public int Slot, ItemId, Quantity;
        public int Category => ItemId / 1000000;
        public InventoryStack Copy() => new InventoryStack { Slot = Slot, ItemId = ItemId, Quantity = Quantity };
    }

    public class Inventory
    {
        private List<InventoryStack> stacks = new List<InventoryStack>();
        private IItemDataProvider data;
        public const int SlotsPerCategory = 30; // Six rows of five cells; existing saved slot addresses remain valid.
        public event Action<int, int> ItemAdded;
        public event Action<int, int> ItemRemoved;
        public event Action Changed;
        public long Revision { get; private set; }
        public void SetItemData(IItemDataProvider provider) => data = provider;
        public InventoryStack GetStack(int category, int slot) => stacks.FirstOrDefault(s => s.Category == category && s.Slot == slot)?.Copy();
        public IEnumerable<InventoryStack> GetStacks() => stacks.OrderBy(s => s.Category).ThenBy(s => s.Slot).Select(s => s.Copy()).ToArray();
        public IEnumerable<KeyValuePair<int, int>> GetItemsInSlotOrder() => GetStacks().Select(s => new KeyValuePair<int, int>(s.ItemId, s.Quantity));
        public int GetItemCount(int id) => stacks.Where(s => s.ItemId == id).Sum(s => s.Quantity);
        public int FirstStackCount(int id) => stacks.Where(s => s.ItemId == id).OrderBy(s => s.Slot).FirstOrDefault()?.Quantity ?? 0;
        public bool HasItem(int id) => GetItemCount(id) > 0;
        public Dictionary<int, int> GetAllItems() => stacks.GroupBy(s => s.ItemId).ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));
        public int UsedSlots(int category) => stacks.Count(s => s.Category == category);
        public bool Organize(int category, bool sort)
        {
            if(category<1||category>5)return false;
            var next=stacks.Select(s=>s.Copy()).ToList();
            var bag=next.Where(s=>s.Category==category);
            var ordered=sort?bag.OrderBy(s=>s.ItemId).ThenBy(s=>s.Slot):bag.OrderBy(s=>s.Slot);
            int slot=1;foreach(var stack in ordered.ToArray())stack.Slot=slot++;
            if(next.All(s=>stacks.Any(old=>old.Category==s.Category&&old.Slot==s.Slot&&old.ItemId==s.ItemId&&old.Quantity==s.Quantity)))return false;
            Commit(next,null,null);return true;
        }
        private int StackLimit(int id) => id / 1000000 == 1 ? 1 : Math.Max(1, Math.Min(32767, data?.GetItem(id)?.MaxStack > 0 ? data.GetItem(id).MaxStack : 100));

        public void AddItem(int id, int quantity) => TryAddItem(id, quantity);
        public bool TryAddItem(int id, int quantity) => TryExchange(null, new Dictionary<int, int> { [id] = quantity });
        public bool RemoveItem(int id, int quantity) => TryExchange(new Dictionary<int, int> { [id] = quantity }, null);

        // Preflight the entire exchange on a copy; full bags must never lose gear, loot or money.
        public bool CanExchange(IDictionary<int, int> remove, IDictionary<int, int> add) => Prepare(remove, add, out _);
        public bool TryExchange(IDictionary<int, int> remove, IDictionary<int, int> add)
        {
            if (!Prepare(remove, add, out var next)) return false;
            Commit(next, remove, add); return true;
        }
        // Slot addresses, rather than item IDs, identify the selected copy/stack in the source client.
        public bool TryExchangeSlot(int category, int slot, int expectedItemId, int quantity, IDictionary<int, int> add, int preferredReturnId = 0)
        {
            if (category != expectedItemId / 1000000 || slot < 1 || slot > SlotsPerCategory || quantity <= 0) return false;
            var selected = new InventoryStack { Slot = slot, ItemId = expectedItemId, Quantity = quantity };
            if (!Prepare(null, add, out var next, selected, preferredReturnId)) return false;
            Commit(next, new Dictionary<int, int> { [expectedItemId] = quantity }, add); return true;
        }
        public bool TryMoveStack(int category, int sourceSlot, int destinationSlot, int expectedItemId)
        {
            if (category < 1 || category > 5 || sourceSlot < 1 || sourceSlot > SlotsPerCategory ||
                destinationSlot < 1 || destinationSlot > SlotsPerCategory) return false;
            var source = stacks.FirstOrDefault(s => s.Category == category && s.Slot == sourceSlot);
            if (source == null || source.ItemId != expectedItemId) return false;
            if (sourceSlot == destinationSlot) return true;
            var destination = stacks.FirstOrDefault(s => s.Category == category && s.Slot == destinationSlot);
            if (destination != null && destination.ItemId == source.ItemId && category != 1)
            {
                // Offline resolution of a move request: fill the target up to its NX slotMax,
                // leaving overflow in the source. Moving never changes the total item count.
                int moved = Math.Min(source.Quantity, StackLimit(source.ItemId) - destination.Quantity);
                if (moved <= 0) return false;
                destination.Quantity += moved; source.Quantity -= moved;
                if (source.Quantity == 0) stacks.Remove(source);
            }
            else
            {
                source.Slot = destinationSlot;
                if (destination != null) destination.Slot = sourceSlot;
            }
            Revision++; Changed?.Invoke(); return true;
        }
        private void Commit(List<InventoryStack> next, IDictionary<int, int> remove, IDictionary<int, int> add)
        {
            stacks = next;
            Revision++;
            if (remove != null) foreach (var p in remove) ItemRemoved?.Invoke(p.Key, p.Value);
            if (add != null) foreach (var p in add) ItemAdded?.Invoke(p.Key, p.Value);
            Changed?.Invoke();
        }
        private bool Prepare(IDictionary<int, int> remove, IDictionary<int, int> add, out List<InventoryStack> next, InventoryStack selected = null, int preferredReturnId = 0)
        {
            next = stacks.Select(s => s.Copy()).ToList();
            if (selected != null)
            {
                var stack = next.FirstOrDefault(s => s.Category == selected.Category && s.Slot == selected.Slot && s.ItemId == selected.ItemId);
                if (stack == null || stack.Quantity < selected.Quantity) return false;
                stack.Quantity -= selected.Quantity;
                if (stack.Quantity == 0) next.Remove(stack);
            }
            if (remove != null) foreach (var p in remove)
            {
                if (p.Value <= 0 || next.Where(s => s.ItemId == p.Key).Sum(s => s.Quantity) < p.Value) return false;
                int left = p.Value;
                foreach (var stack in next.Where(s => s.ItemId == p.Key).OrderBy(s => s.Slot))
                { int take = Math.Min(left, stack.Quantity); stack.Quantity -= take; left -= take; }
                next.RemoveAll(s => s.Quantity == 0);
            }
            if (add != null) foreach (var p in add.OrderBy(p => p.Key == preferredReturnId ? 0 : 1))
            {
                int category = p.Key / 1000000;
                // Keep legacy/imported IDs even when their art is unavailable. Local loot/shop
                // entry points validate their metadata; inventory storage must not discard records.
                if (category < 1 || category > 5 || p.Value <= 0) return false;
                if (data?.GetItem(p.Key)?.IsOneOfAKind == true && (p.Value > 1 || next.Any(s => s.ItemId == p.Key))) return false;
                int limit = StackLimit(p.Key), left = p.Value;
                foreach (var stack in next.Where(s => s.ItemId == p.Key).OrderBy(s => s.Slot))
                { int take = Math.Min(left, limit - stack.Quantity); stack.Quantity += take; left -= take; }
                var occupied = new HashSet<int>(next.Where(s => s.Category == category).Select(s => s.Slot));
                // An equipment swap puts the replaced piece back into the chosen source slot.
                // Other displaced pieces use the remaining gaps only after that slot is filled.
                for (int candidate = 0; left > 0 && candidate <= SlotsPerCategory; candidate++)
                {
                    int slot = candidate == 0 && selected?.Category == category ? selected.Slot : candidate;
                    if (slot < 1 || occupied.Contains(slot)) continue;
                    int take = Math.Min(left, limit);
                    next.Add(new InventoryStack { Slot = slot, ItemId = p.Key, Quantity = take }); occupied.Add(slot); left -= take;
                }
                if (left > 0) return false;
            }
            return true;
        }
        public bool CanRestore(InventoryStack[] values)
        {
            if (values == null || values.Length > SlotsPerCategory * 5) return false;
            var slots = new HashSet<int>(); var unique = new HashSet<int>();
            foreach (var s in values)
            {
                if (s == null || s.Category < 1 || s.Category > 5 || s.Slot < 1 || s.Slot > SlotsPerCategory ||
                    s.Quantity < 1 || s.Quantity > StackLimit(s.ItemId) || !slots.Add(s.Category * 100 + s.Slot) ||
                    (data != null && data.GetItem(s.ItemId) == null)) return false;
                if (data?.GetItem(s.ItemId)?.IsOneOfAKind == true && (s.Quantity > 1 || !unique.Add(s.ItemId))) return false;
            }
            return true;
        }
        public bool Restore(InventoryStack[] values)
        {
            if (!CanRestore(values)) return false;
            stacks = values.Select(s => s.Copy()).ToList(); Revision++; Changed?.Invoke(); return true;
        }
    }
}
