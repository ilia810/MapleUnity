using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.GameLogic.Core
{
    public partial class Player
    {
        private IItemDataProvider itemData;
        private readonly Dictionary<StatType, int> equipmentBonuses = new Dictionary<StatType, int>();
        public event Action EquipmentChanged;
        public event Action<int> ItemUsed;
        public int Gender { get; set; } // Source male=0, female=1; item gender 2 fits either.
        public void SetItemData(IItemDataProvider data) { itemData = data; inventory.SetItemData(data); RecalculateEquipment(); }
        public ItemInfo GetItemInfo(int id) => itemData?.GetItem(id);
        public WeaponProfile CurrentWeapon => equippedItems.TryGetValue(EquipSlot.Weapon, out int id) ? GetItemInfo(id)?.Weapon : null;
        public int EquipmentBonus(StatType stat) => equipmentBonuses.TryGetValue(stat, out int value) ? value : 0;

        public bool TryUseItem(int id, out string message, int bagSlot = 0)
        {
            if (IsDead) { message = "Revive before using items."; return false; }
            if (!HasBagItem(id, bagSlot)) { message = "This item is no longer in the selected bag slot."; return false; }
            var item = GetItemInfo(id);
            if (item == null) { message = "Item data is unavailable."; return false; }
            if ((!item.IsRecoveryConsumable && !item.IsStatBuffConsumable) ||
                (item.IsStatBuffConsumable && !ValidStatBuffs(item.Buffs,item.Time))) { message = "This item's effect is not available yet."; return false; }
            long restoreHp = Math.Max(0, item.Hp) + (long)maxHp * Math.Max(0, Math.Min(100, item.HpRate)) / 100;
            long restoreMp = Math.Max(0, item.Mp) + (long)maxMp * Math.Max(0, Math.Min(100, item.MpRate)) / 100;
            int gainHp = (int)Math.Min(maxHp - hp, restoreHp), gainMp = (int)Math.Min(maxMp - mp, restoreMp);
            if (gainHp <= 0 && gainMp <= 0 && !item.IsStatBuffConsumable) { message = "This item would not restore any HP or MP."; return false; }
            bool removed = bagSlot == 0 ? inventory.RemoveItem(id, 1) : inventory.TryExchangeSlot(id / 1000000, bagSlot, id, 1, null);
            if (!removed) { message = "This item is no longer in the selected bag slot."; return false; }
            hp += gainHp; mp += gainMp;
            if (item.IsStatBuffConsumable) ApplyStatBuffs(-id, item.Name, item.Buffs, item.Time);
            ItemUsed?.Invoke(id);
            message = $"Used {item.Name}.";
            return true;
        }

        private bool HasBagItem(int id, int bagSlot) => bagSlot == 0 ? inventory.HasItem(id) :
            bagSlot > 0 && inventory.GetStack(id / 1000000, bagSlot)?.ItemId == id;

        public bool TryEquipItem(int id, out string message, int bagSlot = 0)
        {
            if (IsDead) { message = "Revive before changing equipment."; return false; }
            if (IsBasicAttacking) { message = "Finish the attack before changing equipment."; return false; }
            if (!HasBagItem(id, bagSlot)) { message = "This item is no longer in the selected bag slot."; return false; }
            var item = GetItemInfo(id);
            if (item == null || item.Type != AssetItemType.Equip || !item.EquipmentSlot.HasValue)
            { message = "This item cannot be equipped."; return false; }
            var slot = item.EquipmentSlot.Value;
            if (!SupportedSlot(slot) || (slot == EquipSlot.Weapon && (!(WeaponProfile.SupportsMelee(id) || WeaponProfile.SupportsRanged(id)) ||
                (item.Weapon != null && WeaponProfile.AttackStances(item.Weapon.AttackType).Count == 0))))
            { message = "This equipment type is not available yet."; return false; }
            if (item.Gender != 2 && item.Gender != Gender) { message = "This item does not fit this character."; return false; }
            int family = JobId / 100 % 10;
            bool jobMatches = item.RequiredJobMask == 0 ||
                (item.RequiredJobMask == -1 ? JobId % 1000 == 0 : family >= 1 && family <= 5 && (item.RequiredJobMask & (1 << (family - 1))) != 0);
            if (Level < item.RequiredLevel || !jobMatches) { message = "Level or job requirements are not met."; return false; }

            var removed = new HashSet<EquipSlot> { slot };
            if (slot == EquipSlot.Weapon && item.IsTwoHanded) removed.Add(EquipSlot.Shield);
            if (slot == EquipSlot.Shield && equippedItems.TryGetValue(EquipSlot.Weapon, out int weapon) && GetItemInfo(weapon)?.IsTwoHanded == true)
                removed.Add(EquipSlot.Weapon);
            if (item.IsOverall) removed.Add(EquipSlot.Bottom);
            if (slot == EquipSlot.Bottom && equippedItems.TryGetValue(EquipSlot.Top, out int top) && GetItemInfo(top)?.IsOverall == true)
                removed.Add(EquipSlot.Top);
            // Requirements exclude every item that will be replaced, including an overall.
            var retained = SumEquipment(equippedItems.Where(e => !removed.Contains(e.Key)).Select(e => e.Value));
            int Bonus(StatType stat) => retained.TryGetValue(stat, out int value) ? value : 0;
            if (baseSTR + Bonus(StatType.STR) < item.RequiredStr || baseDEX + Bonus(StatType.DEX) < item.RequiredDex ||
                baseINT + Bonus(StatType.INT) < item.RequiredInt || baseLUK + Bonus(StatType.LUK) < item.RequiredLuk)
            { message = "Stat requirements are not met without the replaced gear."; return false; }
            var returned = equippedItems.Where(e => removed.Contains(e.Key)).GroupBy(e => e.Value).ToDictionary(g => g.Key, g => g.Count());
            int replacedId = equippedItems.TryGetValue(slot, out int replaced) ? replaced : 0;
            bool exchanged = bagSlot == 0 ? inventory.TryExchange(new Dictionary<int, int> { [id] = 1 }, returned) :
                inventory.TryExchangeSlot(1, bagSlot, id, 1, returned, replacedId);
            if (!exchanged)
            { message = "Make room in your equipment bag first."; return false; }
            foreach (var oldSlot in removed) equippedItems.Remove(oldSlot);
            equippedItems[slot] = id;
            RecalculateEquipment();
            message = $"Equipped {item.Name}.";
            return true;
        }

        public bool TryUnequipItem(EquipSlot slot, out string message)
        {
            if (IsDead) { message = "Revive before changing equipment."; return false; }
            if (IsBasicAttacking) { message = "Finish the attack before changing equipment."; return false; }
            if (!equippedItems.TryGetValue(slot, out int id)) { message = "That slot is empty."; return false; }
            if (!inventory.TryAddItem(id, 1)) { message = "Your equipment bag is full."; return false; }
            equippedItems.Remove(slot); RecalculateEquipment();
            message = $"Removed {GetItemInfo(id)?.Name ?? "equipment"}.";
            return true;
        }

        private static bool SupportedSlot(EquipSlot slot) => slot == EquipSlot.Hat || slot == EquipSlot.Top ||
            slot == EquipSlot.Bottom || slot == EquipSlot.Shoes || slot == EquipSlot.Glove || slot == EquipSlot.Cape ||
            slot == EquipSlot.FaceAccessory || slot == EquipSlot.EyeAccessory || slot == EquipSlot.Earring ||
            slot == EquipSlot.Shield || slot == EquipSlot.Weapon;
        private Dictionary<StatType, int> SumEquipment(IEnumerable<int> ids)
        {
            var bonuses = new Dictionary<StatType, int>();
            foreach (int id in ids)
                foreach (var stat in GetItemInfo(id)?.Stats ?? new Dictionary<StatType, int>())
                    bonuses[stat.Key] = (bonuses.TryGetValue(stat.Key, out int value) ? value : 0) + stat.Value;
            return bonuses;
        }
        private void RecalculateEquipment()
        {
            equipmentBonuses.Clear();
            foreach (var stat in SumEquipment(equippedItems.Values)) equipmentBonuses[stat.Key] = stat.Value;
            hp = Math.Min(hp, maxHp); mp = Math.Min(mp, maxMp);
            SynchronizeBodyStance();
            OnStatsChanged(); EquipmentChanged?.Invoke();
        }
    }
}
