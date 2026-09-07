using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public sealed class ShopOffer
    {
        public int ItemId { get; }
        public int Quantity { get; }
        public int Price { get; }
        public int BagSlot { get; }
        public ShopOffer(int id, int quantity, int price, int bagSlot = 0) { ItemId = id; Quantity = quantity; Price = price; BagSlot = bagSlot; }
    }
    // Client NX contains item metadata, not server loot/shop tables. These are explicit local rules.
    public static class OfflineEconomy
    {
        public const int ShopNpcId = 1011100; // Luna, Henesys General Store (100000102).
        public static IReadOnlyList<ShopOffer> Stock { get; } = Array.AsReadOnly(new[] {
            new ShopOffer(2000000, 1, 50), new ShopOffer(2000001, 1, 160), new ShopOffer(2000003, 1, 200),
            new ShopOffer(2060000, 100, 100), new ShopOffer(2061000, 100, 100),
            new ShopOffer(2070000, 100, 100), new ShopOffer(2330000, 100, 100),
            new ShopOffer(1302000, 1, 50), new ShopOffer(1072001, 1, 100)
        });
        public static List<DropInfo> Drops(int monsterId, int level, Func<int, ItemInfo> itemData)
        {
            var result = new List<DropInfo> { new DropInfo { ItemId = 0, Quantity = Math.Max(5, Math.Min(1000, level * 5)), DropRate = 1 } };
            int material = monsterId == 100100 ? 4000019 : monsterId == 100101 ? 4000000 :
                monsterId == 130101 ? 4000016 : monsterId == 1210102 ? 4000001 : monsterId == 120100 ? 4000011 : 0;
            if (material != 0 && itemData(material) != null) result.Add(new DropInfo { ItemId = material, Quantity = 1, DropRate = 1 });
            if (itemData(2000000) != null) result.Add(new DropInfo { ItemId = 2000000, Quantity = 1, DropRate = .2f });
            return result;
        }
        // ItemData::price is the sale price. Rechargeable ammo needs whole-stack accounting;
        // this local shop sells packs but won't buy ammo until that system is available.
        public static int SellPrice(ItemInfo item) => item == null || item.IsCash || item.IsQuest || item.IsUnsellable ||
            Data.AmmunitionRules.IsAmmunition(item.ItemId) ? 0 : Math.Max(0, item.Price);
    }
    public partial class Player
    {
        public int Mesos { get; private set; }
        public event Action MesosChanged;
        public bool TryGainMesos(int amount)
        {
            if (amount <= 0 || (long)Mesos + amount > int.MaxValue) return false;
            Mesos += amount; MesosChanged?.Invoke(); return true;
        }
        public bool TrySpendMesos(int amount)
        {
            if (amount < 0 || amount > Mesos) return false;
            Mesos -= amount; MesosChanged?.Invoke(); return true;
        }
    }
    public partial class GameWorld
    {
        public bool IsLocal => networkClient == null;
        public event Action<DroppedItem> DropRemoved;
        public NpcSpawn NearestShop => currentMap?.NpcSpawns.Where(CanUseShop).OrderBy(n => Math.Abs(n.X / 100 - player.Position.X)).FirstOrDefault();
        public bool CanUseShop(NpcSpawn npc)
        {
            return npc?.NpcId == OfflineEconomy.ShopNpcId && CanTalkToNpc(npc);
        }
        public bool BuyFromShop(NpcSpawn npc, int itemId, int packs, out string message)
        {
            if (!CanUseShop(npc)) { message = "Stand near Luna while idle to trade."; return false; }
            var offer = OfflineEconomy.Stock.FirstOrDefault(s => s.ItemId == itemId);
            if (offer == null || packs < 1 || packs > 100 || player.GetItemInfo(itemId) == null)
            { message = "That purchase is unavailable."; return false; }
            long price = (long)offer.Price * packs;
            if (price > player.Mesos) { message = "You need more mesos."; return false; }
            var add = new Dictionary<int, int> { [itemId] = offer.Quantity * packs };
            if (!player.Inventory.CanExchange(null, add)) { message = "Make room in your bag first."; return false; }
            player.TrySpendMesos((int)price); player.Inventory.TryExchange(null, add);
            message = $"Bought {offer.Quantity * packs} {player.GetItemInfo(itemId).Name} for {price} mesos."; return true;
        }
        public bool SellToShop(NpcSpawn npc, int itemId, int quantity, out string message, int bagSlot = 0)
        {
            if (!CanUseShop(npc)) { message = "Stand near Luna while idle to trade."; return false; }
            int unitPrice = OfflineEconomy.SellPrice(player.GetItemInfo(itemId));
            long value = (long)unitPrice * quantity;
            var stack = bagSlot > 0 ? player.Inventory.GetStack(itemId / 1000000, bagSlot) : null;
            int available = bagSlot == 0 ? player.Inventory.GetItemCount(itemId) : stack?.ItemId == itemId ? stack.Quantity : 0;
            if (bagSlot < 0 || quantity <= 0 || quantity > available || unitPrice == 0)
            { message = "That item or quantity cannot be sold."; return false; }
            if (value + player.Mesos > int.MaxValue) { message = "Your meso wallet is full."; return false; }
            bool removed = bagSlot == 0 ? player.Inventory.RemoveItem(itemId, quantity) :
                player.Inventory.TryExchangeSlot(itemId / 1000000, bagSlot, itemId, quantity, null);
            if (!removed) { message = "This item is no longer in the selected bag slot."; return false; }
            player.TryGainMesos((int)value);
            message = $"Sold {quantity} {player.GetItemInfo(itemId).Name} for {value} mesos."; return true;
        }
    }
}
