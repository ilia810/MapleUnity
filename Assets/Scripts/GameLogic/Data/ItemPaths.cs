// Paths/categories from HeavenClient Data/ItemData.cpp and EquipData.cpp.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
namespace MapleClient.GameLogic.Data
{
    public static class ItemPaths
    {
        public static string EquipmentCategory(int id)
        {
            switch (id / 10000)
            {
                case 100: return "Cap";
                case 101: case 102: case 103: case 112: case 113: case 114: return "Accessory";
                case 104: return "Coat"; case 105: return "Longcoat"; case 106: return "Pants";
                case 107: return "Shoes"; case 108: return "Glove"; case 109: return "Shield";
                case 110: return "Cape"; case 111: return "Ring";
                default: return id / 10000 >= 130 && id / 10000 <= 170 ? "Weapon" : null;
            }
        }
        public static EquipSlot? Slot(int id)
        {
            switch (id / 10000)
            {
                case 100: return EquipSlot.Hat; case 101: return EquipSlot.FaceAccessory;
                case 102: return EquipSlot.EyeAccessory; case 103: return EquipSlot.Earring;
                case 104: case 105: return EquipSlot.Top; case 106: return EquipSlot.Bottom;
                case 107: return EquipSlot.Shoes; case 108: return EquipSlot.Glove;
                case 109: return EquipSlot.Shield; case 110: return EquipSlot.Cape;
                case 111: return EquipSlot.Ring1; case 112: return EquipSlot.Pendant;
                case 113: return EquipSlot.Belt; case 114: return EquipSlot.Medal;
                default: return id / 10000 >= 130 && id / 10000 <= 149 ? EquipSlot.Weapon : (EquipSlot?)null;
            }
        }
        public static string File(int id) => id / 1000000 == 1 ? "character" : "item";
        public static string Node(int id)
        {
            string category;
            switch (id / 1000000)
            {
                case 1: category = EquipmentCategory(id); return category == null ? null : $"{category}/{id:D8}.img";
                case 2: category = "Consume"; break; case 3: category = "Install"; break;
                case 4: category = "Etc"; break; case 5: category = "Cash"; break;
                default: return null;
            }
            return $"{category}/{id / 10000:D4}.img/{id:D8}";
        }
        public static string StringNode(int id)
        {
            switch (id / 1000000)
            {
                case 1: return $"Eqp.img/Eqp/{EquipmentCategory(id)}/{id}";
                case 2: return $"Consume.img/{id}"; case 3: return $"Ins.img/{id}";
                case 4: return $"Etc.img/Etc/{id}"; case 5: return $"Cash.img/{id}";
                default: return null;
            }
        }
    }
}
