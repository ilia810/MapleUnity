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
    public class MeleeSkillIntegrationTests
    {
        private sealed class Rolls : Random { public override int Next() => 0; public override double NextDouble() => .5; }
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private static Player Equipped(out SkillManager skills)
        {
            var p = new Player { JobId = 110, Position = new Vector2(0, .3f), IsGrounded = true };
            p.SetItemData(Assets.ItemData); p.Inventory.AddItem(1302000, 1); p.TryEquipItem(1302000, out _);
            skills = new SkillManager(p, Assets); skills.SetSkillLevel(1100000, 20); return p;
        }
        private static Monster Mob(float x) => new Monster(new MonsterTemplate { MaxHP = 100, Level = 1,
            ContactAnimations = new Dictionary<string,MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -10, Top = -20, Right = 10, Bottom = 0, DelayMilliseconds = 100 }
            }, false) } }, new Vector2(x, 0));
        [TestCase(1001004,1,25)] [TestCase(1001005,6,12)]
        public void RealMeleeSkillsCaptureClosestTargetsAndAdvanceOnlyOneSwing(int id, int count, int damage)
        {
            var p = Equipped(out _); var combat = new Combat(new Rolls(), new Rolls());
            var mobs = Enumerable.Range(0,8).Select(i => Mob(.2f + i*.1f)).Reverse().ToList();
            var skill = Assets.SkillData.GetSkill(id); int reports = 0;
            combat.AttackResolved += (_, target, hit) => { reports++; Assert.That(hit.Damage, Is.EqualTo(damage)); };
            Assert.That(combat.PerformSkillAttack(p, mobs, skill, skill.Levels[20]), Is.True);
            int delay = p.BasicAttack.HitDelayMilliseconds;
            var baseline = p.CurrentWeapon.CreateAttack(0, false);
            while (p.BasicAttack.ElapsedMilliseconds + 8 < delay) { combat.Update(.008f); baseline.Advance(.008f); }
            Assert.That(reports, Is.Zero); Assert.That(mobs.All(m => m.HP == 100), Is.True);
            combat.Update(.008f); baseline.Advance(.008f);
            Assert.That(reports, Is.EqualTo(count)); Assert.That(p.BasicAttack.Frame, Is.EqualTo(baseline.Frame));
            var ordered = mobs.OrderBy(m => m.Position.X).ToArray();
            for (int i = 0; i < mobs.Count; i++) Assert.That(ordered[i].HP, Is.EqualTo(i < count ? 100 - damage : 100));
            int ticks = 0;
            while (p.IsBasicAttacking && ticks++ < 200) { combat.Update(.008f); baseline.Advance(.008f); Assert.That(p.BasicAttack.Frame, Is.EqualTo(baseline.Frame)); }
            Assert.That(baseline.IsComplete, Is.True); Assert.That(reports, Is.EqualTo(count));
        }
        [TestCase(false)] [TestCase(true)]
        public void SlashBlastScalesOnlyForwardReachWithSourceTruncation(bool left)
        {
            var p = Equipped(out _);
            if (left)
            {
                var map = new MapData(); map.Platforms.Add(new Platform {Id=1,X1=-1000,X2=1000,Y1=0,Y2=0});
                p.MoveLeft(true); p.UpdatePhysics(.008f,map); p.MoveLeft(false); p.Position = new Vector2(0,.3f);
            }
            float sign = left ? -1 : 1;
            var inside = Mob(sign*1.36f); var outside = Mob(sign*1.37f); var behind = Mob(-sign*.5f);
            var combat = new Combat(new Rolls(),new Rolls()); var skill = Assets.SkillData.GetSkill(1001005);
            Assert.That(combat.PerformSkillAttack(p,new List<Monster>{outside,behind,inside},skill,skill.Levels[20]),Is.True);
            for(int i=0;i<40;i++)combat.Update(.008f);
            Assert.That(inside.HP,Is.LessThan(100)); Assert.That(outside.HP,Is.EqualTo(100)); Assert.That(behind.HP,Is.EqualTo(100));
        }
        [Test]
        public void DamageAndMembershipAreCapturedBeforeImpactAndTravelCancelsAllTargets()
        {
            var p = Equipped(out _); var combat = new Combat(new Rolls(),new Rolls()); var skill=Assets.SkillData.GetSkill(1001005);
            var first=Mob(.3f);var second=Mob(.4f);var mobs=new List<Monster>{first,second};
            combat.PerformSkillAttack(p,mobs,skill,skill.Levels[20]);
            p.WeaponAttack=900; second.Template.PhysicalDefense=999;
            mobs.Remove(first); var replacement=Mob(.3f);mobs.Add(replacement);
            for(int i=0;i<40;i++)combat.Update(.008f);
            Assert.That(first.HP,Is.EqualTo(100));Assert.That(replacement.HP,Is.EqualTo(100));Assert.That(second.HP,Is.EqualTo(88));
            p.ResetMovementForMap(); combat.Update(.008f);
            combat.PerformSkillAttack(p,mobs,skill,skill.Levels[20]);p.ResetMovementForMap();
            for(int i=0;i<100;i++)combat.Update(.008f);
            Assert.That(replacement.HP,Is.EqualTo(100));Assert.That(second.HP,Is.EqualTo(88));
        }
        [TestCase(1,10)] [TestCase(10,10)] [TestCase(15,10)] [TestCase(16,15)] [TestCase(25,20)] [TestCase(26,25)]
        public void CharacterLevelEffectSelectionUsesSourceBoundary(int level,int folder)
        {
            foreach(int id in new[]{1001004,1001005}) {
                var effect=SourceSkillRules.EffectsFor(Assets.SkillData.GetSkill(id),level);
                Assert.That(effect.Use.Frames[0].Path,Does.Contain("/CharLevel/"+folder+"/effect/"));
                Assert.That(effect.Hit.Frames[0].Path,Does.Contain("/CharLevel/"+folder+"/hit/0/"));
            }
        }
    }
}
