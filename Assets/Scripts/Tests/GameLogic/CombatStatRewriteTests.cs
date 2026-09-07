using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using ItemType = MapleClient.GameLogic.Interfaces.ItemType;
using SkillType = MapleClient.GameLogic.Interfaces.SkillType;

namespace MapleClient.Tests.GameLogic
{
    public class CombatStatRewriteTests
    {
        public static readonly int[] Families = { 0, 130, 131, 132, 133, 137, 138, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 170 };
        private sealed class Rolls : Random
        {
            private readonly double[] values;
            public int Used { get; private set; }
            public Rolls(params double[] values) { this.values = values; }
            public override double NextDouble() => values[Used++];
            public override int Next() => 0;
        }
        private sealed class Items : IItemDataProvider
        {
            public readonly Dictionary<int, ItemInfo> Values = new Dictionary<int, ItemInfo>();
            public ItemInfo GetItem(int id) => Values.TryGetValue(id, out var item) ? item : null;
            public bool ItemExists(int id) => Values.ContainsKey(id);
            public Dictionary<int, ItemInfo> GetAllItems() => Values;
        }
        private static MapData Map()
        {
            var map = new MapData(); map.Platforms.Add(new Platform { Id = 1, X1 = -1000, X2 = 1000, Y1 = 0, Y2 = 0 }); return map;
        }
        private static Player Equipped(out Items items)
        {
            items = new Items();
            var weapon = new WeaponProfile { AttackType = 1, AttackSpeed = 4 };
            weapon.FrameDelays[CharacterState.Attack1] = new[] { 350, 450 };
            weapon.FrameDelays[CharacterState.ProneStab] = new[] { 300, 400 };
            var trail = new WeaponAfterimage { FirstFrame = 1, HasBounds = true, Bounds = new AttackBounds(-84, -30, -20, -15) };
            weapon.Afterimages[CharacterState.Attack1] = trail; weapon.Afterimages[CharacterState.ProneStab] = trail;
            items.Values[1302000] = new ItemInfo { ItemId = 1302000, Name = "Sword", Type = ItemType.Equip, Gender = 2,
                EquipmentSlot = EquipSlot.Weapon, Weapon = weapon, Stats = new Dictionary<StatType, int> { [StatType.WeaponAttack] = 17 } };
            var player = new Player { Position = new Vector2(0, .3f), IsGrounded = true };
            player.SetItemData(items); player.Inventory.AddItem(1302000, 1); Assert.That(player.TryEquipItem(1302000, out _), Is.True);
            return player;
        }
        private static Monster Target() => new Monster(new MonsterTemplate { Level = 1, MaxHP = 1000,
            ContactAnimations = new Dictionary<string, MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -10, Top = -20, Right = 10, Bottom = 0, DelayMilliseconds = 100 }
            }, false) } }, new Vector2(.5f, 0));

        [TestCaseSource(nameof(Families))]
        public void StatsAndRollsMatchCompiledCppAcrossJobsDefenseLevelsAndCaps(int family)
        {
            var rows = File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenCombatStats.csv"))
                .Where(line => line.StartsWith(family + ",", StringComparison.Ordinal))
                .Select(line => line.Split(',').Select(n => double.Parse(n, CultureInfo.InvariantCulture)).ToArray()).ToArray();
            Assert.That(rows.Length, Is.EqualTo(56));
            foreach (var r in rows)
            {
                var stats = PhysicalAttackStats.Calculate((int)r[0] * 10000, (int)r[1], (int)r[2], (int)r[3], (int)r[4], (int)r[5],
                    (int)r[6], (int)r[7], (int)r[8], (float)r[9], (float)r[10], prone: r[11] == 1);
                var target = new MonsterTemplate { Level = (int)r[12], Avoidability = (int)r[13], PhysicalDefense = (int)r[14] };
                var random = new Rolls(r[15], r[16], r[17]);
                Assert.That(stats.Minimum, Is.EqualTo(r[18]).Within(.000001));
                Assert.That(stats.Maximum, Is.EqualTo(r[19]).Within(.000001));
                Assert.That(stats.Accuracy, Is.EqualTo((int)r[20]));
                Assert.That(stats.HitChance(target), Is.EqualTo(r[21]).Within(.000001));
                Assert.That(stats.MinimumAgainst(target), Is.EqualTo(r[22]).Within(.000001));
                Assert.That(stats.MaximumAgainst(target), Is.EqualTo(r[23]).Within(.000001));
                var hit = stats.Roll(target, random);
                Assert.That(hit.Damage, Is.EqualTo((int)r[24]), "Damage for job " + r[1]);
                Assert.That(hit.Critical, Is.EqualTo(r[25] == 1)); Assert.That(random.Used, Is.EqualTo((int)r[26]));
            }
        }

        [Test]
        public void GuaranteedHitAndCriticalRemainGuaranteedWhenFloatConversionRoundsUp()
        {
            var stats = new PhysicalAttackStats(1, 2, 1, 1, 1);
            var hit = stats.Roll(new MonsterTemplate { Level = 1 }, new Rolls(.999999999, .5, .999999999));
            Assert.That(hit.Miss, Is.False); Assert.That(hit.Critical, Is.True); Assert.That(hit.Damage, Is.EqualTo(2));
        }

        [Test]
        public void EquippedDamageUsesStatsAndExplicitPracticeOverrideCanBeCleared()
        {
            var player = Equipped(out _);
            Assert.That(player.PhysicalAttackStats.Minimum, Is.EqualTo(2));
            Assert.That(player.PhysicalAttackStats.Maximum, Is.EqualTo(12));
            Assert.That(player.PhysicalAttackStats.Accuracy, Is.EqualTo(19));
            Assert.That(player.Accuracy, Is.EqualTo(player.PhysicalAttackStats.Accuracy));
            player.STR = 30;
            Assert.That(player.PhysicalAttackStats.Maximum, Is.EqualTo(22));
            var monster = Target(); player.CriticalChance = 0;
            player.SetBaseDamage(1 - player.EquipmentBonus(StatType.WeaponAttack));
            Assert.That(new Combat(damageRandom: new Rolls(.5)).CalculatePhysicalDamage(player, monster), Is.EqualTo(1));
            player.ClearPracticeDamageOverride();
            Assert.That(new Combat(damageRandom: new Rolls(0, .5, .5)).CalculatePhysicalDamage(player, monster), Is.GreaterThan(1));
        }

        [TestCase(false)] [TestCase(true)]
        public void HitOrMissIsCapturedAtSwingStartAndPublishedOnceAtDelayedImpact(bool miss)
        {
            var player = Equipped(out _); var monster = Target();
            if (miss) monster.Template.Avoidability = 100000;
            var combat = new Combat(new Rolls(), miss ? new Rolls(.9) : new Rolls(0, .5, 0));
            int reports = 0; AttackHit result = default;
            combat.AttackResolved += (_, __, hit) => { reports++; result = hit; };
            var expected = player.PhysicalAttackStats.Roll(monster.Template, miss ? new Rolls(.9) : new Rolls(0, .5, 0));
            combat.PerformBasicAttack(player, new List<Monster> { monster }, 1);
            // Neither a later stat buff nor a new target defense changes an already rolled hit.
            player.ApplyStatBuffs(1, "Attack", new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 100 }, 1000);
            monster.Template.PhysicalDefense = 999; monster.Template.Avoidability = 0; player.CriticalChance = 0;
            combat.Update(.264f); Assert.That(reports, Is.Zero); Assert.That(monster.HP, Is.EqualTo(1000));
            combat.Update(.008f); Assert.That(reports, Is.EqualTo(1));
            Assert.That(result.Damage, Is.EqualTo(expected.Damage)); Assert.That(result.Critical, Is.EqualTo(!miss));
            Assert.That(monster.HP, Is.EqualTo(1000 - expected.Damage)); Assert.That(result.Miss, Is.EqualTo(miss));
            if (miss) Assert.That(monster.IsHit, Is.False);
            combat.Update(1); Assert.That(reports, Is.EqualTo(1));
        }

        [Test]
        public void BuffRefreshReplacementAndOldSourceRemovalDoNotAccumulateOrRemoveNewBonuses()
        {
            var player = Equipped(out _);
            var first = new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 10 };
            player.ApplyStatBuffs(1, "First", first, 32); player.ApplyStatBuffs(1, "First", first, 32);
            Assert.That(player.WeaponAttack, Is.EqualTo(27)); Assert.That(player.ActiveBuffs.Count(), Is.EqualTo(1));
            player.ApplyStatBuffs(2, "Second", new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 5, [BuffType.Speed] = 8 }, 64);
            player.RemoveStatBuffs(1); Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.Speed, Is.EqualTo(108));
            player.UpdatePhysics(.056f, Map()); Assert.That(player.WeaponAttack, Is.EqualTo(22));
            player.UpdatePhysics(.008f, Map()); Assert.That(player.WeaponAttack, Is.EqualTo(17));
            Assert.That(player.Speed, Is.EqualTo(100)); Assert.That(player.ActiveBuffs, Is.Empty);
        }

        [Test]
        public void BuffsStaySeparateFromEquipmentAndBaseStatEdits()
        {
            var player = Equipped(out _);
            player.ApplyStatBuffs(1, "Attack", new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 5 }, 32);
            player.WeaponAttack = 30; Assert.That(player.WeaponAttack, Is.EqualTo(52));
            player.TryUnequipItem(EquipSlot.Weapon, out _); Assert.That(player.WeaponAttack, Is.EqualTo(35));
            player.TryEquipItem(1302000, out _); Assert.That(player.WeaponAttack, Is.EqualTo(52));
            player.UpdatePhysics(.032f, Map()); Assert.That(player.WeaponAttack, Is.EqualTo(47));
            player.TryUnequipItem(EquipSlot.Weapon, out _); Assert.That(player.WeaponAttack, Is.EqualTo(30));
        }

        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void BuffExpirationUsesSimulationTicksAndSurvivesMapMovementReset(float interval)
        {
            var player = Equipped(out _); player.Speed = 139; player.JumpPower = 120;
            player.ApplyStatBuffs(-2002001, "Speed", new Dictionary<BuffType, int> { [BuffType.Speed] = 8, [BuffType.Jump] = 5 }, 64);
            Assert.That(player.Speed, Is.EqualTo(140)); Assert.That(player.JumpPower, Is.EqualTo(123));
            player.ResetMovementForMap(); Assert.That(player.ActiveBuffs.Count(), Is.EqualTo(2));
            for (int i = 0; i < (int)Math.Round(.048 / interval); i++) player.UpdatePhysics(interval, Map());
            Assert.That(player.ActiveBuffs.First().RemainingMilliseconds, Is.EqualTo(16));
            for (int i = 0; i < (int)Math.Round(.016 / interval); i++) player.UpdatePhysics(interval, Map());
            Assert.That(player.Speed, Is.EqualTo(139)); Assert.That(player.JumpPower, Is.EqualTo(120));
        }

        [Test]
        public void BoosterExpiresDuringSwingChangingAnimationSpeedButNotQueuedHitDelay()
        {
            var player = Equipped(out _); var map = Map(); player.UpdatePhysics(.008f, map);
            player.ApplyStatBuffs(1101004, "Sword Booster", new Dictionary<BuffType, int> { [BuffType.Booster] = -2 }, 168);
            Assert.That(player.PlayBasicAttackAnimation(), Is.True); var swing = player.BasicAttack;
            Assert.That(swing.HitDelayMilliseconds, Is.EqualTo(233)); // 350 / 1.5
            for (int i = 0; i < 20; i++) { player.UpdatePhysics(.008f, map); swing.Advance(.008f); }
            Assert.That(player.EffectiveAttackSpeed, Is.EqualTo(2)); // body elapsed 20 * 12 = 240
            player.UpdatePhysics(.008f, map); swing.Advance(.008f);
            Assert.That(player.EffectiveAttackSpeed, Is.EqualTo(4)); // body elapsed 250, now 10/tick
            for (int i = 0; i < 9; i++) { player.UpdatePhysics(.008f, map); swing.Advance(.008f); }
            Assert.That(swing.Frame, Is.Zero);
            player.UpdatePhysics(.008f, map); swing.Advance(.008f);
            Assert.That(swing.Frame, Is.EqualTo(1)); Assert.That(swing.HitDelayMilliseconds, Is.EqualTo(233));
            for (int i = 0; i < 44; i++) { player.UpdatePhysics(.008f, map); swing.Advance(.008f); }
            Assert.That(player.IsBasicAttacking, Is.True);
            player.UpdatePhysics(.008f, map); swing.Advance(.008f);
            Assert.That(player.IsBasicAttacking, Is.False);
        }

        [Test]
        public void FullHealthBuffPotionConsumesOneAndRefreshesWithoutStacking()
        {
            var player = Equipped(out var items);
            items.Values[2002004] = new ItemInfo { ItemId = 2002004, Name = "Warrior Potion", Type = ItemType.Use,
                IsStatBuffConsumable = true, Time = 180000, Buffs = new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 5 } };
            player.Inventory.AddItem(2002004, 3);
            Assert.That(player.TryUseItem(2002004, out _), Is.True); Assert.That(player.PhysicalAttackStats.Maximum, Is.EqualTo(16));
            player.UpdatePhysics(.008f, Map()); Assert.That(player.ActiveBuffs.Single().RemainingMilliseconds, Is.EqualTo(179992));
            Assert.That(player.TryUseItem(2002004, out _), Is.True);
            Assert.That(player.ActiveBuffs.Single().RemainingMilliseconds, Is.EqualTo(180000));
            Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.Inventory.GetItemCount(2002004), Is.EqualTo(1));
            items.Values[2002004].Buffs[BuffType.PowerGuard] = 5;
            Assert.That(player.TryUseItem(2002004, out _), Is.False); Assert.That(player.Inventory.GetItemCount(2002004), Is.EqualTo(1));
        }

        private sealed class Skills : IAssetProvider, ISkillDataProvider
        {
            public SkillInfo Info;
            public ISkillDataProvider SkillData => this;
            public IItemDataProvider ItemData => null; public IMobDataProvider MobData => null; public INpcDataProvider NpcData => null;
            public IMapDataProvider MapData => null; public ICharacterDataProvider CharacterData => null; public ISoundDataProvider SoundData => null;
            public void Initialize() { } public void Shutdown() { }
            public SkillInfo GetSkill(int id) => id == Info.SkillId ? Info : null;
            public bool SkillExists(int id) => GetSkill(id) != null;
            public Dictionary<int, SkillInfo> GetSkillsForJob(int job) => new Dictionary<int, SkillInfo> { [Info.SkillId] = Info };
        }

        [Test]
        public void RecastingDefinedSkillUsesSharedStatOwnershipAndDoesNotBakeEquipmentIntoBaseStats()
        {
            var player = Equipped(out _); player.Level = 20; player.JobId = 110;
            var data = new Skills { Info = new SkillInfo { SkillId = 1101004, JobId = 110, Name = "Test buff", Type = SkillType.Buff,
                Behavior = new SkillBehavior { Execution = SkillExecution.Self, RequiresAction = false, CastEffects = new[] { SkillCastEffects.StatBuff } },
                Levels = new Dictionary<int, SkillInfo.LevelData> { [1] = new SkillInfo.LevelData { Duration = 32, MpCost = 1,
                    Buffs = new Dictionary<BuffType, int> { [BuffType.WeaponAttack] = 5 } } } } };
            var manager = new SkillManager(player, data);
            Assert.That(manager.LearnSkill(1101004), Is.True);
            Assert.That(manager.UseSkill(1101004).Success, Is.True); Assert.That(manager.UseSkill(1101004).Success, Is.True);
            Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.CurrentMP, Is.EqualTo(48));
            player.TryUnequipItem(EquipSlot.Weapon, out _); Assert.That(player.WeaponAttack, Is.EqualTo(5));
            player.UpdatePhysics(.032f, Map()); manager.Update(.032f);
            Assert.That(manager.IsBuffActive(1101004), Is.False); Assert.That(player.WeaponAttack, Is.EqualTo(0));
            data.Info.Levels[1].Buffs[BuffType.PowerGuard] = 1;
            Assert.That(manager.UseSkill(1101004).Success, Is.False); Assert.That(player.CurrentMP, Is.EqualTo(48));
        }
    }
}
