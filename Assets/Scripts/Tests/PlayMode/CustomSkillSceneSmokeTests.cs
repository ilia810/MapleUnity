using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class CustomSkillSceneSmokeTests
    {
        private CustomSkillAsset asset;
        private SkillCatalog catalog;
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string text, string stack, LogType type) { if (type != LogType.Log) errors.Add(type + ": " + text); }
        [TearDown] public void Cleanup()
        {
            Application.logMessageReceived -= Log;
            if (asset != null) { catalog?.RemoveCustom(asset.Id); Object.DestroyImmediate(asset); }
            Assert.That(errors, Is.Empty);
        }
        [UnityTest] public IEnumerator AuthoredSkillUsesNormalBookSpQuickslotSpritesAndSaveRestoration()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var p = world.Player;
            for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
            while (p.Level < 8) p.AddExperience(p.ExperienceToNextLevel - p.Experience);
            Assert.That(world.TryAdvanceFirstJob(200, out _), Is.True);
            catalog = (SkillCatalog)NXDataManagerSingleton.Instance.DataManager.SkillData;
            asset = ScriptableObject.CreateInstance<CustomSkillAsset>(); asset.Id = 180000099; asset.DisplayName = "Field Focus";
            asset.Description = "An authored skill using the normal skill book.";
            asset.Icon = SkillSprites.Icon(catalog.GetSkill(2001002));
            asset.CastEffects = new[] { SkillCastEffects.StatBuff, SkillCastEffects.Recovery };
            asset.Ranks[0].MpCost = 4; asset.Ranks[0].HealHp = 12; asset.Ranks[0].DurationMilliseconds = 10000;
            asset.Ranks[0].Buffs = new[] { new CustomSkillAsset.Buff { Type = BuffType.MagicAttack, Value = 7 } };
            var frame = catalog.GetSkill(2001002).Effects.Values.First().Use.Frames[0];
            var castSprite = SkillSprites.Frame("skill", frame.Path);
            asset.Use.Frames = new[] { new CustomSkillAsset.Frame { Sprite = castSprite, Milliseconds = 1000 } };
            Assert.That(catalog.TryRegister(asset, out var error), Is.True, error);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");
            yield return InventorySceneSmokeTests.Click("SkillRow_" + asset.Id);
            yield return InventorySceneSmokeTests.Click("SpendSkillPoint_" + asset.Id); yield return null;
            Assert.That(world.SkillManager.GetSkillLevel(asset.Id), Is.EqualTo(1)); Assert.That(p.SkillPoints, Is.Zero);
            yield return InventorySceneSmokeTests.Click("AssignSkill_0");
            Object.FindFirstObjectByType<SkillMenu>().Show(false); yield return null;
            Assert.That(GameObject.Find("SkillSlot_0").transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(asset.Icon));
            p.TakeDamage(20); int hp = p.CurrentHP, mp = p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");
            for (int i = 0; i < 10; i++) world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(p.CurrentHP, Is.EqualTo(hp + 12)); Assert.That(p.CurrentMP, Is.EqualTo(mp - 4));
            Assert.That(GameObject.Find("Buff_" + asset.Id).transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(asset.Icon));
            Assert.That(GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>().sprite, Is.SameAs(castSprite));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-custom-skill-cast"); yield return null; yield return null;
            var save = world.CaptureProgress();
            Assert.That(world.TryRestoreProgress(save, out error), Is.True, error); yield return null; yield return null;
            Assert.That(world.SkillManager.GetSkillLevel(asset.Id), Is.EqualTo(1)); Assert.That(p.ActiveBuffs, Is.Empty);
            Assert.That(GameObject.Find("SkillSlot_0").transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(asset.Icon));
            yield return InventorySceneSmokeTests.Click("SkillsToggle");
            yield return InventorySceneSmokeTests.Click("SkillRow_" + asset.Id);
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text, Does.Contain("Magic attack +7"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-custom-skill-book");
        }
        private sealed class Rolls : System.Random { public override int Next() => 0; public override double NextDouble() => .5; }
        [UnityTest] public IEnumerator AuthoredProjectileAndImpactSpritesRenderThroughTheSharedCombatViews()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var p = world.Player;
            for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
            p.JobId = 200;
            var combat = (Combat)typeof(GameWorld).GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
            typeof(Combat).GetField("damageRandom", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, new Rolls());
            catalog = (SkillCatalog)NXDataManagerSingleton.Instance.DataManager.SkillData;
            var source = catalog.GetSkill(2001004);
            var bulletSprite = SkillSprites.Frame("skill", source.Levels[1].Projectile.Frames[0].Path);
            var hitSprite = SkillSprites.Frame("skill", source.Effects.Values.First().Hit.Frames[0].Path);
            Assert.That(bulletSprite, Is.Not.Null); Assert.That(hitSprite, Is.Not.Null);
            asset = ScriptableObject.CreateInstance<CustomSkillAsset>(); asset.Id = 180000099; asset.DisplayName = "Authored Arc";
            asset.Execution = SkillExecution.Attack; asset.AttackFamily = SkillAttackFamily.Magic;
            asset.DamagePolicy = SkillDamagePolicy.FixedMagic; asset.CastEffects = System.Array.Empty<string>();
            asset.Ranks[0].MpCost = 4; asset.Ranks[0].Damage = 18; asset.Ranks[0].Area = new Vector4(-400, -90, 0, 20);
            asset.Projectile.Frames = new[] { new CustomSkillAsset.Frame { Sprite = bulletSprite, Milliseconds = 1000 } };
            asset.Hit.Frames = new[] { new CustomSkillAsset.Frame { Sprite = hitSprite, Milliseconds = 1000 } };
            Assert.That(catalog.TryRegister(asset, out var error), Is.True, error);
            world.SpawnMonsterForTesting(130101, new MapleClient.GameLogic.Vector2(p.Position.X + 3.2f, p.Position.Y - Player.Height / 2));
            var target = world.Monsters.Single(); target.Template.BodyAttack = false;
            target.SetMovementPattern(MovementPattern.Stationary);
            Assert.That(world.SkillManager.SetSkillLevel(asset.Id, 1), Is.True);
            var result = world.SkillManager.UseSkill(asset.Id); Assert.That(result.Success, Is.True, result.ErrorMessage);
            int hp = target.HP;
            for (int i = 0; i < 160 && !world.Projectiles.Any(); i++) world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(world.Projectiles.Single().AssetFile, Is.EqualTo("unity"));
            Assert.That(GameObject.Find("SkillProjectile").GetComponent<SpriteRenderer>().sprite, Is.SameAs(bulletSprite));
            Assert.That(target.HP, Is.EqualTo(hp));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-custom-projectile-flight");
            for (int i = 0; i < 400 && target.HP == hp; i++) world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(target.HP, Is.LessThan(hp));
            Assert.That(Object.FindFirstObjectByType<MonsterView>().transform.Find("SkillHitEffect").GetComponent<SpriteRenderer>().sprite, Is.SameAs(hitSprite));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-custom-projectile-impact");
        }
    }
}
