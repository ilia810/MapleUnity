// SkillData metadata and action lookup follow HeavenClient (AGPL-3.0-or-later).
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameData
{
    /// <summary>Lazy source skill metadata; missing IDs never acquire fabricated levels.</summary>
    public sealed class NxSkillDataProvider : ISkillDataProvider
    {
        private readonly NXDataManager manager;
        private readonly Dictionary<int, SkillInfo> cache = new Dictionary<int, SkillInfo>();
        public NxSkillDataProvider(NXDataManager manager) { this.manager = manager; }
        private static int Int(INxNode node, string key, int fallback = 0) => node?[key]?.GetValue<int>() ?? fallback;
        public static string Path(int id) { string key = id.ToString("D7"); return key.Substring(0, 3) + ".img/skill/" + key; }
        public SkillInfo GetSkill(int id)
        {
            if (id < 0 || id >= 100000000) return null;
            if (cache.TryGetValue(id, out var cached)) return cached;
            var node = manager.GetNode("skill", Path(id));
            if (node?["level"] == null) return null;
            var text = manager.GetNode("string", "Skill.img/" + id.ToString("D7"));
            bool passive = id % 10000 / 1000 == 0;
            var descriptor = ClassicSkillCatalog.Get(id);
            var targetVisual = descriptor?.PhysicalWeakening == true ? ReadEffectDefinition(Path(id) + "/mob") : null;
            var skill = new SkillInfo {
                Behavior = descriptor?.Behavior.Copy() ?? new SkillBehavior(),
                SkillId = id, JobId = id / 10000, IsSourceData = true, IsPassive = passive,
                Type = passive ? SkillType.Passive : ClassicSkillCatalog.IsAttackMetadata(id) ? SkillType.Attack : SkillType.Buff,
                IsInvisible = Int(node, "invisible") != 0,
                Name = text?["name"]?.GetValue<string>() ?? $"Skill {id}", Description = text?["desc"]?.GetValue<string>() ?? "",
                H1 = text?["h1"]?.GetValue<string>() ?? "", IconPath = Path(id) + "/icon", EffectPath = Path(id) + "/effect",
                RequiredWeaponType = (id / 10000 == 900 || id / 10000 == 910) ? 0 : SourceSkillRules.WeaponType(100 + Int(node, "weapon")),
                Action = node["action"]?["0"]?.GetValue<string>(), Levels = new Dictionary<int, SkillInfo.LevelData>(),
                AttackCount = 1, MobCount = 1, BulletCount = 1, BulletConsume = 1,
                Element = Element(node["elemAttr"]?.GetValue<string>())
            };
            foreach (var level in node["level"].Children)
            {
                if (!int.TryParse(level.Name, out int number) || number < 1) continue;
                var data = new SkillInfo.LevelData {
                    HpCost = Int(level, "hpCon"), MpCost = Int(level, "mpCon"), Damage = Int(level, "damage"), MagicAttack = Int(level, "mad"),
                    AttackCount = Int(level, "attackCount", 1), MobCount = Int(level, "mobCount", 1), Range = Int(level, "range", 100),
                    BulletCount = Int(level, "bulletCount", 1), BulletConsume = Int(level, "bulletConsume", Int(level, "bulletCount", 1)),
                    // Skill time/cooltime use seconds, unlike Item spec/time's milliseconds.
                    Duration = Milliseconds(Int(level, "time")), Cooldown = Milliseconds(Int(level, "cooltime")),
                    Mastery = Int(level, "mastery"), Hp = Int(level, "hp"), HpR = Int(level, "hpR"),
                    Mp = Int(level, "mp"), MpR = Int(level, "mpR"), Prop = Int(level, "prop", 100),
                    X = Int(level, "x"), Y = Int(level, "y"), Z = Int(level, "z"), Buffs = new Dictionary<BuffType, int>()
                };
                var lt = level["lt"]?.GetValue<UnityEngine.Vector2>(); var rb = level["rb"]?.GetValue<UnityEngine.Vector2>();
                if (lt.HasValue && rb.HasValue)
                    data.AttackBounds = new AttackBounds((int)lt.Value.x, (int)lt.Value.y, (int)rb.Value.x, (int)rb.Value.y);
                if (skill.Behavior.IsAttack)
                    data.Projectile = ReadEffectDefinition(Path(id) + (node["level"]?["1"]?["ball"] != null ? "/level/" + number : "") + "/ball");
                if (descriptor != null)
                {
                    foreach (var binding in descriptor.BuffFields)
                        if (level[binding.Key] != null) data.Buffs[binding.Value] = Int(level, binding.Key);
                    data.Passive = ClassicSkillCatalog.DecodePassive(id, data);
                    if (descriptor.PhysicalWeakening)
                        data.TargetDebuff = new MonsterDebuffDefinition {
                            PhysicalAttackChange = data.X, PhysicalDefenseChange = data.Y,
                            DurationMilliseconds = data.Duration, ChancePercent = data.Prop, RejectSameSource = true,
                            Visual = targetVisual };
                }
                skill.Levels[number] = data;
                skill.LevelDescriptions[number] = text?["h" + number]?.GetValue<string>() ?? "";
            }
            skill.MaxLevel = skill.Levels.Count; // SkillData uses level count, not the masterLevel book cap.
            if (node["req"] != null)
                foreach (var requirement in node["req"].Children)
                    if (int.TryParse(requirement.Name, out int required)) skill.RequiredSkills[required] = requirement.GetValue<int>();
            ReadAction(skill);
            ReadEffect(skill);
            cache[id] = skill; return skill;
        }
        internal void ReadAction(SkillInfo skill)
        {
            if (string.IsNullOrEmpty(skill.Action)) return;
            var node = manager.GetNode("character", "00002000.img/" + skill.Action);
            if (node == null) return;
            var stances = new List<CharacterState>(); var frames = new List<int>(); var delays = new List<int>();
            var markers = new List<int>(); var moves = new List<MapleClient.GameLogic.Vector2>(); int elapsed = 0;
            for (int i = 0; node[i.ToString()] != null; i++)
            {
                var frame = node[i.ToString()];
                string stanceName = frame["action"]?.GetValue<string>() ?? skill.Action;
                var matching = Enum.GetValues(typeof(CharacterState)).Cast<CharacterState>().Where(s => CharacterStances.Name(s) == stanceName).ToArray();
                if (matching.Length == 0) return;
                int delay = Int(frame, "delay"); if (delay == 0) delay = 100;
                if (delay > 0) markers.Add(elapsed);
                elapsed += Math.Abs(delay);
                stances.Add(matching[0]); frames.Add(Int(frame, "frame", i)); delays.Add(Math.Abs(delay));
                var move = frame["move"]?.GetValue<UnityEngine.Vector2>() ?? UnityEngine.Vector2.zero;
                moves.Add(new MapleClient.GameLogic.Vector2(move.x, move.y));
            }
            if (stances.Count == 0) return;
            skill.ActionStances = stances.ToArray(); skill.ActionFrames = frames.ToArray(); skill.ActionDelays = delays.ToArray();
            skill.ActionHitDelays = markers.ToArray(); skill.ActionMoves = moves.ToArray();
        }
        private void ReadEffect(SkillInfo skill)
        {
            if (!skill.Behavior.Available) return;
            var root = manager.GetNode("skill", Path(skill.SkillId));
            bool levelUse = root["CharLevel"]?["10"]?["effect"]?.Children.Any() == true;
            bool levelHit = root["CharLevel"]?["10"]?["hit"]?.Children.Any() == true;
            bool byLevel = levelUse || levelHit;
            var sources = byLevel ? root["CharLevel"].Children.Where(n => int.TryParse(n.Name, out _)) : new[] { root };
            foreach (var source in sources)
            {
                string path = Path(skill.SkillId) + (byLevel ? "/CharLevel/" + source.Name : "");
                string usePath = levelUse ? path : Path(skill.SkillId), hitPath = levelHit ? path : Path(skill.SkillId);
                skill.Effects[byLevel ? int.Parse(source.Name) : 0] = new SkillEffectSet {
                    Use = ReadEffectDefinition(usePath + "/effect"), Hit = ReadEffectDefinition(hitPath + "/hit/0"),
                    TwoHandedHit = ReadEffectDefinition(hitPath + "/hit/1"),
                    HasTwoHandedHit = root["hit"]?["0"] != null && root["hit"]?["1"] != null
                };
            }
            skill.EffectFrames = skill.Effects.Values.FirstOrDefault()?.Use?.Frames;
        }
        private SkillEffectDefinition ReadEffectDefinition(string path) => NxEffectFrames.Read(manager, "skill", path);
        private static int Milliseconds(int seconds) => (int)Math.Max(0, Math.Min(int.MaxValue, (long)seconds * 1000));
        private static ElementType Element(string value)
        {
            switch (value) { case "I": return ElementType.Ice; case "F": return ElementType.Fire; case "L": return ElementType.Lightning;
                case "S": return ElementType.Poison; case "H": return ElementType.Holy; case "D": return ElementType.Dark;
                case "P": return ElementType.Physical; default: return ElementType.Neutral; }
        }
        public bool SkillExists(int id) => GetSkill(id) != null;
        public Dictionary<int, SkillInfo> GetSkillsForJob(int job)
        {
            var result = new Dictionary<int, SkillInfo>();
            var node = manager.GetNode("skill", job.ToString("D3") + ".img/skill");
            if (node != null)
                foreach (var child in node.Children)
                    if (int.TryParse(child.Name, out int id)) { var skill = GetSkill(id); if (skill != null) result[id] = skill; }
            return result;
        }
    }
}
