using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Vec = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.GameData
{
    public class SkillFrameworkIntegrationTests
    {
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private SkillCatalog catalog;
        private readonly List<CustomSkillAsset> assets = new List<CustomSkillAsset>();
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => new MapData { MapId = id,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } };
        }
        private GameWorld world;
        private Player p;
        [SetUp] public void Setup()
        {
            catalog = (SkillCatalog)Assets.SkillData;
            world = new GameWorld(null, new Maps(), assetProvider: Assets); world.LoadMap(42); p = world.Player;
            Step(100); p.JobId = 200;
        }
        [TearDown] public void Cleanup()
        { foreach (var asset in assets) { catalog.RemoveCustom(asset.Id); Object.DestroyImmediate(asset); } assets.Clear(); }
        private void Step(int ticks) { for (int i = 0; i < ticks; i++) world.UpdatePhysics(.008f); }
        private CustomSkillAsset Make()
        {
            var a = ScriptableObject.CreateInstance<CustomSkillAsset>(); assets.Add(a);
            a.Id = 180000000 + assets.Count; a.DisplayName = "Field Focus"; a.Action = "";
            a.Ranks[0].MpCost = 4; a.Ranks[0].DurationMilliseconds = 80;
            a.Ranks[0].Buffs = new[] { new CustomSkillAsset.Buff { Type = BuffType.MagicAttack, Value = 7 } };
            return a;
        }
        private void Register(CustomSkillAsset a)
        { Assert.That(catalog.TryRegister(a, out var error), Is.True, error); }
        [TestCase("ArcBolt")] [TestCase("FieldFocus")]
        public void ShippedExampleAssetsDeserializeIntoValidDefinitions(string name)
        {
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<CustomSkillAsset>("Assets/SkillExamples/" + name + ".asset");
            Assert.That(source, Is.Not.Null);
            var a = Object.Instantiate(source); assets.Add(a); a.Id = 180000088;
            Register(a); Assert.That(Cast(a).Success, Is.True);
        }
        private SkillUseResult Cast(CustomSkillAsset a)
        { Assert.That(world.SkillManager.SetSkillLevel(a.Id, 1), Is.True); return world.SkillManager.UseSkill(a.Id); }

        [Test] public void CustomIdUsesExplicitJobAndInheritsOnlyWhenConfigured()
        {
            var a = Make(); Register(a);
            Assert.That(a.Id / 10000, Is.Not.EqualTo(a.JobId));
            Assert.That(world.SkillManager.GetAvailableSkills().ContainsKey(a.Id), Is.True);
            p.JobId = 212; Assert.That(world.SkillManager.CanLearnSkill(a.Id), Is.True);
            Assert.That(Cast(a).Success, Is.True);
            p.JobId = 100; int mp = p.CurrentMP;
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.False); Assert.That(p.CurrentMP, Is.EqualTo(mp));
            var only = Make(); only.InheritJob = false; Register(only); p.JobId = 212;
            Assert.That(world.SkillManager.GetAvailableSkills().ContainsKey(only.Id), Is.False);
        }
        [Test] public void ComposedRecoveryAndBuffPayOnceAndPlayerOwnsExpirationAndReplacement()
        {
            var a = Make(); a.CastEffects = new[] { SkillCastEffects.StatBuff, SkillCastEffects.Recovery };
            a.Ranks[0].HealHp = 12; a.Ranks[0].RestoreMp = 1; Register(a); p.TakeDamage(30);
            int mp = p.CurrentMP; var result = Cast(a);
            Assert.That(result.Success, Is.True, result.ErrorMessage); Assert.That(result.Damage, Is.Zero);
            Assert.That(p.CurrentHP, Is.EqualTo(82)); Assert.That(p.CurrentMP, Is.EqualTo(mp - 3));
            Assert.That(p.MagicAttack, Is.EqualTo(7)); world.SkillManager.Update(.2f);
            Assert.That(world.SkillManager.IsBuffActive(a.Id), Is.True, "The skill manager cannot tick the player's buff clock twice.");
            p.ApplyStatBuffs(-2002002, "Replacement", new Dictionary<BuffType, int> { [BuffType.MagicAttack] = 2 }, 16);
            Assert.That(world.SkillManager.IsBuffActive(a.Id), Is.False); Step(2); Assert.That(p.MagicAttack, Is.Zero);
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.True); Step(10);
            Assert.That(p.ActiveBuffs, Is.Empty); Assert.That(world.SkillManager.IsBuffActive(a.Id), Is.False);
        }
        [Test] public void InvalidSecondEffectFailsBeforeAnyCostCooldownAnimationOrFirstEffect()
        {
            var a = Make(); Register(a); var info = catalog.GetSkill(a.Id);
            info.Behavior.CastEffects = new[] { SkillCastEffects.StatBuff, "unregistered-effect" };
            var result = Cast(a);
            Assert.That(result.Success, Is.False); Assert.That(result.ErrorMessage, Does.Contain("unregistered-effect"));
            Assert.That(p.CurrentMP, Is.EqualTo(50)); Assert.That(p.ActiveBuffs, Is.Empty); Assert.That(p.IsBasicAttacking, Is.False);
            info.Behavior.CastEffects = new[] { SkillCastEffects.StatBuff };
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.True);
        }
        [TestCase("missing-effect")] [TestCase("duplicate-effect")] [TestCase("missing-action")]
        [TestCase("negative-cost")] [TestCase("bad-area")] [TestCase("invalid-passive")] [TestCase("invalid-buff")]
        public void MalformedAssetsNeverEnterTheCatalog(string fault)
        {
            var a = Make();
            switch (fault)
            {
                case "missing-effect": a.CastEffects = new[] { "summon-not-registered" }; break;
                case "duplicate-effect": a.CastEffects = new[] { SkillCastEffects.StatBuff, SkillCastEffects.StatBuff }; break;
                case "missing-action": a.Action = "not-an-original-action"; break;
                case "negative-cost": a.Ranks[0].MpCost = -1; break;
                case "bad-area": a.Execution = SkillExecution.Attack; a.AttackFamily = SkillAttackFamily.Melee; a.Ranks[0].Area = UnityEngine.Vector4.zero; break;
                case "invalid-passive": a.Ranks[0].PassiveMastery = float.NaN; break;
                case "invalid-buff": a.Ranks[0].Buffs[0].Type = BuffType.PowerGuard; break;
            }
            Assert.That(catalog.TryRegister(a, out var error), Is.False); Assert.That(error, Is.Not.Empty);
            Assert.That(catalog.GetSkill(a.Id), Is.Null);
        }
        [Test] public void DuplicateIdsCannotReplaceDefinitionsAndCompiledDataDoesNotAliasAuthoringArrays()
        {
            var a = Make(); Register(a); var info = catalog.GetSkill(a.Id);
            a.Ranks[0].Buffs[0].Value = 999; a.CastEffects[0] = "changed";
            Assert.That(info.Levels[1].Buffs[BuffType.MagicAttack], Is.EqualTo(7));
            Assert.That(catalog.TryRegister(a, out _), Is.False); Assert.That(catalog.GetSkill(a.Id), Is.SameAs(info));
            var reserved = Make(); reserved.Id = 2001002;
            Assert.That(catalog.TryRegister(reserved, out _), Is.False); Assert.That(catalog.GetSkill(2001002).Name, Is.EqualTo("Magic Guard"));
        }
        [Test] public void PassiveDefinitionAppliesByCapabilitiesAndDisappearsOnJobOrWeaponChange()
        {
            var a = Make(); a.Execution = SkillExecution.Passive; a.CastEffects = Array.Empty<string>();
            a.Ranks[0].PassiveMagicAttack = 8; a.Ranks[0].PassiveAccuracy = 6; Register(a);
            Assert.That(world.SkillManager.SetSkillLevel(a.Id, 1), Is.True); Assert.That(p.MagicAttack, Is.EqualTo(8));
            p.JobId = 100; Assert.That(p.MagicAttack, Is.Zero); p.JobId = 200; Assert.That(p.MagicAttack, Is.EqualTo(8));
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.False);
            Assert.That(world.SkillManager.SetSkillLevel(a.Id, 0), Is.True); Assert.That(p.MagicAttack, Is.Zero);
        }
        [Test] public void UnitySpritesAndOriginalPresentationUseTheSameCompiledFrameReferences()
        {
            var a = Make(); var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new UnityEngine.Vector2(.5f, .5f), 100);
            try
            {
                a.SourcePresentationSkillId = 2001004; a.Icon = sprite;
                a.Hit.Frames = new[] { new CustomSkillAsset.Frame { Sprite = sprite, Milliseconds = 40 } };
                Register(a); var info = catalog.GetSkill(a.Id);
                Assert.That(SkillSprites.Icon(info, catalog), Is.SameAs(sprite));
                var set = SourceSkillRules.EffectsFor(info, 1);
                Assert.That(SkillSprites.Frame(set.Hit.AssetFile, set.Hit.Frames[0].Path, catalog), Is.SameAs(sprite));
                Assert.That(info.Levels[1].Projectile.Frames.Length, Is.GreaterThan(0));
                Assert.That(info.Levels[1].Projectile.AssetFile, Is.EqualTo("skill"));
                Assert.That(Assets.SkillData.GetSkill(2001004).Effects.Values.First().Hit.AssetFile, Is.EqualTo("skill"));
            }
            finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
        private sealed class Rolls : System.Random { public override int Next() => 0; public override double NextDouble() => .5; }
        private sealed class ExtraRecovery : ISkillCastEffect
        {
            public string Key { get; } = "test-recovery-" + Guid.NewGuid().ToString("N");
            public int Applications;
            public bool Validate(SkillInfo.LevelData level, out string error)
            { error = "Expected a positive amount."; return level.EffectParameters.TryGetValue("amount", out float value) && value > 0; }
            public void Apply(Player caster, SkillInfo skill, SkillInfo.LevelData level)
            { Applications++; caster.Heal((int)level.EffectParameters["amount"]); }
        }
        [Test] public void RegisteredMechanicUsesAuthoredParametersThroughTheNormalCastPipeline()
        {
            var handler = new ExtraRecovery(); catalog.CastEffects.Register(handler);
            var a = Make(); a.CastEffects = new[] { SkillCastEffects.StatBuff, handler.Key };
            a.Ranks[0].EffectParameters = new[] { new CustomSkillAsset.Parameter { Key = "amount", Value = 9 } };
            Register(a); p.TakeDamage(20); var result = Cast(a);
            Assert.That(result.Success, Is.True, result.ErrorMessage); Assert.That(handler.Applications, Is.EqualTo(1));
            Assert.That(p.CurrentHP, Is.EqualTo(89)); Assert.That(p.MagicAttack, Is.EqualTo(7)); Assert.That(p.CurrentMP, Is.EqualTo(46));
        }
        [Test] public void CustomProjectilePaysAtCastAndDeliversConfiguredHitsOnlyAtImpact()
        {
            var a = Make(); a.Execution = SkillExecution.Attack; a.AttackFamily = SkillAttackFamily.Magic;
            a.DamagePolicy = SkillDamagePolicy.FixedMagic; a.CastEffects = Array.Empty<string>();
            a.Action = "alert2"; a.SourcePresentationSkillId = 2001004; a.Ranks[0].Damage = 18; a.Ranks[0].AttackCount = 2;
            Register(a); p.Position = new Vec(0, Player.Height / 2);
            var mob = new Monster(new MonsterTemplate { MaxHP = 1000, Level = 1, ContactAnimations = new Dictionary<string, MobContactAnimation> {
                ["stand"] = new MobContactAnimation(new[] { new MobContactFrame { Left = -10, Top = -20, Right = 10, Bottom = 0, HeadY = -14, DelayMilliseconds = 100 } }, false)
            } }, new Vec(1.5f, 0));
            var combat = new Combat(new Rolls(), new Rolls()); var targets = new List<Monster> { mob };
            typeof(SkillManager).GetField("StartAttackSkill", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world.SkillManager,
                new Func<SkillInfo, SkillInfo.LevelData, bool>((s, l) => combat.PerformSkillAttack(p, targets, s, l)));
            var result = Cast(a); Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(p.CurrentMP, Is.EqualTo(46)); Assert.That(result.Damage, Is.Zero); Assert.That(mob.HP, Is.EqualTo(1000));
            for (int i = 0; i < 400; i++) combat.Update(.008f);
            Assert.That(mob.HP, Is.EqualTo(964)); Assert.That(p.CurrentMP, Is.EqualTo(46));
        }
        [Test] public void CustomBookLevelsAndCooldownSurviveSpendingAndProgressRoundTrip()
        {
            p.JobId = 0;
            while (p.Level < 8) p.AddExperience(p.ExperienceToNextLevel - p.Experience);
            Assert.That(world.TryAdvanceFirstJob(200, out _), Is.True);
            var a = Make(); a.Ranks = new[] { a.Ranks[0], new CustomSkillAsset.Rank { DurationMilliseconds = 80,
                Buffs = new[] { new CustomSkillAsset.Buff { Type = BuffType.MagicAttack, Value = 9 } } } };
            a.Ranks[0].CooldownMilliseconds = 2000; Register(a);
            Assert.That(world.SkillManager.TrySpendSkillPoint(a.Id, out var error), Is.True, error);
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.True);
            p.AddExperience(p.ExperienceToNextLevel - p.Experience);
            Assert.That(world.SkillManager.TrySpendSkillPoint(a.Id, out error), Is.True, error);
            Assert.That(world.SkillManager.UseSkill(a.Id).Success, Is.False, "Spending SP cannot erase a live cooldown.");
            var save = world.CaptureProgress();
            Assert.That(world.TryRestoreProgress(save, out error), Is.True, error);
            Assert.That(world.SkillManager.GetSkillLevel(a.Id), Is.EqualTo(2)); Assert.That(p.ActiveBuffs, Is.Empty);
        }
    }
}
