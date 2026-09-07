using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class RangedCombatIntegrationTests
    {
        private sealed class Rolls : Random { public override int Next() => 0; public override double NextDouble() => .5; }
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private static Player Equipped(int type, int ammoCount = 1)
        {
            var p = new Player { JobId = type <= 146 ? 300 : type == 147 ? 400 : 500, Level = 12, Position = new Vector2(0,.3f), IsGrounded = true };
            p.SetItemData(Assets.ItemData); int id=GameWorld.RangedPracticeWeapon(type);
            p.Inventory.AddItem(id,1);Assert.That(p.TryEquipItem(id,out _),Is.True);p.Inventory.AddItem(AmmunitionRules.Prefix(type)*1000,ammoCount);return p;
        }
        private static Monster Mob(float x) => new Monster(new MonsterTemplate { MaxHP = 1000, Level = 1,
            ContactAnimations = new Dictionary<string,MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left=-10,Top=-20,Right=10,Bottom=0,HeadY=-14,DelayMilliseconds=100 }
            },false) } },new Vector2(x,0));
        private static void Step(Combat c,int ticks=1) {for(int i=0;i<ticks;i++)c.Update(.008f);}
        private static MapData FlatMap()
        {
            var map=new MapData();map.Platforms.Add(new Platform { Id=1,X1=-1000,X2=1000,Y1=0,Y2=0 });return map;
        }
        [TestCase(145)] [TestCase(146)] [TestCase(147)] [TestCase(149)]
        public void LeftFacingShotKeepsItsTargetSideAndCrouchingUsesCloseRange(int type)
        {
            var p=Equipped(type,3);var c=new Combat(new Rolls(),new Rolls());
            p.MoveLeft(true);p.UpdatePhysics(.008f,FlatMap());p.MoveLeft(false);
            p.Position=new Vector2(0,Player.Height/2);p.Velocity=Vector2.Zero;
            Assert.That(p.FacingRight,Is.False);
            var front=Mob(-3);var behind=Mob(.5f);var mobs=new List<Monster>{behind,front};
            Assert.That(c.PerformBasicAttack(p,mobs,1),Is.EqualTo(new[]{front}));
            for(int i=0;i<160&&!c.Projectiles.Any();i++)Step(c);
            Assert.That(c.Projectiles.Single().FacingRight,Is.False);
            Step(c,180);Assert.That(front.HP,Is.LessThan(1000));Assert.That(behind.HP,Is.EqualTo(1000));
            var standing=p.PhysicalAttackStats;p.Crouch(true);p.UpdatePhysics(.008f,FlatMap());
            Assert.That(p.State,Is.EqualTo(PlayerState.Crouching));
            Assert.That(p.PhysicalAttackStats.Maximum,Is.EqualTo(standing.Maximum/10).Within(.00001));
            Assert.That(c.PerformBasicAttack(p,mobs,1),Is.Empty,"Prone attacks do not retain 400-pixel reach.");
            Assert.That(p.BasicAttack.Stance,Is.EqualTo(CharacterState.ProneStab));
            Assert.That(p.AmmunitionCount,Is.EqualTo(1));
            for(int i=0;i<160&&!c.Projectiles.Any();i++)Step(c);
            Assert.That(c.Projectiles.Any(),Is.True,"The source retains its bullet even while prone.");
        }
        [TestCase(145)] [TestCase(146)] [TestCase(147)] [TestCase(149)]
        public void LastRoundKeepsItsBonusAndHitsOnlyAfterTravel(int type)
        {
            var p=Equipped(type);var c=new Combat(new Rolls(),new Rolls());var mob=Mob(3);var mobs=new List<Monster>{mob};
            var expected=p.PhysicalAttackStats.Roll(mob.Template,new Rolls());int ammoId=p.AmmunitionId;
            var hits=new List<AttackHit>();c.AttackResolved+=(_,__,hit)=>hits.Add(hit);
            c.PerformBasicAttack(p,mobs,1);
            Assert.That(p.Inventory.GetItemCount(ammoId),Is.Zero);Assert.That(p.IsBasicAttacking,Is.True);
            for(int i=0;i<160 && !c.Projectiles.Any();i++)Step(c);
            Assert.That(c.Projectiles.Count(),Is.EqualTo(1));Assert.That(mob.HP,Is.EqualTo(1000));
            Assert.That(c.Projectiles.Single().AssetFile,Is.EqualTo("item"));
            Step(c,180);Assert.That(hits.Count,Is.EqualTo(1));Assert.That(hits[0].Damage,Is.EqualTo(expected.Damage));
            Assert.That(c.CanPlayerAttack(p),Is.False);var previous=p.BasicAttack;c.PerformBasicAttack(p,mobs,1);
            Assert.That(p.BasicAttack,Is.SameAs(previous));Assert.That(c.Projectiles,Is.Empty);
        }
        [TestCase(145)] [TestCase(146)] [TestCase(147)] [TestCase(149)]
        public void EmptyCastSpendsOneRoundAndDoesNotSelectALateTarget(int type)
        {
            var p=Equipped(type,2);var c=new Combat(new Rolls(),new Rolls());var mobs=new List<Monster>();
            c.PerformBasicAttack(p,mobs,1);Assert.That(p.AmmunitionCount,Is.EqualTo(1));
            c.PerformBasicAttack(p,mobs,1);Assert.That(p.AmmunitionCount,Is.EqualTo(1));
            for(int i=0;i<160 && !c.Projectiles.Any();i++)Step(c);
            Assert.That(c.Projectiles.Any(),Is.True);var late=Mob(2);mobs.Add(late);Step(c,180);Assert.That(late.HP,Is.EqualTo(1000));
        }
        [TestCase(145)] [TestCase(146)] [TestCase(147)] [TestCase(149)]
        public void FourHundredPixelReachAndTargetMembershipArePreserved(int type)
        {
            var p=Equipped(type,3);var c=new Combat(new Rolls(),new Rolls());var inside=Mob(4.1f);var outside=Mob(4.11f);var behind=Mob(-.5f);
            var mobs=new List<Monster>{outside,behind,inside};var chosen=c.PerformBasicAttack(p,mobs,1);
            Assert.That(chosen,Is.EqualTo(new[]{inside}));Step(c,200);Assert.That(inside.HP,Is.LessThan(1000));Assert.That(outside.HP,Is.EqualTo(1000));
            int hp=inside.HP;c.PerformBasicAttack(p,mobs,1);mobs.Remove(inside);var replacement=Mob(4.1f);mobs.Add(replacement);Step(c,200);
            Assert.That(inside.HP,Is.EqualTo(hp));Assert.That(replacement.HP,Is.EqualTo(1000));
            c.PerformBasicAttack(p,mobs,1);p.ResetMovementForMap();Step(c,200);Assert.That(c.Projectiles,Is.Empty);Assert.That(replacement.HP,Is.EqualTo(1000));
        }
        [Test]
        public void RealAmmoFollowsCompatibleSlotOrderAndReplacesItsStatBonus()
        {
            var p=Equipped(147,0);p.Inventory.AddItem(2060001,9);p.Inventory.AddItem(2070001,1);p.Inventory.AddItem(2070000,4);
            Assert.That(p.AmmunitionId,Is.EqualTo(2070001));Assert.That(p.WeaponAttack,Is.EqualTo(27));
            var c=new Combat(new Rolls(),new Rolls());c.PerformBasicAttack(p,new List<Monster>(),1);
            Assert.That(p.AmmunitionId,Is.EqualTo(2070000));Assert.That(p.WeaponAttack,Is.EqualTo(25));
            Step(c,200);p.TryUnequipItem(EquipSlot.Weapon,out _);Assert.That(p.AmmunitionId,Is.Zero);Assert.That(p.WeaponAttack,Is.Zero);
            p.TryEquipItem(1472000,out _);Assert.That(p.WeaponAttack,Is.EqualTo(25));
        }
        [TestCase(145,2061000)] [TestCase(147,2070999)]
        public void WrongAmmoAndMissingArtworkCannotSpendAmmunitionOrStartAnAttack(int type, int ammo)
        {
            var p=Equipped(type,0);p.Inventory.AddItem(ammo,5);var c=new Combat();
            Assert.That(c.CanPlayerAttack(p),Is.False);c.PerformBasicAttack(p,new List<Monster>(),1);Assert.That(p.BasicAttack,Is.Null);
            Assert.That(p.Inventory.GetItemCount(ammo),Is.EqualTo(5));
        }
        [Test]
        public void ServerOwnedInventoryCountsAreNotDecrementedLocally()
        {
            var p=Equipped(149,4);var c=new Combat(new Rolls(),new Rolls(),consumeAmmunition:false);
            c.PerformBasicAttack(p,new List<Monster>(),1);Assert.That(p.AmmunitionCount,Is.EqualTo(4));Assert.That(p.IsBasicAttacking,Is.True);
        }
        [Test]
        public void GunLoadsSignedDelaysActionAliasesAndVisualMoveFromNx()
        {
            var p=Equipped(149);var gun=p.CurrentWeapon;
            Assert.That(gun.ActionDelays,Is.EqualTo(new[]{240,540,100}));Assert.That(gun.ActionHitDelay,Is.EqualTo(240));
            Assert.That(gun.ActionStances,Is.EqualTo(new[]{CharacterState.Shoot2,CharacterState.Attack1,CharacterState.Shoot2}));
            var motion=gun.CreateAttack(0,false);Assert.That(motion.HitDelayMilliseconds,Is.EqualTo(199));
            for(int i=0;i<27;i++)motion.Advance(.008f);
            Assert.That(motion.Stance,Is.EqualTo(CharacterState.Attack1));Assert.That(motion.VisualOffset,Is.EqualTo(new Vector2(2,0)));
        }
        [TestCase(2060000,0)] [TestCase(2060001,1)] [TestCase(2061000,0)] [TestCase(2070000,15)] [TestCase(2070001,17)] [TestCase(2330000,10)]
        public void RealAmmunitionReadsBitmapOrAnimationAndAttackBonus(int id,int bonus)
        {
            var item=Assets.ItemData.GetItem(id);Assert.That(item.Ammunition.Frames.Length,Is.EqualTo(id/10000==207?2:1));
            Assert.That(item.Stats.TryGetValue(StatType.WeaponAttack,out int value)?value:0,Is.EqualTo(bonus));
            foreach(var frame in item.Ammunition.Frames)
            {
                Assert.That(Assets.GetNode("item",frame.Path)?.Value,Is.TypeOf<byte[]>());
                Assert.That(MapleClient.GameData.SpriteLoader.LoadSprite(Assets.GetNode("item",frame.Path),"item/"+frame.Path),Is.Not.Null);
            }
        }
    }
}
