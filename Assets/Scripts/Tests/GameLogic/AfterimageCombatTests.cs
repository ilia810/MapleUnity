using System;
using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.Tests.GameLogic
{
    public class AfterimageCombatTests
    {
        private sealed class FirstAttack : Random { public override int Next() => 0; }
        private sealed class Items : IItemDataProvider
        {
            public ItemInfo Sword;
            public ItemInfo GetItem(int id) => id == 1302000 ? Sword : null;
            public bool ItemExists(int id) => GetItem(id) != null;
            public Dictionary<int, ItemInfo> GetAllItems() => new Dictionary<int, ItemInfo> { [1302000] = Sword };
        }
        private Player player;
        private Combat combat;
        private WeaponProfile weapon;
        private static MapData FlatMap()
        {
            var map = new MapData(); map.Platforms.Add(new Platform { Id = 1, X1 = -1000, X2 = 1000, Y1 = 0, Y2 = 0 }); return map;
        }
        [SetUp] public void Setup()
        {
            weapon = new WeaponProfile { AttackType = 1, AttackSpeed = 4 };
            weapon.FrameDelays[CharacterState.Attack1] = new[] { 350, 450 };
            weapon.Afterimages[CharacterState.Attack1] = new WeaponAfterimage { FirstFrame = 1, HasBounds = true,
                Bounds = new AttackBounds(-84, -30, -20, -15), Frames = new[] { new AfterimageFrame { DelayMilliseconds = 230 } } };
            player = new Player { Position = new Vector2(0, Player.Height / 2), IsGrounded = true };
            player.SetItemData(new Items { Sword = new ItemInfo { ItemId = 1302000, Type = AssetItemType.Equip, EquipmentSlot = EquipSlot.Weapon,
                Weapon = weapon, Stats = new Dictionary<StatType, int>() } });
            player.Inventory.AddItem(1302000, 1); Assert.That(player.TryEquipItem(1302000, out _), Is.True);
            player.SetBaseDamage(1); combat = new Combat(new FirstAttack());
        }
        private static Monster Mob(float x, float y = 0) => new Monster(new MonsterTemplate { MaxHP = 100,
            ContactAnimations = new Dictionary<string, MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -10, Top = -20, Right = 10, Bottom = 0, DelayMilliseconds = 100 }
            }, false) } }, new Vector2(x, y));
        private void FaceLeft()
        {
            player.MoveLeft(true); player.UpdatePhysics(.008f, FlatMap()); player.MoveLeft(false);
            player.Position = new Vector2(0, Player.Height / 2); player.Velocity = Vector2.Zero;
            Assert.That(player.FacingRight, Is.False);
        }

        [TestCase(.94f, 0, true)] [TestCase(.95f, 0, false)]
        [TestCase(.5f, .30f, true)] [TestCase(.5f, .31f, false)]
        [TestCase(-.5f, 0, false)] [TestCase(0f, 0, false)]
        public void SourceRectangleTestsMonsterEdgesAndVerticalReach(float x, float y, bool hits)
        {
            var mob = Mob(x, y);
            Assert.That(combat.PerformBasicAttack(player, new List<Monster> { mob }, 100).Count, Is.EqualTo(hits ? 1 : 0), "Legacy range must not override weapon metadata.");
            Assert.That(mob.HP, Is.EqualTo(100)); combat.Update(.272f);
            Assert.That(mob.HP, Is.EqualTo(hits ? 99 : 100));
        }
        [Test]
        public void LeftFacingMirrorsOnlyAttackBoundsAndRetainsCapturedKnockbackDirection()
        {
            FaceLeft(); var mob = Mob(-.5f); var behind = Mob(.5f);
            var selected = combat.PerformBasicAttack(player, new List<Monster> { behind, mob }, 1);
            Assert.That(selected, Is.EqualTo(new[] { mob }));
            // Changing input and crossing the target after selection must not reverse the hit.
            player.MoveRight(true); player.UpdatePhysics(.008f, FlatMap()); player.MoveRight(false);
            player.Position = new Vector2(-2, .3f); combat.Update(.272f);
            Assert.That(mob.HP, Is.EqualTo(99)); Assert.That(behind.HP, Is.EqualTo(100));
            Assert.That(mob.FacingRight, Is.True, "A leftward hit keeps the source's leftward knockback flag.");
        }
        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void DamageWaitsForSourceFloatDelayAtEveryHostRate(float delta)
        {
            var mob = Mob(.5f); int hits = 0; combat.DamageDealt += (_, __, ___) => hits++;
            combat.PerformBasicAttack(player, new List<Monster> { mob }, 1);
            Assert.That(player.BasicAttack.HitDelayMilliseconds, Is.EqualTo(269));
            for (int i = 0; i < (int)Math.Round(.256 / delta); i++) combat.Update(delta);
            Assert.That(mob.HP, Is.EqualTo(100));
            for (int i = 0; i < (int)Math.Round(.016 / delta); i++) combat.Update(delta);
            Assert.That(mob.HP, Is.EqualTo(99)); Assert.That(player.BasicAttack.Frame, Is.Zero);
            combat.Update(.008f); Assert.That(player.BasicAttack.Frame, Is.EqualTo(1));
            Assert.That(player.BasicAttack.AfterimageMilliseconds, Is.Zero);
            combat.Update(.4f); Assert.That(hits, Is.EqualTo(1));
        }
        [Test]
        public void ClosestTargetIsChosenAtStartAndLateArrivalsCannotStealTheHit()
        {
            var far = Mob(.7f); var close = Mob(.5f); var late = Mob(2);
            Assert.That(combat.PerformBasicAttack(player, new List<Monster> { far, close, late }, 1), Is.EqualTo(new[] { close }));
            close.Position = new Vector2(3, 0); late.Position = new Vector2(.4f, 0);
            combat.Update(.272f);
            Assert.That(close.HP, Is.EqualTo(99)); Assert.That(far.HP, Is.EqualTo(100)); Assert.That(late.HP, Is.EqualTo(100));
        }
        [TestCase("map")] [TestCase("death")]
        public void InterruptedSwingCannotApplyDamageOrRewards(string interrupt)
        {
            var mob = Mob(.5f); int hits = 0; combat.DamageDealt += (_, __, ___) => hits++;
            combat.PerformBasicAttack(player, new List<Monster> { mob }, 1);
            if (interrupt == "map") player.ResetMovementForMap();
            else if (interrupt == "death") player.TakeDamage(1000);
            combat.Update(.8f);
            Assert.That(mob.HP, Is.EqualTo(100)); Assert.That(hits, Is.Zero); Assert.That(player.BasicAttack.IsCancelled, Is.True);
        }
        [Test]
        public void HoldingUpDuringSwingWaitsForCompletionToGrabLadderWithoutCancelingDamage()
        {
            var map = FlatMap(); map.Ladders.Add(new LadderInfo { Id = 1, IsLadder = true, X = 0, Y1 = 0, Y2 = 1 });
            var mob = Mob(.5f); combat.PerformBasicAttack(player, new List<Monster> { mob }, 1);
            player.ClimbUp(true); player.UpdatePhysics(.008f, map);
            Assert.That(player.State, Is.Not.EqualTo(PlayerState.Climbing));
            combat.Update(.272f); Assert.That(mob.HP, Is.EqualTo(99));
            combat.Update(.368f);
            Assert.That(player.BasicAttack.IsCancelled, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
        }
        [TestCase(true)] [TestCase(false)]
        public void DeathOrDespawnBeforeImpactCannotDamageRespawnWithTheSameId(bool died)
        {
            var old = Mob(.5f); old.Id = 10;
            int kills = 0; combat.MonsterDefeated += (_, __) => kills++;
            var monsters = new List<Monster> { old };
            combat.PerformBasicAttack(player, monsters, 1);
            if (died) old.TakeDamage(1000); else monsters.Remove(old);
            var replacement = Mob(.5f); replacement.Id = old.Id; monsters.Add(replacement);
            combat.Update(.8f);
            Assert.That(replacement.HP, Is.EqualTo(100)); Assert.That(kills, Is.Zero);
            if (!died) Assert.That(old.HP, Is.EqualTo(100));
        }
        [Test]
        public void LethalDamageAndRewardEventOccurOnceAtImpact()
        {
            var mob = Mob(.5f); player.SetBaseDamage(1000);
            int kills = 0; combat.MonsterDefeated += (_, __) => kills++;
            combat.PerformBasicAttack(player, new List<Monster> { mob }, 1);
            Assert.That(mob.IsDead, Is.False); Assert.That(kills, Is.Zero);
            combat.Update(.272f); Assert.That(mob.IsDead, Is.True); Assert.That(kills, Is.EqualTo(1));
            combat.Update(1); Assert.That(kills, Is.EqualTo(1));
        }
        [Test]
        public void MissingWeaponBoundsDoNotInventAHitbox()
        {
            weapon.Afterimages.Clear(); var mob = Mob(.5f);
            Assert.That(combat.PerformBasicAttack(player, new List<Monster> { mob }, 100), Is.Empty);
            combat.Update(1); Assert.That(mob.HP, Is.EqualTo(100));
        }
    }
}
