namespace MapleClient.GameLogic
{
    public class ItemTemplate
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int MaxStack { get; set; }
        public ItemStats Stats { get; set; }
        public int Price { get; set; }
    }

    public enum ItemType
    {
        Equipment,
        Consumable,
        Etc,
        Setup,
        Cash
    }

    public class ItemStats
    {
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int Luck { get; set; }
        public int HP { get; set; }
        public int MP { get; set; }
        public int WeaponAttack { get; set; }
        public int MagicAttack { get; set; }
        public int WeaponDefense { get; set; }
        public int MagicDefense { get; set; }
        public int Accuracy { get; set; }
        public int Avoidability { get; set; }
        public int Speed { get; set; }
        public int Jump { get; set; }
    }
}