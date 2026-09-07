using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.GameData
{
    /// <summary>Lazy Item/Character/String metadata. Icons are decoded only when requested by a view.</summary>
    public sealed class NxItemDataProvider : IItemDataProvider
    {
        private readonly NXDataManager manager;
        private readonly Dictionary<int, ItemInfo> items = new Dictionary<int, ItemInfo>();
        public NxItemDataProvider(NXDataManager manager) { this.manager = manager; }
        private static int Int(INxNode node, string key, int fallback = 0) => node?[key]?.GetValue<int>() ?? fallback;
        public ItemInfo GetItem(int id)
        {
            if (items.TryGetValue(id, out var cached)) return cached;
            string path = ItemPaths.Node(id);
            if (path == null) return null;
            var node = manager.GetNode(ItemPaths.File(id), path);
            var info = node?["info"];
            if (info == null) return null;
            var text = manager.GetNode("string", ItemPaths.StringNode(id));
            var item = new ItemInfo {
                ItemId = id, Type = (AssetItemType)(id / 1000000 - 1),
                Name = text?["name"]?.GetValue<string>() ?? $"Item {id}", Description = text?["desc"]?.GetValue<string>() ?? "",
                Price = Int(info, "price"), MaxStack = id / 1000000 == 1 ? 1 : Int(info, "slotMax", 100),
                IsCash = Int(info, "cash") != 0, IsQuest = Int(info, "quest") != 0,
                IsTradeable = Int(info, "tradeBlock") == 0, IsOneOfAKind = Int(info, "only") != 0,
                IsUnsellable = Int(info, "notSale") != 0,
                RequiredLevel = Int(info, "reqLevel"), RequiredStr = Int(info, "reqSTR"), RequiredDex = Int(info, "reqDEX"),
                RequiredInt = Int(info, "reqINT"), RequiredLuk = Int(info, "reqLUK"), RequiredJobMask = Int(info, "reqJob"),
                Gender = id / 1000000 == 1 ? System.Math.Min(2, id / 1000 % 10) : 2,
                EquipmentSlot = ItemPaths.Slot(id), IsOverall = id / 10000 == 105,
                IsTwoHanded = WeaponProfile.UsesTwoHands(id),
                Slots = Int(info, "tuc"), Stats = new Dictionary<StatType, int>(), Buffs = new Dictionary<BuffType, int>(),
                IconPath = ItemPaths.File(id) + "/" + path + "/info/icon"
            };
            if (item.EquipmentSlot == EquipSlot.Weapon) item.Weapon = LoadWeapon(info, item.IsTwoHanded);
            if (AmmunitionRules.IsAmmunition(id)) item.Ammunition = NxEffectFrames.Read(manager, "item", path + "/bullet");
            var fields = new Dictionary<StatType, string> {
                [StatType.STR]="incSTR", [StatType.DEX]="incDEX", [StatType.INT]="incINT", [StatType.LUK]="incLUK",
                [StatType.MaxHP]="incMHP", [StatType.MaxMP]="incMMP", [StatType.WeaponAttack]="incPAD", [StatType.MagicAttack]="incMAD",
                [StatType.WeaponDefense]="incPDD", [StatType.MagicDefense]="incMDD", [StatType.Accuracy]="incACC", [StatType.Avoidability]="incEVA",
                [StatType.Hands]="incHANDS", [StatType.Speed]="incSPEED", [StatType.Jump]="incJUMP"
            };
            foreach (var field in fields) { int value = Int(info, field.Value); if (value != 0) item.Stats[field.Key] = value; }
            var spec = node["spec"];
            item.Hp = Int(spec, "hp"); item.Mp = Int(spec, "mp"); item.HpRate = Int(spec, "hpR"); item.MpRate = Int(spec, "mpR");
            item.Time = Int(spec, "time");
            // Do not consume a buff, scroll, teleport, cure, etc. while silently dropping its unported effects.
            var recoveryFields = new HashSet<string> { "hp", "mp", "hpR", "mpR" };
            item.IsRecoveryConsumable = item.Type == AssetItemType.Use && spec != null &&
                spec.Children.All(n => recoveryFields.Contains(n.Name)) &&
                (item.Hp > 0 || item.Mp > 0 || item.HpRate > 0 || item.MpRate > 0);
            var buffFields = new Dictionary<string, BuffType> {
                ["pad"] = BuffType.WeaponAttack, ["mad"] = BuffType.MagicAttack,
                ["pdd"] = BuffType.WeaponDefense, ["mdd"] = BuffType.MagicDefense,
                ["speed"] = BuffType.Speed, ["jump"] = BuffType.Jump,
                ["acc"] = BuffType.Accuracy, ["eva"] = BuffType.Avoidability
            };
            foreach (var field in buffFields)
                if (spec?[field.Key] != null) item.Buffs[field.Value] = Int(spec, field.Key);
            // Item spec/time is milliseconds in these NX files (e.g. 180000 = 3 minutes).
            item.IsStatBuffConsumable = item.Type == AssetItemType.Use && item.Time > 0 && item.Buffs.Count > 0 &&
                spec.Children.All(n => n.Name == "time" || recoveryFields.Contains(n.Name) || buffFields.ContainsKey(n.Name));
            items[id] = item;
            return item;
        }
        public bool ItemExists(int id) => GetItem(id) != null;
        public Dictionary<int, ItemInfo> GetAllItems() => new Dictionary<int, ItemInfo>(items);

        private WeaponProfile LoadWeapon(INxNode info, bool twoHanded)
        {
            int stand = Int(info, "stand"), walk = Int(info, "walk");
            var weapon = new WeaponProfile {
                AttackType = Int(info, "attack"), AttackSpeed = Int(info, "attackSpeed"), IsTwoHanded = twoHanded,
                Stand = (stand == 2 || (stand != 1 && twoHanded)) ? CharacterState.Stand2 : CharacterState.Stand,
                Walk = (walk == 2 || (walk != 1 && twoHanded)) ? CharacterState.Walk2 : CharacterState.Walk
            };
            if (weapon.AttackType == 9) ReadGunAction(weapon);
            foreach (var stance in WeaponProfile.AttackStances(weapon.AttackType).Where(s => s != CharacterState.Shot)
                .Concat(weapon.ActionStances ?? new CharacterState[0]).Concat(new[] { CharacterState.ProneStab }).Distinct())
            {
                var node = manager.GetNode("character", "00002000.img/" + CharacterStances.Name(stance));
                var delays = new List<int>();
                for (int frame = 0; node?[frame.ToString()] != null; frame++)
                    delays.Add(System.Math.Max(1, Int(node[frame.ToString()], "delay", 100)));
                if (delays.Count > 0) weapon.FrameDelays[stance] = delays.ToArray();
                var afterimage = NxWeaponAfterimages.Read(manager, info["afterImage"]?.GetValue<string>(),
                    Int(info, "reqLevel"), CharacterStances.Name(stance));
                if (afterimage != null) weapon.Afterimages[stance] = afterimage;
            }
            return weapon;
        }
        private void ReadGunAction(WeaponProfile weapon)
        {
            var node = manager.GetNode("character", "00002000.img/handgun");
            var stances = new List<CharacterState>(); var frames = new List<int>(); var delays = new List<int>();
            var moves = new List<MapleClient.GameLogic.Vector2>(); int elapsed = 0; bool foundHit = false;
            for (int i = 0; node?[i.ToString()] != null; i++)
            {
                var frame = node[i.ToString()]; string name = frame["action"]?.GetValue<string>();
                var matches = System.Enum.GetValues(typeof(CharacterState)).Cast<CharacterState>().Where(s => CharacterStances.Name(s) == name).ToArray();
                if (matches.Length == 0) return;
                int delay = Int(frame, "delay"); if (delay == 0) delay = 100;
                if (delay > 0 && !foundHit) { weapon.ActionHitDelay = elapsed; foundHit = true; }
                elapsed += System.Math.Abs(delay);
                stances.Add(matches[0]); frames.Add(Int(frame, "frame")); delays.Add(System.Math.Abs(delay));
                var move = frame["move"]?.GetValue<UnityEngine.Vector2>() ?? UnityEngine.Vector2.zero;
                moves.Add(new MapleClient.GameLogic.Vector2(move.x, move.y));
            }
            weapon.ActionStances = stances.ToArray(); weapon.ActionFrames = frames.ToArray(); weapon.ActionDelays = delays.ToArray(); weapon.ActionMoves = moves.ToArray();
        }
    }
}
