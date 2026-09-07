// ActiveBuffs / Player::give_buff stat replacement follows HeavenClient.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public sealed class ActiveStatBuff
    {
        public int SourceId { get; internal set; }
        public string Name { get; internal set; }
        public BuffType Type { get; internal set; }
        public int Value { get; internal set; }
        public int RemainingMilliseconds { get; internal set; }
    }

    public partial class Player
    {
        private readonly Dictionary<BuffType, ActiveStatBuff> statBuffs = new Dictionary<BuffType, ActiveStatBuff>();
        public IEnumerable<ActiveStatBuff> ActiveBuffs => statBuffs.Values;
        public event Action BuffsChanged;
        public int ArmorEchoMilliseconds { get; private set; }
        public bool IsHidden => statBuffs.TryGetValue(BuffType.Hide, out var concealment) && concealment.Value != 0;
        public bool IgnoresContact => IsInvulnerable || IsHidden;
        public float ConcealmentOpacity => IsHidden ? .35f : 1f;
        public int AttackSpeedModifier => statBuffs.TryGetValue(BuffType.Booster, out var buff) ? buff.Value : 0;
        public int EffectiveAttackSpeed => CurrentWeapon == null ? 0 : CurrentWeapon.AttackSpeed + AttackSpeedModifier;

        public static bool SupportsStatBuff(BuffType type) => type == BuffType.WeaponAttack || type == BuffType.MagicAttack ||
            type == BuffType.WeaponDefense || type == BuffType.MagicDefense || type == BuffType.Speed ||
            type == BuffType.Jump || type == BuffType.Booster || type == BuffType.MaxHPPercent ||
            type == BuffType.MaxMPPercent || type == BuffType.MagicGuard || type == BuffType.Accuracy ||
            type == BuffType.Avoidability || type == BuffType.Hide;

        public static bool ValidStatBuffs(IDictionary<BuffType, int> values, int milliseconds)
        {
            if (milliseconds <= 0 || values == null || values.Count == 0 || values.Keys.Any(t => !SupportsStatBuff(t))) return false;
            if (values.TryGetValue(BuffType.Hide, out int hidden) && hidden != 1) return false;
            if (values.Any(v => (v.Key == BuffType.MaxHPPercent || v.Key == BuffType.MaxMPPercent || v.Key == BuffType.MagicGuard) &&
                (v.Value < 0 || v.Value > 100))) return false;
            return true;
        }

        public bool ApplyStatBuffs(int sourceId, string name, IDictionary<BuffType, int> values, int milliseconds)
        {
            if (IsDead || !ValidStatBuffs(values, milliseconds)) return false;
            // The source owns one value per buff stat; recasts replace it, never add to base stats.
            foreach (var entry in values)
                statBuffs[entry.Key] = new ActiveStatBuff { SourceId = sourceId, Name = name, Type = entry.Key,
                    Value = entry.Value, RemainingMilliseconds = milliseconds };
            BuffStatsChanged(); return true;
        }

        public void RemoveStatBuffs(int sourceId)
        {
            var keys = statBuffs.Where(b => b.Value.SourceId == sourceId).Select(b => b.Key).ToArray();
            foreach (var key in keys) statBuffs.Remove(key);
            if (keys.Length > 0) BuffStatsChanged();
        }

        private void BuffStatsChanged()
        {
            // Local authority: losing/replacing maximum-resource buffs clamps excess
            // current resources; gaining a larger maximum never grants a free heal.
            hp = Math.Min(hp, maxHp); mp = Math.Min(mp, maxMp);
            OnStatsChanged(); BuffsChanged?.Invoke();
        }

        private void ClearStatBuffs()
        {
            ArmorEchoMilliseconds = 0;
            if (statBuffs.Count == 0) return;
            statBuffs.Clear(); BuffStatsChanged();
        }

        private int WithPercentBuff(int total, BuffType type)
        {
            // CharStats::close_totalstats: base plus equipment, then truncated
            // percentage contribution, then the source HP/MP cap.
            return statBuffs.TryGetValue(type, out var buff) ? Math.Min(30000, total + (int)(total * (buff.Value / 100f))) : total;
        }

        private int GuardContactDamage(int damage)
        {
            if (!statBuffs.TryGetValue(BuffType.MagicGuard, out var guard)) return damage;
            // The C++ client receives HP/MP from its server. Our explicit local rule
            // uses NX x percent after defense, rounds down and spills any MP shortage
            // back into HP. Skill costs and direct scripted damage do not enter here.
            int absorbed = (int)Math.Min(mp, (long)damage * guard.Value / 100);
            mp -= absorbed;
            return damage - absorbed;
        }

        private void TickStatBuffs()
        {
            ArmorEchoMilliseconds = Math.Max(0, ArmorEchoMilliseconds - 8);
            if (statBuffs.Count == 0) return;
            List<BuffType> expired = null;
            foreach (var entry in statBuffs)
            {
                entry.Value.RemainingMilliseconds = Math.Max(0, entry.Value.RemainingMilliseconds - 8);
                if (entry.Value.RemainingMilliseconds == 0) (expired ?? (expired = new List<BuffType>())).Add(entry.Key);
            }
            if (expired == null) return;
            foreach (var key in expired) statBuffs.Remove(key);
            BuffStatsChanged();
        }

        private int WithBuff(int value, BuffType type, int cap)
        {
            // Preserve existing direct stat overrides used by movement fixtures/practice.
            // A source active buff applies set_total's cap without changing that base value.
            return statBuffs.TryGetValue(type, out var buff) ? (int)Math.Min(cap, (long)value + buff.Value) : value;
        }
    }
}
