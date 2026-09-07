using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class PlayerCombatProgressionTests
    {
        private static MapData FlatMap(int id = 1)
        {
            var map = new MapData { MapId = id };
            map.Platforms.Add(new Platform { Id = 1, X1 = -200, X2 = 200, Y1 = 0, Y2 = 0 });
            map.Portals.Add(new Portal { Id = 0, Type = PortalType.Spawn, X = 0, Y = -10 });
            return map;
        }

        private static Monster ContactMob(float x = 0, float y = 0)
        {
            return new Monster(new MonsterTemplate { MaxHP = 100, PhysicalDamage = 20, BodyAttack = true,
                ContactAnimations = new Dictionary<string, MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                    new MobContactFrame { Left = -15, Right = 15, Top = -30, Bottom = 0, DelayMilliseconds = 100 }
                }, false) } }, new Vector2(x, y));
        }

        [Test]
        public void ContactUsesAuthoredBoundsAndIgnoresDeadAndNonAttackingMobs()
        {
            var player = new Player { Position = new Vector2(0, .3f), WeaponDefense = 0 };
            var combat = new Combat();
            var mob = ContactMob(.3f);
            Assert.That(combat.CheckContact(player, new[] { mob }), Is.Null, "Outside the authored 15-pixel half width.");
            mob.Position = Vector2.Zero; mob.Template.BodyAttack = false;
            Assert.That(combat.CheckContact(player, new[] { mob }), Is.Null);
            mob.Template.BodyAttack = true; mob.TakeDamage(1000);
            Assert.That(combat.CheckContact(player, new[] { mob }), Is.Null);
            var live = ContactMob();
            Assert.That(combat.CheckContact(player, new[] { live }), Is.SameAs(live));
            Assert.That(player.CurrentHP, Is.InRange(80, 84));
            Assert.That(combat.CheckContact(player, new[] { live }), Is.Null);
        }

        [Test]
        public void ContactSweepsPlayerMotionAndKeepsSeparatePlatformsApart()
        {
            var map = FlatMap();
            var player = new Player { Position = new Vector2(-.25f, .3f), Velocity = new Vector2(60, 0), IsGrounded = true };
            player.UpdatePhysics(.008f, map);
            Assert.That(player.Position.X, Is.GreaterThan(.1f));
            var combat = new Combat();
            Assert.That(combat.CheckContact(player, new[] { ContactMob(0, -1) }), Is.Null);
            Assert.That(combat.CheckContact(player, new[] { ContactMob() }), Is.Not.Null, "The tick crosses the monster bounds.");
        }

        [Test]
        public void HitReactionChangesContactBoundsOnTheDamageTick()
        {
            var mob = ContactMob();
            mob.Template.ContactAnimations["hit1"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -20, Right = 20, Top = -40, Bottom = 0, DelayMilliseconds = 600 }
            }, false);
            Assert.That(mob.ContactBounds.Top, Is.EqualTo(-30));
            mob.TakeDamage(1);
            Assert.That(mob.ContactBounds.Top, Is.EqualTo(-40));
        }

        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void InvulnerabilityExpiresAfterTwoSecondsOfSimulation(float hostDelta)
        {
            var player = new Player { Position = new Vector2(0, .3f), IsGrounded = true };
            Assert.That(player.ReceiveContactDamage(5, false), Is.True);
            for (int i = 0; i < (int)System.Math.Round(1.984 / hostDelta); i++) player.UpdatePhysics(hostDelta, FlatMap());
            Assert.That(player.IsInvulnerable, Is.True);
            Assert.That(player.ReceiveContactDamage(5, false), Is.False);
            player.UpdatePhysics(.016f, FlatMap());
            Assert.That(player.IsInvulnerable, Is.False);
            Assert.That(player.ReceiveContactDamage(5, false), Is.True);
            Assert.That(player.CurrentHP, Is.EqualTo(90));
        }

        [TestCase(false, 1)] [TestCase(true, -1)]
        public void ContactKnockbackUsesSourceHorizontalSpeedAndVerticalForce(bool sourceToRight, int direction)
        {
            var map = FlatMap(); var player = new Player { Position = new Vector2(0, .3f), IsGrounded = true };
            player.UpdatePhysics(.008f, map);
            player.ReceiveContactDamage(10, sourceToRight);
            player.UpdatePhysics(.008f, map);
            Assert.That(player.Velocity.X, Is.EqualTo(direction * 1.5f).Within(.00001), "1.5 source speed minus ground friction, converted to units/second.");
            Assert.That(player.Velocity.Y, Is.EqualTo(4.375f).Within(.00001), "-3.5 source pixels per tick.");
        }

        [Test]
        public void DeathFreezesMovementAndAttacksUntilExplicitRevival()
        {
            var input = new FakeInputProvider(); var loader = new FakeMapLoader(); loader.AddMap(1, FlatMap());
            var world = new GameWorld(input, loader); world.LoadMap(1); world.UpdatePhysics(.008f);
            int deaths = 0; world.Player.Died += () => deaths++;
            world.Player.TakeDamage(1000); var position = world.Player.Position;
            world.Player.TakeDamage(1000); world.Player.Heal(1000);
            world.Player.Inventory.AddItem(2000000, 1);
            Assert.That(world.Player.UseItem(2000000), Is.False);
            Assert.That(world.Player.Inventory.GetItemCount(2000000), Is.EqualTo(1));
            Assert.That(new MapleClient.GameLogic.Skills.SkillManager(world.Player, null).UseSkill(1000).ErrorMessage,
                Is.EqualTo("Cannot use skills while defeated"));
            input.IsRightPressed = input.IsJumpPressed = input.IsAttackPressed = true;
            for (int i = 0; i < 50; i++) world.Update(.016f);
            Assert.That(world.Player.IsDead, Is.True); Assert.That(deaths, Is.EqualTo(1));
            Assert.That(world.Player.Position, Is.EqualTo(position));
            Assert.That(new Combat().CanPlayerAttack(world.Player), Is.False);
            Assert.That(world.RevivePlayer(), Is.True); Assert.That(world.RevivePlayer(), Is.False);
            Assert.That(world.Player.CurrentHP, Is.EqualTo(world.Player.MaxHP));
            Assert.That(world.Player.IsInvulnerable, Is.True);
        }

        [Test]
        public void ContactDamagesClimbingPlayerWithoutDislodgingThem()
        {
            var map = FlatMap();
            map.Ladders.Add(new LadderInfo { Id = 1, IsLadder = true, X = 0, Y1 = 0, Y2 = 1 });
            var player = new Player { Position = new Vector2(0, .3f), IsGrounded = true };
            player.ClimbUp(true); player.UpdatePhysics(.008f, map); player.ClimbUp(false);
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
            var position = player.Position;
            player.ReceiveContactDamage(10, true); player.UpdatePhysics(.008f, map);
            Assert.That(player.CurrentHP, Is.EqualTo(90));
            Assert.That(player.Position, Is.EqualTo(position));
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
        }

        [Test]
        public void RevivalWithNoSafeTerrainLeavesPlayerDefeated()
        {
            var loader = new FakeMapLoader(); loader.AddMap(1, new MapData { MapId = 1 });
            var world = new GameWorld(new FakeInputProvider(), loader); world.LoadMap(1);
            world.Player.TakeDamage(1000);
            Assert.That(world.RevivePlayer(), Is.False);
            Assert.That(world.Player.IsDead, Is.True);
        }

        [Test]
        public void RevivalUsesReturnMapAndPreservesProgress()
        {
            var loader = new FakeMapLoader(); var map = FlatMap(); map.ReturnMapId = 2;
            loader.AddMap(1, map); loader.AddMap(2, FlatMap(2));
            var world = new GameWorld(new FakeInputProvider(), loader); world.LoadMap(1);
            world.Player.AddExperience(7); world.Player.TakeDamage(1000);
            Assert.That(world.RevivePlayer(), Is.True); Assert.That(world.CurrentMapId, Is.EqualTo(2));
            Assert.That(world.Player.Experience, Is.EqualTo(7));
        }

        [Test]
        public void ExperienceCarriesRemainderAcrossMultipleLevelsAndHonorsCap()
        {
            var player = new Player(); var levels = new List<int>(); player.LeveledUp += levels.Add;
            player.TakeDamage(10); player.AddExperience(15 + 34 + 3);
            Assert.That(player.Level, Is.EqualTo(3)); Assert.That(player.Experience, Is.EqualTo(3));
            Assert.That(player.ExperienceToNextLevel, Is.EqualTo(57));
            Assert.That(levels, Is.EqualTo(new[] { 2, 3 })); Assert.That(player.CurrentHP, Is.EqualTo(100));
            player.AddExperience(-1); Assert.That(player.Experience, Is.EqualTo(3));
            player.AddExperience(long.MaxValue); Assert.That(player.Level, Is.EqualTo(200));
            Assert.That(player.Experience, Is.Zero); Assert.That(player.ExperienceToNextLevel, Is.Zero);
        }

        [Test]
        public void OnlyALethalPlayerAttackAwardsMonsterExperienceOnce()
        {
            var loader = new FakeMapLoader(); var map = FlatMap();
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 1, X = 50, Y = -10, SpawnInterval = -1 });
            loader.AddMap(1, map); var input = new FakeInputProvider(); var world = new GameWorld(input, loader);
            world.LoadMap(1); var mob = world.Monsters[0]; mob.Template.Exp = 4;
            mob.TakeDamage(1); Assert.That(world.Player.Experience, Is.Zero);
            world.Player.SetBaseDamage(1000); input.IsAttackPressed = true; world.Update(.008f);
            Assert.That(mob.IsDead, Is.True); Assert.That(world.Player.Experience, Is.EqualTo(4));
            for (int i = 0; i < 100; i++) world.Update(.008f);
            Assert.That(world.Player.Experience, Is.EqualTo(4));
        }
    }
}
