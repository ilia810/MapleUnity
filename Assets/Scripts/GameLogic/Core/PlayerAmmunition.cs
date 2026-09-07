using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public partial class Player
    {
        public int AmmunitionId => AmmunitionRules.Select(EquippedWeaponType, inventory.GetItemsInSlotOrder());
        public int AmmunitionCount => inventory.FirstStackCount(AmmunitionId);
        public ItemInfo SelectedAmmunition { get { int id = AmmunitionId; return id == 0 ? null : GetItemInfo(id); } }
        public bool UsesAmmunition => AmmunitionRules.Prefix(EquippedWeaponType) != 0;
        public bool HasUsableAmmunition => AmmunitionCount > 0 && SelectedAmmunition?.Ammunition?.Frames.Length > 0;
        private int AmmunitionAttack => SelectedAmmunition?.Stats?.TryGetValue(StatType.WeaponAttack, out int attack) == true ? attack : 0;
    }
}
