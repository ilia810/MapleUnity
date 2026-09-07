using System;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public enum QuickslotKind { Empty, Skill, Item, Action, KeyReference }
    public enum QuickslotAction
    {
        Equipment = 0, Inventory = 1, Stats = 2, Skills = 3, WorldMap = 5, Minimap = 7,
        Quests = 8, Keyboard = 9, Menu = 14, Quickslots = 15, Attack = 52, Jump = 53,
        Hit = 100, Smile = 101, Troubled = 102, Cry = 103, Angry = 104, Bewildered = 105, Stunned = 106,
        Left = 200, Right = 201, Up = 202, Down = 203, Talk = 204, Shop = 205, Screenshot = 206
    }

    [Serializable]
    public sealed class QuickslotBinding
    {
        public QuickslotKind Kind;
        public int Id;
        public QuickslotBinding() { }
        public QuickslotBinding(QuickslotKind kind, int id) { Kind = kind; Id = id; }
        public QuickslotBinding Copy() => new QuickslotBinding(Kind, Id);
        public static bool SupportsItem(ItemInfo item) => item != null && item.Type == MapleClient.GameLogic.Interfaces.ItemType.Use &&
            (item.IsRecoveryConsumable || item.IsStatBuffConsumable);
        public static bool IsAction(int id) => Enum.IsDefined(typeof(QuickslotAction), id);
    }

    public partial class GameWorld
    {
        private bool CanRestoreQuickslots(LocalProgress save)
        {
            if (save.Version < 4) return true;
            return save.Quickslots != null && save.Quickslots.Length <= 8 && save.Quickslots.All(b => CanRestoreBinding(b, save));
        }
        private bool CanRestoreBinding(QuickslotBinding b, LocalProgress save) => b != null &&
                (b.Kind == QuickslotKind.Empty ? b.Id == 0 :
                 b.Kind == QuickslotKind.Item ? QuickslotBinding.SupportsItem(player.GetItemInfo(b.Id)) :
                 b.Kind == QuickslotKind.Action ? QuickslotBinding.IsAction(b.Id) :
                 b.Kind == QuickslotKind.Skill && save.Skills.Any(s => s.Id == b.Id && s.Level > 0) && assetProvider.SkillData.GetSkill(b.Id)?.IsPassive == false);
    }
}
