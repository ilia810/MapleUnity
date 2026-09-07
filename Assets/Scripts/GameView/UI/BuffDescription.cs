using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameView.UI
{
    internal static class BuffDescription
    {
        public static string Describe(BuffType type, int value)
        {
            string amount = value.ToString("+0;-0;0");
            switch (type)
            {
                case BuffType.MaxHPPercent: return "Max HP " + amount + "%";
                case BuffType.MaxMPPercent: return "Max MP " + amount + "%";
                case BuffType.MagicGuard: return value + "% of contact damage uses MP";
                case BuffType.WeaponAttack: return "Weapon attack " + amount;
                case BuffType.MagicAttack: return "Magic attack " + amount;
                case BuffType.WeaponDefense: return "Weapon defense " + amount;
                case BuffType.MagicDefense: return "Magic defense " + amount;
                case BuffType.Booster: return "Attack speed " + amount;
                case BuffType.Hide: return "Concealed: no contact damage; cannot attack";
                case BuffType.Accuracy: return "Accuracy " + amount;
                case BuffType.Avoidability: return "Avoidability " + amount;
                default: return type + " " + amount;
            }
        }
    }
}
