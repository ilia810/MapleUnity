using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class RangedCombatSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message,string stack,LogType type) { if(type!=LogType.Log)diagnostics.Add(type+": "+message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics,Is.Empty); }
        private sealed class Rolls : System.Random { public override int Next()=>0;public override double NextDouble()=>.5; }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed {get;set;} public bool IsRightPressed {get;set;} public bool IsJumpPressed {get;set;}
            public bool IsAttackPressed {get;set;} public bool IsUpPressed {get;set;} public bool IsDownPressed {get;set;}
        }
        private static void Step(GameWorld w,int ticks=1) { for(int i=0;i<ticks;i++){w.ProcessInput();w.UpdatePhysics(.008f);} }
        [UnityTest]
        public IEnumerator FourRangedKitsEquipFireSpendAmmoAndKeepSourceArtworkAcrossTravel()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var w=manager.World;var p=w.Player;var input=new Input();
            typeof(GameWorld).GetField("inputProvider",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(w,input);
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(w);
            foreach(string field in new[]{"attackRandom","damageRandom"})typeof(Combat).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(combat,new Rolls());
            Step(w,100);w.RequestPracticeSupplies(out _);foreach(int id in new[]{1040002,1060002,1072001})Assert.That(w.UseInventoryItem(id,out _),Is.True);
            p.AddExperience(3);var inventory=Object.FindFirstObjectByType<InventoryView>();var hits=new List<AttackHit>();w.AttackResolved+=(_,hit)=>hits.Add(hit);
            w.SpawnMonsterForTesting(130101,new LogicVector(p.Position.X+2.9f,p.Position.Y-Player.Height/2));
            var target=w.Monsters.Single();target.SetMovementPattern(MovementPattern.Stationary);target.Template.BodyAttack=false;
            foreach(int type in new[]{145,146,147,149})
            {
                inventory.Show(true);yield return null;yield return InventorySceneSmokeTests.OpenPractice();yield return null;
                if(type==145)yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-practice-small",640,480);
                yield return InventorySceneSmokeTests.Click("RangedPreset_"+type);yield return null;
                int weapon=GameWorld.RangedPracticeWeapon(type),ammo=AmmunitionRules.Prefix(type)*1000;
                Assert.That(p.Inventory.GetItemCount(ammo),Is.EqualTo(100));Assert.That(p.Experience,Is.EqualTo(3));
                Assert.That(p.Inventory.GetItemCount(weapon),Is.EqualTo(1));
                yield return InventorySceneSmokeTests.Click("ItemRow_"+weapon);
                yield return InventorySceneSmokeTests.Click("ItemAction");yield return null;
                Assert.That(p.EquippedWeaponType,Is.EqualTo(type));Assert.That(p.HasUsableAmmunition,Is.True);
                inventory.Show(false);yield return null;yield return null;
                var visual=GameObject.Find("Player").transform.Find("VisualRoot");
                var weaponSprites=visual.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sprite!=null && s.sprite.name.StartsWith("Weapon/"+weapon.ToString("D8"))).ToArray();
                Assert.That(weaponSprites,Is.Not.Empty,"Standing weapon "+type);
                // Keep one round to exercise the final-shot transition without firing 100 times.
                p.Inventory.RemoveItem(ammo,99);int hp=target.HP;hits.Clear();int mana=p.CurrentMP;
                input.IsAttackPressed=true;Step(w);input.IsAttackPressed=false;
                Assert.That(p.AmmunitionCount,Is.Zero);Assert.That(p.IsBasicAttacking,Is.True);Assert.That(target.HP,Is.EqualTo(hp));
                for(int i=0;i<160&&!w.Projectiles.Any();i++)Step(w);
                Step(w,3);yield return null;yield return null;
                Assert.That(w.Projectiles.Count(),Is.EqualTo(1));Assert.That(target.HP,Is.EqualTo(hp));
                var bullet=w.Projectiles.Single();var bulletSprite=GameObject.Find("SkillProjectile").GetComponent<SpriteRenderer>();
                Assert.That(bullet.AssetFile,Is.EqualTo("item"));Assert.That(bulletSprite.sprite,Is.Not.Null);
                Assert.That(bulletSprite.sprite.name,Does.Match(@"/bullet(?:/[01])?$"));
                Assert.That(visual.Find("Body").GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
                weaponSprites=visual.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sprite!=null && s.sprite.name.StartsWith("Weapon/"+weapon.ToString("D8"))).ToArray();
                Assert.That(weaponSprites,Is.Not.Empty,"Firing weapon "+type);
                if(type==149) {Assert.That(p.BasicAttack.Stance,Is.EqualTo(CharacterState.Attack1));Assert.That(visual.localPosition.x,Is.EqualTo(.02f).Within(.00001));}
                Assert.That(visual.Find("WeaponAfterimage").position.x,Is.EqualTo(visual.parent.position.x).Within(.00001));
                var pos=bullet.Position;yield return new WaitForSeconds(.10f);Assert.That(bullet.Position,Is.EqualTo(pos));
                Step(w,20);yield return null;yield return null;
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-"+type+"-flight");
                Step(w,200);yield return null;yield return null;
                Assert.That(hits.Count,Is.EqualTo(1));Assert.That(target.HP,Is.LessThan(hp));Assert.That(p.CurrentMP,Is.EqualTo(mana));
                Assert.That(p.IsBasicAttacking,Is.False);Assert.That(w.Projectiles,Is.Empty);
                Assert.That(GameObject.Find("StatDetailHint").GetComponent<Text>().text,Does.Contain("Out of ammunition"));
                input.IsAttackPressed=true;Step(w);input.IsAttackPressed=false;Assert.That(p.IsBasicAttacking,Is.False);
                Assert.That(w.RequestRangedPractice(type,out _),Is.True);Assert.That(p.Inventory.GetItemCount(ammo),Is.Zero,"No duplicate practice refill");
                p.Inventory.AddItem(ammo,2);
            }
            input.IsLeftPressed=true;Step(w,2);input.IsLeftPressed=false;Step(w);
            Assert.That(p.FacingRight,Is.False);
            input.IsAttackPressed=true;Step(w);input.IsAttackPressed=false;
            for(int i=0;i<150&&!w.Projectiles.Any();i++)Step(w);
            Step(w,3);yield return null;yield return null;
            Assert.That(w.Projectiles.Single().FacingRight,Is.False);
            Assert.That(GameObject.Find("SkillProjectile").GetComponent<SpriteRenderer>().flipX,Is.False);
            Assert.That(GameObject.Find("Player").transform.Find("VisualRoot").localPosition.x,Is.EqualTo(.02f).Within(.00001));
            Assert.That(GameObject.Find("Player").transform.Find("VisualRoot/WeaponAfterimage").position.x,
                Is.EqualTo(GameObject.Find("Player").transform.position.x).Within(.00001));
            Step(w,20);yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-gun-left");
            w.LoadMap(100000001);yield return null;yield return null;
            Assert.That(w.Projectiles,Is.Empty);Assert.That(GameObject.Find("SkillProjectile"),Is.Null);
            Assert.That(p.EquippedWeaponType,Is.EqualTo(149));Assert.That(p.AmmunitionCount,Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-after-travel");
        }
    }
}
