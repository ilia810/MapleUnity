using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Globalization;
using GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class RangedSkillIntegrationTests
    {
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private sealed class Rolls : Random
        {
            public int Draws;
            public override int Next() => 0;
            public override double NextDouble() => new[]{ .5,.25,.01,.5,.75,.5 }[Draws++ % 6];
        }
        private static Player Equipped(int type, int id, int ammunition, out SkillManager skills, out Combat combat, out List<Monster> mobs, out Rolls rolls)
        {
            var p=new Player { JobId=type<=146?300:type==147?400:500, Level=12, Position=new Vector2(0,Player.Height/2),IsGrounded=true };
            p.SetItemData(Assets.ItemData);int weapon=GameWorld.RangedPracticeWeapon(type);
            p.Inventory.AddItem(weapon,1);Assert.That(p.TryEquipItem(weapon,out _),Is.True);
            p.Inventory.AddItem(AmmunitionRules.Prefix(type)*1000,ammunition);
            skills=new SkillManager(p,Assets);Assert.That(skills.SetSkillLevel(id,20),Is.True);
            var random=new Rolls();var c=new Combat(new Rolls(),random);var targets=new List<Monster>();
            typeof(SkillManager).GetField("StartAttackSkill",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(skills,
                new Func<SkillInfo,SkillInfo.LevelData,bool>((s,l)=>c.PerformSkillAttack(p,targets,s,l)));
            combat=c;mobs=targets;rolls=random;return p;
        }
        private static Monster Mob(float x) => new Monster(new MonsterTemplate { MaxHP=1000,Level=1,
            ContactAnimations=new Dictionary<string,MobContactAnimation> { ["stand"]=new MobContactAnimation(new[]{
                new MobContactFrame { Left=-10,Top=-20,Right=10,Bottom=0,HeadY=-14,DelayMilliseconds=100 }
            },false) } },new Vector2(x,0));
        private static void Step(Combat c,int ticks=1) {for(int i=0;i<ticks;i++)c.Update(.008f);}
        private static MapData FlatMap() {var m=new MapData();m.Platforms.Add(new Platform {Id=1,X1=-2000,X2=2000,Y1=0,Y2=0});return m;}

        private static string[][] Reference(string name) => File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),
            "Assets/Scripts/Tests/GameLogic/Fixtures/"+name+".csv")).Skip(1).Select(l=>l.Split(',')).ToArray();
        private static int Int(string[] row,int column) => int.Parse(row[column],CultureInfo.InvariantCulture);
        private static double Number(string[] row,int column) => double.Parse(row[column],CultureInfo.InvariantCulture);
        [TestCase(3001004)] [TestCase(3001005)] [TestCase(4001344)] [TestCase(5001003)]
        public void SourceSkillScalingCountsAndRangeMatchCompiledCpp(int id)
        {
            foreach(var row in Reference("HeavenRangedSkillStats").Where(r=>Int(r,0)==id))
            {
                var data=Assets.SkillData.GetSkill(id).Levels[Int(row,1)];
                var stats=SourceSkillRules.ApplyAttackStats(new PhysicalAttackStats(Number(row,3),Number(row,4),19,12,.05f),data);
                Assert.That(stats.Minimum,Is.EqualTo(Number(row,11)).Within(1e-9));Assert.That(stats.Maximum,Is.EqualTo(Number(row,12)).Within(1e-9));
                Assert.That(Int(row,2)==1?data.BulletCount:data.AttackCount,Is.EqualTo(Int(row,13)));
                var bounds=new AttackBounds(Int(row,16),-50,Int(row,17),50).ScaleForward(data.Range/100f);
                Assert.That(bounds.Left,Is.EqualTo(Int(row,18)));Assert.That(bounds.Right,Is.EqualTo(Int(row,17)));
            }
        }
        [TestCase(3001004)] [TestCase(3001005)] [TestCase(4001344)] [TestCase(5001003)]
        public void RealMetadataCostBoundariesMatchCppWithinTheEnabledWeaponFamilies(int id)
        {
            // Cases0..9 use the real skill costs. Later CSV cases override metadata
            // for future zero-cost and independent-consumption skills, outside this port.
            foreach(var row in Reference("HeavenRangedSkillCosts").Where(r=>Int(r,0)==id&&Int(r,2)<=9))
            {
                int type=Int(row,5);int initialType=type==130?145:type;
                var p=Equipped(initialType,id,Int(row,11),out var skills,out _,out _,out _);
                if(type==130){p.TryUnequipItem(EquipSlot.Weapon,out _);p.Inventory.AddItem(1302000,1);Assert.That(p.TryEquipItem(1302000,out _),Is.True);}
                p.JobId=Int(row,4)==0?0:id/10000;skills.SetSkillLevel(id,0);skills.SetSkillLevel(id,Int(row,3));
                p.SetHPMP(Int(row,7),Int(row,9));
                bool expected=Int(row,13)==0 && SourceSkillRules.RangedWeaponMatches(id,type);
                var result=skills.UseSkill(id);Assert.That(result.Success,Is.EqualTo(expected),string.Join(",",row));
                if(!expected)Assert.That(p.CurrentMP,Is.EqualTo(Int(row,9)));
            }
        }
        public static int[] Speeds => Enumerable.Range(0,16).ToArray();
        [TestCaseSource(nameof(Speeds))]
        public void NamedAndOrdinarySkillDelaysMatchEveryCppSpeedAndLine(int speed)
        {
            var p=Equipped(149,5001003,2,out var skills,out _,out _,out _);
            int original=p.CurrentWeapon.AttackSpeed;
            try
            {
                p.CurrentWeapon.AttackSpeed=speed;Assert.That(skills.UseSkill(5001003).Success,Is.True);
                var ordinary=new WeaponProfile {AttackType=1,AttackSpeed=speed};
                ordinary.FrameDelays[CharacterState.Attack1]=new[]{170,160,120};
                ordinary.Afterimages[CharacterState.Attack1]=new WeaponAfterimage {FirstFrame=2};
                var motion=ordinary.CreateAttack(0,false);
                foreach(var row in Reference("HeavenRangedSkillDelays").Where(r=>Int(r,1)==speed))
                    Assert.That((row[0]=="doublefire"?p.BasicAttack:motion).HitDelayForLine(Int(row,2)),Is.EqualTo(Int(row,4)),string.Join(",",row));
            }
            finally {p.CurrentWeapon.AttackSpeed=original;}
        }
        [TestCase("doublefire")] [TestCase("handgun")]
        public void SourceBodyActionSignedDelaysAndMovesMatchCompiledCpp(string action)
        {
            var gun=Assets.ItemData.GetItem(1492000).Weapon;var skill=Assets.SkillData.GetSkill(5001003);
            var delays=action=="doublefire"?skill.ActionDelays:gun.ActionDelays;
            var moves=action=="doublefire"?skill.ActionMoves:gun.ActionMoves;
            foreach(var row in Reference("HeavenRangedSkillActions").Where(r=>r[0]==action))
            {
                int frame=Int(row,1);Assert.That(delays[frame],Is.EqualTo(Int(row,3)));
                Assert.That(moves[frame],Is.EqualTo(new Vector2(Int(row,6),Int(row,7))));
            }
        }
        [TestCase(3001004,14,1,260,3)] [TestCase(3001005,16,2,130,0)]
        [TestCase(4001344,16,2,150,0)] [TestCase(5001003,7,2,110,1)]
        public void RealSkillCostsCountsDamageAndProjectileArtwork(int id,int mp,int count,int damage,int frames)
        {
            var skill=Assets.SkillData.GetSkill(id);var level=skill.Levels[20];
            Assert.That(skill.Type,Is.EqualTo(SkillType.Attack));Assert.That(level.MpCost,Is.EqualTo(mp));
            Assert.That(level.BulletCount,Is.EqualTo(count));Assert.That(level.BulletConsume,Is.EqualTo(count));
            Assert.That(level.AttackCount,Is.EqualTo(1));Assert.That(level.Damage,Is.EqualTo(damage));
            Assert.That(level.Projectile.Frames.Length,Is.EqualTo(frames));
            foreach(var frame in level.Projectile.Frames)Assert.That(Assets.GetNode("skill",frame.Path).Value,Is.TypeOf<byte[]>());
        }
        [TestCase(1,10)] [TestCase(10,10)] [TestCase(15,10)] [TestCase(16,15)] [TestCase(25,20)] [TestCase(26,25)]
        public void HitArtworkSelectsItsCharacterLevelIndependentlyOfUseArtwork(int level,int folder)
        {
            foreach(int id in new[]{3001004,3001005,4001344,5001003})
            {
                var effect=SourceSkillRules.EffectsFor(Assets.SkillData.GetSkill(id),level);
                Assert.That(effect.Hit.Frames[0].Path,Does.Contain("/CharLevel/"+folder+"/hit/0/"));
                Assert.That(effect.Use.Frames.Length>0,Is.EqualTo(id==4001344||id==5001003));
                Assert.That(effect.HasTwoHandedHit,Is.EqualTo(id==3001005));
                foreach(var frame in effect.Hit.Frames.Concat(effect.Use.Frames).Concat(effect.TwoHandedHit.Frames))
                    Assert.That(Assets.GetNode("skill",frame.Path).Value,Is.TypeOf<byte[]>());
            }
        }
        [TestCase(145,3001004,1,"skill")] [TestCase(146,3001004,1,"skill")]
        [TestCase(145,3001005,2,"item")] [TestCase(146,3001005,2,"item")]
        [TestCase(147,4001344,2,"item")] [TestCase(149,5001003,2,"skill")]
        public void LastRoundsPayOnceAndEveryLineKeepsCapturedDamageUntilArrival(int type,int id,int count,string file)
        {
            var p=Equipped(type,id,count,out var skills,out var c,out var mobs,out var rolls);var target=Mob(3);mobs.Add(target);
            var data=Assets.SkillData.GetSkill(id).Levels[20];var stats=p.PhysicalAttackStats;
            var expected=new PhysicalAttackStats(stats.Minimum*data.DamageMultiplier,stats.Maximum*data.DamageMultiplier,stats.Accuracy,stats.Level,stats.CriticalChance);
            var expectedRolls=new Rolls();var damage=Enumerable.Range(0,count).Select(_=>expected.Roll(target.Template,expectedRolls).Damage).ToArray();
            var hits=new List<AttackHit>();c.AttackResolved+=(_,__,hit)=>hits.Add(hit);int mp=p.CurrentMP;
            var result=skills.UseSkill(id);Assert.That(result.Success,Is.True,result.ErrorMessage);Assert.That(result.AttackCount,Is.EqualTo(count));
            Assert.That(p.CurrentMP,Is.EqualTo(mp-data.MpCost));Assert.That(p.AmmunitionCount,Is.Zero);Assert.That(hits,Is.Empty);
            Assert.That(skills.UseSkill(id).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(mp-data.MpCost));
            p.WeaponAttack=900;target.Template.PhysicalDefense=999;
            for(int i=0;i<160&&!c.Projectiles.Any();i++)Step(c);
            Assert.That(c.Projectiles.All(b=>b.AssetFile==file),Is.True);Assert.That(c.Projectiles.Count(),Is.EqualTo(id==5001003?1:count));
            Step(c,300);Assert.That(hits.Select(h=>h.LineIndex),Is.EqualTo(Enumerable.Range(0,count)));
            Assert.That(hits.Select(h=>h.Damage),Is.EqualTo(damage));Assert.That(rolls.Draws,Is.EqualTo(3*count));
            Assert.That(hits[0].Critical,Is.True);if(count==2)Assert.That(hits[1].Critical,Is.False);
            Assert.That(c.Projectiles,Is.Empty);Assert.That(target.HP,Is.EqualTo(1000-damage.Sum()));
            if(id==3001005)Assert.That(target.SkillHitEffect.Sample(out _).Path,Does.Contain(type==146?"/hit/1/":"/hit/0/"));
        }
        [Test]
        public void GunDoubleFireLaunchesAtZeroAndSeventyFiveMillisecondsAndTravelCancelsTheSecond()
        {
            var p=Equipped(149,5001003,4,out var skills,out var c,out var mobs,out _);mobs.Add(Mob(3));
            var skill=Assets.SkillData.GetSkill(5001003);
            Assert.That(skill.ActionDelays,Is.EqualTo(new[]{90,360,100}));Assert.That(skill.ActionHitDelays,Is.EqualTo(new[]{0,90,450}));
            Assert.That(skills.UseSkill(5001003).Success,Is.True);Assert.That(c.Projectiles,Is.Empty);
            Assert.That(p.BasicAttack.HitDelayForLine(1),Is.EqualTo(75));Assert.That(p.BasicAttack.HitDelayForLine(3),Is.Zero);
            Step(c);Assert.That(c.Projectiles.Count(),Is.EqualTo(1));
            Step(c,8);Assert.That(c.Projectiles.Count(),Is.EqualTo(1));Step(c);Assert.That(c.Projectiles.Count(),Is.EqualTo(2));
            Assert.That(p.BasicAttack.Stance,Is.EqualTo(CharacterState.Attack1));Assert.That(p.BasicAttack.VisualOffset,Is.EqualTo(new Vector2(2,0)));
            Step(c,200);int hp=mobs[0].HP;Assert.That(skills.UseSkill(5001003).Success,Is.True);
            p.ResetMovementForMap();Step(c,200);Assert.That(c.Projectiles,Is.Empty);Assert.That(mobs[0].HP,Is.EqualTo(hp));Assert.That(p.AmmunitionCount,Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void GunRangeScalesTheSourceFourHundredPixelRectangleByThreeHundredEightyPercent(bool left)
        {
            var p=Equipped(149,5001003,2,out var skills,out var c,out var mobs,out _);
            if(left){p.MoveLeft(true);p.UpdatePhysics(.008f,FlatMap());p.MoveLeft(false);p.Position=new Vector2(0,Player.Height/2);}
            float sign=left?-1:1;var inside=Mob(sign*15.30f);var outside=Mob(sign*15.31f);var behind=Mob(-sign*.5f);
            mobs.AddRange(new[]{outside,behind,inside});Assert.That(skills.UseSkill(5001003).Success,Is.True);Step(c,500);
            Assert.That(inside.HP,Is.LessThan(1000));Assert.That(outside.HP,Is.EqualTo(1000));Assert.That(behind.HP,Is.EqualTo(1000));
        }
        [TestCase(145,3001004)] [TestCase(146,3001005)] [TestCase(147,4001344)] [TestCase(149,5001003)]
        public void EmptyCastPaysListedCostsAndCannotAcquireALateTarget(int type,int id)
        {
            var p=Equipped(type,id,4,out var skills,out var c,out var mobs,out _);int mp=p.CurrentMP;
            Assert.That(skills.UseSkill(id).Success,Is.True);var late=Mob(2);mobs.Add(late);Step(c,300);
            Assert.That(late.HP,Is.EqualTo(1000));Assert.That(c.Projectiles,Is.Empty);
            Assert.That(p.AmmunitionCount,Is.EqualTo(4-Assets.SkillData.GetSkill(id).Levels[20].BulletConsume));
            Assert.That(p.CurrentMP,Is.EqualTo(mp-Assets.SkillData.GetSkill(id).Levels[20].MpCost));
        }
        [TestCase(145,3001004)] [TestCase(146,3001005)] [TestCase(147,4001344)] [TestCase(149,5001003)]
        public void InvalidCastsDoNotSpendResourcesOrStartActions(int type,int id)
        {
            foreach(string reason in new[]{"mp","prone","weapon","job","ammo"})
            {
                var p=Equipped(type,id,4,out var skills,out var c,out _,out _);int ammo=AmmunitionRules.Prefix(type)*1000;
                switch(reason)
                {
                    case "mp":p.CurrentMP=0;break;
                    case "prone":p.Crouch(true);p.UpdatePhysics(.008f,FlatMap());break;
                    case "weapon":p.TryUnequipItem(EquipSlot.Weapon,out _);break;
                    case "job":p.JobId=0;break;
                    case "ammo":p.Inventory.RemoveItem(ammo,4);break;
                }
                int mp=p.CurrentMP,hp=p.CurrentHP,count=p.Inventory.GetItemCount(ammo);
                Assert.That(skills.UseSkill(id).Success,Is.False,reason);Assert.That(p.CurrentMP,Is.EqualTo(mp));Assert.That(p.CurrentHP,Is.EqualTo(hp));
                Assert.That(p.Inventory.GetItemCount(ammo),Is.EqualTo(count));Assert.That(p.IsBasicAttacking,Is.False);Assert.That(c.Projectiles,Is.Empty);
            }
        }
        [Test]
        public void ASecondStarStackCannotFundAnUnderfilledFirstStack()
        {
            var p=Equipped(147,4001344,1,out var skills,out _,out _,out _);p.Inventory.AddItem(2070001,100);
            int mp=p.CurrentMP;var result=skills.UseSkill(4001344);Assert.That(result.Success,Is.False);Assert.That(result.ErrorMessage,Does.Contain("active stack"));
            Assert.That(p.CurrentMP,Is.EqualTo(mp));Assert.That(p.Inventory.GetItemCount(2070000),Is.EqualTo(1));Assert.That(p.Inventory.GetItemCount(2070001),Is.EqualTo(100));
        }
    }
}
