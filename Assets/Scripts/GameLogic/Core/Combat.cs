// Contact bounds/damage ported from HeavenClient (continued Journey client).
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameLogic.Core
{
    public class Combat
    {
        private sealed class Swing
        {
            public Player Player;
            public BasicAttackMotion Motion;
            public List<TargetHit> Targets;
            public List<Monster> Monsters;
            public SkillEffectDefinition HitEffect;
            public MonsterDebuffDefinition Debuff;
            public int SkillId;
            public string SkillName;
            public bool DealsDamage;
            public readonly HashSet<Monster> DebuffAttempts = new HashSet<Monster>();
            public SkillEffectDefinition Projectile;
            public int OriginX, OriginY, ContextVersion;
            public readonly HashSet<int> Applied = new HashSet<int>();
        }
        private struct TargetHit { public Monster Target; public AttackHit Hit; public bool Landed; public int HeadX, HeadY; }
        private sealed class Flight { public Swing Swing; public TargetHit Target; public SkillProjectile Bullet; }
        private readonly List<Swing> swings = new List<Swing>();
        private readonly List<Flight> flights = new List<Flight>();
        private double accumulator;
        public IEnumerable<SkillProjectile> Projectiles => flights.Select(f => f.Bullet);
        public void Clear() { foreach (var swing in swings) swing.Motion.Cancel(); swings.Clear(); flights.Clear(); accumulator = 0; }
        private readonly Random attackRandom;
        private readonly Random damageRandom;
        private readonly bool consumeAmmunition;
        private readonly Random effectRandom;
        public Combat(Random attackRandom = null, Random damageRandom = null, bool consumeAmmunition = true, Random contactRandom = null, Random effectRandom = null)
        { this.attackRandom = attackRandom ?? new Random(); this.damageRandom = damageRandom ?? new Random(); this.consumeAmmunition = consumeAmmunition;
            this.contactRandom = contactRandom ?? new Random(); this.effectRandom = effectRandom ?? new Random(); }

        public event Action<Player, Monster, int> DamageDealt;
        public event Action<Player, Monster, AttackHit> AttackResolved;
        public event Action<Player, Monster> MonsterDefeated;
        private readonly Random contactRandom;

        // Stage::check_collisions / MapMobs::find_colliding: sweep the player's feet
        // between ticks, extending 50 pixels upward. Mob bounds stay in source orientation.
        public Monster CheckContact(Player player, IEnumerable<Monster> monsters)
        {
            if (player.IgnoresContact) return null;
            int x0 = (int)(player.PreviousPosition.X * 100), x1 = (int)(player.Position.X * 100);
            int y0 = (int)(-(player.PreviousPosition.Y - Player.Height / 2) * 100);
            int y1 = (int)(-(player.Position.Y - Player.Height / 2) * 100);
            foreach (var monster in monsters)
            {
                if (monster.IsDead || !monster.Template.BodyAttack || monster.Template.ContactAnimations == null) continue;
                var b = monster.ContactBounds;
                int x = (int)(monster.Position.X * 100), y = (int)(-monster.Position.Y * 100);
                if (Math.Max(x0, x1) < x + b.Left || Math.Min(x0, x1) > x + b.Right ||
                    Math.Max(y0, y1) < y + b.Top || Math.Min(y0, y1) - 50 > y + b.Bottom) continue;
                // Local policy: use the existing hit rule for contact evasion too.
                // Missing monster accuracy retains the existing always-hit fallback.
                float hitChance = ContactHitChance(player, monster.Template);
                if (hitChance < 1 && contactRandom.NextDouble() >= hitChance)
                {
                    player.ReceiveContactDamage(0, monster.Position.X > player.Position.X);
                    return monster;
                }
                int attack = monster.PhysicalAttack;
                int rolled = (int)(attack * .8f) + (int)(contactRandom.NextDouble() * (attack - (int)(attack * .8f) + 1L));
                int defense = Math.Max(0, player.WeaponDefense);
                // CharStats::calculate_damage bypasses passive reduction when defense is zero.
                int reduced = defense == 0 ? rolled : rolled / 2 + rolled / defense;
                int damage = defense == 0 ? rolled : reduced - (int)(reduced * player.PassiveDamageReduction);
                player.ReceiveContactDamage(damage, monster.Position.X > player.Position.X);
                return monster;
            }
            return null;
        }

        public static float ContactHitChance(Player player, MonsterTemplate monster) =>
            player.Avoidability <= 0 || monster.Accuracy <= 0 ? 1f : Math.Min(1f,
                AccuracyRules.HitChance(monster.Accuracy, monster.Level, player.Avoidability, player.Level));

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            accumulator += deltaTime;
            while (accumulator + 1e-8 >= .008) { accumulator = Math.Max(0, accumulator - .008); Tick(); }
        }
        private void Tick()
        {
            for (int index = swings.Count - 1; index >= 0; index--)
            {
                var swing = swings[index];
                if (swing.Player.IsDead || swing.Player.State == PlayerState.Climbing || !ReferenceEquals(swing.Player.BasicAttack, swing.Motion))
                    swing.Motion.Cancel();
                swing.Motion.Advance(.008f);
                ApplyHit(swing);
                if (swing.Motion.IsComplete) swings.RemoveAt(index);
            }
            for (int index = 0; index < flights.Count; index++)
            {
                var flight = flights[index]; var swing = flight.Swing;
                if (swing.Player.IsDead || swing.Player.CombatContextVersion != swing.ContextVersion) { flights.RemoveAt(index--); continue; }
                var target = flight.Target;
                bool present = target.Target != null && swing.Monsters.Contains(target.Target);
                if (present) { HeadPosition(target.Target, out target.HeadX, out target.HeadY); flight.Target = target; }
                flight.Bullet.Tick(target.HeadX);
                if (!flight.Bullet.HasArrived) continue;
                ResolveHit(swing, target);
                flights.RemoveAt(index--);
            }
        }

        public bool CanPlayerAttack(Player player, bool requireAmmunition = true)
        {
            return player != null && !player.IsDead && !player.IsHidden && player.State != PlayerState.Climbing && !player.IsBasicAttacking &&
                (!requireAmmunition || !player.UsesAmmunition || player.HasUsableAmmunition);
        }

        public List<Monster> PerformBasicAttack(Player player, List<Monster> monsters, float range)
            => PerformAttack(player, monsters, range, null, null);

        public bool PerformSkillAttack(Player player, List<Monster> monsters, SkillInfo skill, SkillInfo.LevelData level)
        {
            var behavior = skill?.Behavior;
            if (player == null || behavior?.IsAttack != true || level == null || !behavior.WeaponMatches(player.EquippedWeaponType) ||
                !CanPlayerAttack(player, behavior.UsesAmmunition)) return false;
            if (behavior.BlocksCrouching && player.State == PlayerState.Crouching) return false;
            if (behavior.UsesAmmunition && (!player.HasUsableAmmunition || player.AmmunitionCount < level.BulletConsume)) return false;
            if (!string.IsNullOrEmpty(skill.Action) && skill.ActionDelays == null) return false;
            PerformAttack(player, monsters, 1, skill, level);
            return player.IsBasicAttacking;
        }

        private List<Monster> PerformAttack(Player player, List<Monster> monsters, float range, SkillInfo skill, SkillInfo.LevelData level)
        {
            var hitMonsters = new List<Monster>();

            if (!CanPlayerAttack(player, skill?.Behavior.UsesAmmunition ?? true))
                return hitMonsters;

            var ammo = (skill?.Behavior.UsesAmmunition ?? player.UsesAmmunition) ? player.SelectedAmmunition : null;
            bool namedAction = skill != null && !string.IsNullOrEmpty(skill.Action);
            if (!(namedAction ? player.PlaySkillAnimation(skill, true) : player.PlayBasicAttackAnimation(attackRandom.Next()))) return hitMonsters;
            var motion = player.BasicAttack;
            bool dealsDamage = skill?.Behavior.DamagePolicy != SkillDamagePolicy.None;
            // Combat::apply_move chooses targets at swing start. The later damage
            // event retains that object and direction rather than seeking a new target.
            float reach = level == null ? 1 : level.Range / 100f;
            bool magic = skill?.Behavior.AttackFamily == SkillAttackFamily.Magic;
            // SkillBullet selects an authored ball before falling back to ammunition art.
            var projectileArt = level?.Projectile?.Frames.Length > 0 ? level.Projectile : ammo?.Ammunition;
            int hitCount = level == null ? 1 : ammo != null ? level.BulletCount : level.AttackCount;
            bool projectile = projectileArt?.Frames.Length > 0;
            // The supported magic spells have no authored action: RegularAction selects
            // the equipped weapon's ordinary stance and afterimage delay for every line.
            // Wands/staves cast from the source 400px range; other melee gear retains
            // its close range for a spell without a projectile, just as Skill::apply_stats.
            bool magicType = (player.EquippedWeaponType == 137 || player.EquippedWeaponType == 138) && player.State != PlayerState.Crouching;
            int projectileRange = ammo != null && !magic ? player.ProjectileRangePixels : 400;
            AttackBounds? spellBounds = level?.AttackBounds ?? ((magic && (projectile || magicType)) ||
                (ammo != null && player.State != PlayerState.Crouching) ? new AttackBounds(-projectileRange, -50, -5, 50) : (AttackBounds?)null);
            var targets = monsters
                .Where(m => !m.IsDead)
                .Where(m => dealsDamage || m.CanApplyDebuff(skill.SkillId, level.TargetDebuff))
                .Where(m => spellBounds.HasValue ? IsInBounds(player, m, spellBounds.Value, motion.FacingRight, reach) :
                    player.CurrentWeapon == null ? IsInAttackRange(player, m, range) : IsInWeaponRange(player, m, motion, reach))
                // RegularAttack::apply_stats sets mobcount=1; Combat picks the closest.
                .OrderBy(m => SourceDistance(player, m))
                .Take(level == null ? 1 : level.MobCount).ToList();
            var selected = new List<TargetHit>();
            var stats = skill == null ? player.PhysicalAttackStats : SkillRules.AttackStats(player, skill, level);
            foreach (var target in targets)
                for (int line = 0; line < hitCount; line++)
                {
                    var hit = !dealsDamage ? new AttackHit(0, false) : level == null ? CalculatePhysicalHit(player, target) :
                        stats.Roll(target.Template, damageRandom, target.PhysicalDefense);
                    bool landed = dealsDamage ? !hit.Miss : damageRandom.NextDouble() < stats.HitChance(target.Template);
                    HeadPosition(target, out int headX, out int headY);
                    selected.Add(new TargetHit { Target = target, Hit = new AttackHit(hit.Damage, hit.Critical, line), Landed = landed, HeadX = headX, HeadY = headY });
                }
            var effects = skill == null ? null : SourceSkillRules.EffectsFor(skill, player.Level);
            int originX = SourcePixel(player.Position.X), originY = SourcePixel(-(player.Position.Y - Player.Height / 2));
            if (projectile && selected.Count == 0)
                for (int line = 0; line < hitCount; line++)
                    selected.Add(new TargetHit { Hit = new AttackHit(0, false, line), HeadX = originX + (motion.FacingRight ? projectileRange : -projectileRange), HeadY = originY - 26 });
            var swing = new Swing { Player = player, Motion = motion, Monsters = monsters, Targets = selected,
                DealsDamage = dealsDamage, Debuff = level?.TargetDebuff?.Copy(), SkillId = skill?.SkillId ?? 0, SkillName = skill?.Name,
                Projectile = projectile ? projectileArt : null, OriginX = originX, OriginY = originY, ContextVersion = player.CombatContextVersion,
                HitEffect = effects?.HasTwoHandedHit == true && player.CurrentWeapon?.IsTwoHanded == true ? effects.TwoHandedHit : effects?.Hit };
            swings.Add(swing);
            hitMonsters.AddRange(targets);
            if (skill != null) player.ShowSkillUse(skill);
            // Offline policy: pay one shot after successful preparation, including a
            // miss/empty cast. Capture damage first so the final round retains its bonus.
            // Connected play leaves counts to server inventory updates.
            if (ammo != null && consumeAmmunition) player.Inventory.RemoveItem(ammo.ItemId, level?.BulletConsume ?? 1);
            // TimedQueue dispatches a zero-marker skill on the next 8 ms tick.
            // Retain the local unarmed practice attack's immediate impact.
            if (skill == null) ApplyHit(swing);
            // Return selected targets; DamageDealt/MonsterDefeated report the eventual impact.
            return hitMonsters;
        }

        private void ApplyHit(Swing swing)
        {
            if (swing.Motion.IsCancelled || swing.Player.IsDead || !ReferenceEquals(swing.Player.BasicAttack, swing.Motion)) return;
            for (int index = 0; index < swing.Targets.Count; index++)
            {
                var target = swing.Targets[index];
                if (swing.Applied.Contains(index) || swing.Motion.ElapsedMilliseconds < swing.Motion.HitDelayForLine(target.Hit.LineIndex)) continue;
                swing.Applied.Add(index);
                if (swing.Projectile != null)
                {
                    var bullet = new SkillProjectile(swing.OriginX, swing.OriginY, swing.Motion.FacingRight, target.HeadX, target.HeadY, swing.Projectile);
                    if (bullet.HasArrived) ResolveHit(swing, target);
                    else flights.Add(new Flight { Swing = swing, Target = target, Bullet = bullet });
                }
                else ResolveHit(swing, target);
            }
        }
        private void ResolveHit(Swing swing, TargetHit target)
        {
            var monster = target.Target;
            if (monster == null || monster.IsDead || !swing.Monsters.Contains(monster)) return;
            if (swing.DealsDamage) monster.TakeDamage(target.Hit.Damage, !swing.Motion.FacingRight);
            if (target.Landed && swing.Debuff != null && !monster.IsDead && swing.DebuffAttempts.Add(monster) &&
                monster.CanApplyDebuff(swing.SkillId, swing.Debuff) &&
                (swing.Debuff.ChancePercent == 100 || effectRandom.NextDouble() < swing.Debuff.ChancePercent / 100.0))
                monster.TryApplyDebuff(swing.SkillId, swing.SkillName, swing.Debuff);
            // A successful status-only strike has no damage number, knockback or kill event.
            if (!swing.DealsDamage)
            {
                if (!target.Landed) AttackResolved?.Invoke(swing.Player, monster, target.Hit);
                return;
            }
            if (swing.HitEffect?.Frames.Length > 0)
            {
                var head = monster.ContactBounds;
                monster.SkillHitEffect = new SkillUseVisual(swing.HitEffect.Frames, 1, swing.Motion.FacingRight, swing.HitEffect.AssetFile) {
                    Z = swing.HitEffect.Z,
                    Offset = swing.HitEffect.Position == 0 ? new Vector2((monster.FacingRight && !monster.Template.NoFlip ? -head.HeadX : head.HeadX) / 100f, -head.HeadY / 100f) : Vector2.Zero
                };
            }
            DamageDealt?.Invoke(swing.Player, monster, target.Hit.Damage);
            AttackResolved?.Invoke(swing.Player, monster, target.Hit);
            if (monster.IsDead) MonsterDefeated?.Invoke(swing.Player, monster);
        }

        private static void HeadPosition(Monster monster, out int x, out int y)
        {
            var head = monster.ContactBounds;
            x = SourcePixel(monster.Position.X) + (monster.FacingRight && !monster.Template.NoFlip ? -head.HeadX : head.HeadX);
            y = SourcePixel(-monster.Position.Y) + head.HeadY;
        }

        private static int SourceDistance(Player player, Monster monster)
        {
            double x = SourcePixel(player.Position.X) - SourcePixel(monster.Position.X);
            double y = SourcePixel(-(player.Position.Y - Player.Height / 2)) - SourcePixel(-monster.Position.Y);
            return (int)Math.Sqrt(x * x + y * y);
        }

        // Match Player's source-coordinate normalization before integer truncation:
        // an exact 95-pixel position stored as .95f must not become 94 pixels.
        private static int SourcePixel(float worldPosition) => (int)Math.Round(worldPosition * 100.0, 5);

        private static bool IsInWeaponRange(Player player, Monster monster, BasicAttackMotion motion, float horizontalRange = 1)
        {
            if (motion.Afterimage?.HasBounds != true || monster.Template.ContactAnimations == null || monster.Template.ContactAnimations.Count == 0) return false;
            return IsInBounds(player, monster, motion.Afterimage.Bounds, motion.FacingRight, horizontalRange);
        }
        private static bool IsInBounds(Player player, Monster monster, AttackBounds boundsAtStart, bool facingRight, float horizontalRange)
        {
            if (monster.Template.ContactAnimations == null || monster.Template.ContactAnimations.Count == 0) return false;
            boundsAtStart = boundsAtStart.ScaleForward(horizontalRange);
            var range = boundsAtStart.At(SourcePixel(player.Position.X),
                SourcePixel(-(player.Position.Y - Player.Height / 2)), facingRight);
            var bounds = monster.ContactBounds;
            int x = SourcePixel(monster.Position.X), y = SourcePixel(-monster.Position.Y);
            // Mob::is_in_range does not mirror its animation bounds with its facing.
            return range.Overlaps(new AttackBounds(x + bounds.Left, y + bounds.Top, x + bounds.Right, y + bounds.Bottom));
        }

        public int CalculatePhysicalDamage(Player player, Monster monster) => CalculatePhysicalHit(player, monster).Damage;

        public AttackHit CalculatePhysicalHit(Player player, Monster monster)
        {
            if (player.CurrentWeapon != null && !player.HasPracticeDamageOverride)
                return player.PhysicalAttackStats.Roll(monster.Template, damageRandom, monster.PhysicalDefense);
            // Keep explicit practice overrides and the legacy unarmed attack separate
            // from equipped source combat. These do not roll misses or critical hits.
            float variance = .8f + (float)(damageRandom.NextDouble() * .4f);
            int damage = Math.Max(1, (int)((player.GetBaseDamage() - (monster.Template?.PhysicalDefense ?? 0)) * variance));
            return new AttackHit(damage, false);
        }

        private bool IsInAttackRange(Player player, Monster monster, float range)
        {
            float deltaX = monster.Position.X - player.Position.X;
            bool facingRight = player.FacingRight ?? true;
            if (facingRight ? deltaX < 0 : deltaX > 0) return false;
            var distance = Math.Abs(deltaX);
            
            // Also check Y distance (must be on similar height)
            var yDistance = Math.Abs(player.Position.Y - Player.Height / 2 - monster.Position.Y);
            
            return distance <= range && yDistance <= 0.5f; // 50 pixels in world units
        }
    }
}
