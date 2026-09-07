using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class MagicSkillIntegrationTests
    {
        private sealed class Rolls : Random { public int Draws; public override int Next() => 0; public override double NextDouble() { Draws++; return .5; } }
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private static Player Equipped(out SkillManager skills)
        {
            var p = new Player { Level = 8, JobId = 200, Position = new Vector2(0, .3f), IsGrounded = true };
            p.SetItemData(Assets.ItemData); p.Inventory.AddItem(1372005, 1); Assert.That(p.TryEquipItem(1372005, out _), Is.True);
            skills = new SkillManager(p, Assets); skills.SetSkillLevel(2001004, 20); skills.SetSkillLevel(2001005, 20); return p;
        }
        private static Monster Mob(float x) => new Monster(new MonsterTemplate { MaxHP = 100, Level = 1,
            ContactAnimations = new Dictionary<string, MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -10, Top = -20, Right = 10, Bottom = 0, HeadY = -14, DelayMilliseconds = 100 }
            }, false) } }, new Vector2(x, 0));
        private static void Step(Combat c, int count = 1) { for (int i = 0; i < count; i++) c.Update(.008f); }
        [TestCase(2001004,14,1,55)] [TestCase(2001005,20,2,40)]
        public void RealMagicMetadataHasSourcePowerCostsCountsAndArtwork(int id, int cost, int count, int power)
        {
            var skill = Assets.SkillData.GetSkill(id); var level = skill.Levels[20];
            Assert.That(level.MpCost, Is.EqualTo(cost)); Assert.That(level.AttackCount, Is.EqualTo(count)); Assert.That(level.MagicAttack, Is.EqualTo(power));
            Assert.That(skill.Action, Is.Null, "These two spells use RegularAction with equipped weapon stances.");
            Assert.That(level.Projectile.Frames.Length > 0, Is.EqualTo(id == 2001004));
            var effect = SourceSkillRules.EffectsFor(skill, 8);
            foreach (var frame in effect.Hit.Frames.Concat(effect.Use.Frames).Concat(level.Projectile.Frames))
                Assert.That(Assets.GetNode("skill", frame.Path)?.Value, Is.TypeOf<byte[]>(), frame.Path);
        }
        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void EnergyBoltLaunchesBeforeDamageAndRetainsCapturedDamageAcrossHostIntervals(float dt)
        {
            var p = Equipped(out _); var c = new Combat(new Rolls(), new Rolls()); var victim = Mob(3.5f); var mobs = new List<Monster> { victim };
            var skill = Assets.SkillData.GetSkill(2001004); var hits = new List<AttackHit>(); c.AttackResolved += (_, __, hit) => hits.Add(hit);
            Assert.That(c.PerformSkillAttack(p, mobs, skill, skill.Levels[20]), Is.True);
            for (int i = 0; i < 300 && !c.Projectiles.Any(); i++) c.Update(dt);
            Assert.That(c.Projectiles.Count(), Is.EqualTo(1)); Assert.That(victim.HP, Is.EqualTo(100));
            p.WeaponAttack = 999; victim.Template.MagicDefense = 999;
            for (int i = 0; i < 600 && hits.Count == 0; i++) c.Update(dt);
            Assert.That(hits.Count, Is.EqualTo(1)); Assert.That(hits[0].Damage, Is.EqualTo(7)); Assert.That(victim.HP, Is.EqualTo(93));
            Step(c, 150); Assert.That(hits.Count, Is.EqualTo(1)); Assert.That(c.Projectiles, Is.Empty);
        }
        [Test]
        public void MagicClawRollsTwoLinesAtSourceWeaponDelayWithoutProjectileOrWeaponDefense()
        {
            var p = Equipped(out _); var rolls = new Rolls(); var c = new Combat(new Rolls(), rolls);
            var target = Mob(3.5f); target.Template.PhysicalDefense = 999; var skill = Assets.SkillData.GetSkill(2001005); var hits = new List<AttackHit>();
            c.AttackResolved += (_, __, hit) => hits.Add(hit); c.PerformSkillAttack(p, new List<Monster> { target }, skill, skill.Levels[20]);
            while (p.BasicAttack.ElapsedMilliseconds + 8 < p.BasicAttack.HitDelayMilliseconds) Step(c);
            Assert.That(hits, Is.Empty); Step(c);
            Assert.That(hits.Select(h => h.LineIndex), Is.EqualTo(new[] { 0, 1 })); Assert.That(hits.Select(h => h.Damage), Is.EqualTo(new[] { 7, 7 }));
            Assert.That(rolls.Draws, Is.EqualTo(6)); Assert.That(target.HP, Is.EqualTo(86)); Assert.That(c.Projectiles, Is.Empty);
        }
        [Test]
        public void BulletsRetainIdentityFollowMovingArrivalAndClearAcrossTravel()
        {
            var p = Equipped(out _); var c = new Combat(new Rolls(), new Rolls()); var original = Mob(3.5f); var mobs = new List<Monster> { original };
            var skill = Assets.SkillData.GetSkill(2001004); c.PerformSkillAttack(p, mobs, skill, skill.Levels[20]);
            for (int i = 0; i < 150 && !c.Projectiles.Any(); i++) Step(c);
            var bullet = c.Projectiles.Single(); double x = bullet.SourceX;
            original.Position = new Vector2(3.8f, .4f); Step(c); Assert.That(bullet.SourceX, Is.GreaterThan(x));
            mobs.Clear(); var replacement = Mob(3.8f); mobs.Add(replacement); Step(c, 180);
            Assert.That(original.HP, Is.EqualTo(100)); Assert.That(replacement.HP, Is.EqualTo(100)); Assert.That(c.Projectiles, Is.Empty);
            c.PerformSkillAttack(p, mobs, skill, skill.Levels[20]);
            for (int i = 0; i < 150 && !c.Projectiles.Any(); i++) Step(c);
            p.ResetMovementForMap(); Step(c); Assert.That(c.Projectiles, Is.Empty); Assert.That(replacement.HP, Is.EqualTo(100));
        }
        [Test]
        public void NoTargetCastStillLaunchesAndCannotDamageALateSpawn()
        {
            var p = Equipped(out _); var c = new Combat(new Rolls(), new Rolls()); var mobs = new List<Monster>(); var skill = Assets.SkillData.GetSkill(2001004);
            c.PerformSkillAttack(p, mobs, skill, skill.Levels[20]);
            for (int i = 0; i < 150 && !c.Projectiles.Any(); i++) Step(c);
            Assert.That(c.Projectiles.Count(), Is.EqualTo(1)); var late = Mob(2); mobs.Add(late); Step(c, 180);
            Assert.That(late.HP, Is.EqualTo(100)); Assert.That(c.Projectiles, Is.Empty);
        }
        [Test]
        public void SourceRangeUsesClosestTargetAndFourHundredPixelBoundary()
        {
            var p = Equipped(out _); var c = new Combat(new Rolls(), new Rolls()); var skill = Assets.SkillData.GetSkill(2001005);
            var inside = Mob(4.10f); var outside = Mob(4.11f); var behind = Mob(-.5f);
            c.PerformSkillAttack(p, new List<Monster> { outside, behind, inside }, skill, skill.Levels[20]); Step(c, 150);
            Assert.That(inside.HP, Is.EqualTo(86)); Assert.That(outside.HP, Is.EqualTo(100)); Assert.That(behind.HP, Is.EqualTo(100));
        }
        [Test]
        public void AnInFlightBoltSurvivesTheNextSwingButNotAChangedMapContext()
        {
            var p = Equipped(out _); var c = new Combat(new Rolls(), new Rolls()); var target = Mob(4.1f); var mobs = new List<Monster> { target };
            var bolt = Assets.SkillData.GetSkill(2001004); var claw = Assets.SkillData.GetSkill(2001005);
            c.PerformSkillAttack(p, mobs, bolt, bolt.Levels[20]);
            for (int i = 0; i < 200 && p.IsBasicAttacking; i++) Step(c);
            var first = c.Projectiles.Single();
            Assert.That(c.PerformSkillAttack(p, mobs, claw, claw.Levels[20]), Is.True);
            Step(c); Assert.That(c.Projectiles.Contains(first), Is.True);
            p.ResetMovementForMap(); Step(c);
            Assert.That(c.Projectiles, Is.Empty); Assert.That(target.HP, Is.EqualTo(100));
        }
        [TestCase(1)] [TestCase(20)]
        public void LoadedMagicPowerAndCharacterMagicAttackRemainUnusedByObservedSourceDamage(int level)
        {
            var p = Equipped(out _); p.MagicAttack = level == 1 ? 0 : 999;
            var c = new Combat(new Rolls(), new Rolls()); var target = Mob(3); target.Template.MagicDefense = 10;
            var skill = Assets.SkillData.GetSkill(2001005); var damage = new List<int>(); c.DamageDealt += (_, __, n) => damage.Add(n);
            c.PerformSkillAttack(p, new List<Monster> { target }, skill, skill.Levels[level]); Step(c, 160);
            Assert.That(damage, Is.EqualTo(new[] { 4, 4 }));
        }
    }
}
