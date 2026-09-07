// Inventory::recalc_stats / RegularAttack::can_use from HeavenClient. AGPL-3.0-or-later.
using System.Collections.Generic;

namespace MapleClient.GameLogic.Data
{
    public static class AmmunitionRules
    {
        public static int Prefix(int weaponType)
        {
            switch (weaponType) { case 145: return 2060; case 146: return 2061; case 147: return 2070; case 149: return 2330; default: return 0; }
        }
        public static bool IsAmmunition(int itemId) => itemId / 1000 == 2060 || itemId / 1000 == 2061 || itemId / 1000 == 2070 || itemId / 1000 == 2330;
        public static int Select(int weaponType, IEnumerable<KeyValuePair<int, int>> inventoryInSlotOrder)
        {
            int prefix = Prefix(weaponType);
            if (prefix != 0)
                foreach (var item in inventoryInSlotOrder) if (item.Value > 0 && item.Key / 1000 == prefix) return item.Key;
            return 0;
        }
    }
}
