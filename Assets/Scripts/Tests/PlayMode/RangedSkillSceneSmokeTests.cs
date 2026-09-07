using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Skills;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using LogicVector=MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class RangedSkillSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void WatchLogs(){diagnostics.Clear();Application.logMessageReceived+=OnLog;}
        private void OnLog(string message,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+message);}
        [TearDown] public void CheckLogs(){Application.logMessageReceived-=OnLog;Assert.That(diagnostics,Is.Empty);}
        private sealed class Rolls:System.Random {public override int Next()=>0;public override double NextDouble()=>.5;}
        private static void Step(GameWorld w,int ticks=1){for(int i=0;i<ticks;i++){w.ProcessInput();w.UpdatePhysics(.008f);}}
        [UnityTest]
        public IEnumerator FirstJobRangedSkillsEquipCastShowTheirRealEffectsAndCancelAcrossTravel()
        {
            var setups=new[]{new[]{145,3001004,0},new[]{145,3001005,1},new[]{146,3001005,1},new[]{147,4001344,0},new[]{149,5001003,0}};
            foreach(var setup in setups)
            {
                int type=setup[0],id=setup[1];
                yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
                var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var w=manager.World;var p=w.Player;
                var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(w);
                foreach(string field in new[]{"attackRandom","damageRandom"})typeof(Combat).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(combat,new Rolls());
                Step(w,100);w.RequestPracticeSupplies(out _);foreach(int clothing in new[]{1040002,1060002,1072001})Assert.That(w.UseInventoryItem(clothing,out _),Is.True);
                long exp=p.Experience;int potions=p.Inventory.GetItemCount(2000003);var bag=Object.FindFirstObjectByType<InventoryView>();bag.Show(true);yield return null;
                yield return InventorySceneSmokeTests.OpenPractice();yield return null;yield return InventorySceneSmokeTests.Click("RangedPreset_"+type);yield return null;
                Assert.That(w.SkillManager.GetSkillLevel(id),Is.EqualTo(20));Assert.That(p.Inventory.GetItemCount(2000003),Is.EqualTo(potions+10));Assert.That(p.Experience,Is.EqualTo(exp));
                yield return InventorySceneSmokeTests.Click("ItemRow_"+GameWorld.RangedPracticeWeapon(type));
                yield return InventorySceneSmokeTests.Click("ItemAction");yield return null;bag.Show(false);yield return null;
                Assert.That(p.EquippedWeaponType,Is.EqualTo(type));
                var skill=w.SkillManager.LearnedSkills.Single(s=>s.SkillId==id);var data=skill.GetCurrentLevelData();
                if(type==149)
                {
                    yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return null;
                    yield return InventorySceneSmokeTests.Click("SkillRow_"+id);yield return null;
                    Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("MP 7 / Ammo 2"));
                    yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-skill-book-small",640,480);
                    yield return InventorySceneSmokeTests.Click("CloseSkills");yield return null;
                }
                w.SpawnMonsterForTesting(130101,new LogicVector(p.Position.X+3.2f,p.Position.Y-Player.Height/2));
                var target=w.Monsters.Single();target.Template.BodyAttack=false;target.SetMovementPattern(MovementPattern.Stationary);
                var hits=new List<AttackHit>();w.AttackResolved+=(_,hit)=>hits.Add(hit);int hp=target.HP,mp=p.CurrentMP;
                yield return InventorySceneSmokeTests.Click("SkillSlot_"+setup[2]);yield return null;
                Assert.That(p.IsBasicAttacking,Is.True);Assert.That(p.CurrentMP,Is.EqualTo(mp-data.MpCost));Assert.That(p.AmmunitionCount,Is.EqualTo(100-data.BulletConsume));Assert.That(hits,Is.Empty);
                if(type==149){Assert.That(w.Projectiles,Is.Empty);Step(w);Assert.That(w.Projectiles.Count(),Is.EqualTo(1));Step(w,9);}
                else {for(int i=0;i<160&&!w.Projectiles.Any();i++)Step(w);}
                Step(w,12);yield return null;yield return null;
                Assert.That(w.Projectiles.Count(),Is.EqualTo(data.BulletCount));Assert.That(target.HP,Is.EqualTo(hp));
                var bullets=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.name=="SkillProjectile").ToArray();
                Assert.That(bullets.Length,Is.EqualTo(data.BulletCount));Assert.That(bullets.All(s=>s.sprite!=null),Is.True);
                Assert.That(bullets.All(s=>s.sprite.name.Contains(id==3001004||id==5001003?"/ball/":"/bullet")),Is.True);
                if(type==149)Assert.That(Mathf.Abs(bullets[0].transform.position.x-bullets[1].transform.position.x),Is.GreaterThan(.2f));
                if(type==147||type==149)Assert.That(GameObject.Find("Player").transform.Find("VisualRoot/SkillUseEffect").GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
                var positions=w.Projectiles.Select(b=>b.Position).ToArray();yield return new WaitForSeconds(.10f);
                Assert.That(w.Projectiles.Select(b=>b.Position),Is.EqualTo(positions));
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-skill-"+type+"-"+id+"-flight");
                for(int i=0;i<300&&hits.Count<data.BulletCount;i++)Step(w);yield return null;yield return null;
                Assert.That(hits.Count,Is.EqualTo(data.BulletCount));Assert.That(hits.Select(h=>h.LineIndex),Is.EqualTo(Enumerable.Range(0,data.BulletCount)));
                Assert.That(target.HP,Is.LessThan(hp));Assert.That(w.Projectiles,Is.Empty);
                var hitSprite=Object.FindFirstObjectByType<MonsterView>().transform.Find("SkillHitEffect").GetComponent<SpriteRenderer>();
                Assert.That(hitSprite.sprite,Is.Not.Null);Assert.That(hitSprite.sprite.name,Does.Contain("/CharLevel/10/hit/"+(type==146?1:0)+"/"));
                var numbers=Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.name=="MonsterDamage").OrderBy(t=>t.rectTransform.anchoredPosition.y).ToArray();
                Assert.That(numbers.Length,Is.EqualTo(data.BulletCount));
                if(numbers.Length==2)Assert.That(numbers[1].rectTransform.anchoredPosition.y-numbers[0].rectTransform.anchoredPosition.y,Is.GreaterThan(20));
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-skill-"+type+"-"+id+"-impact");
                Step(w,150);int ammo=p.AmmunitionCount;Assert.That(w.RequestRangedPractice(type,out _),Is.True);Assert.That(p.AmmunitionCount,Is.EqualTo(ammo));Assert.That(p.Inventory.GetItemCount(2000003),Is.EqualTo(potions+10));
                if(type==149)
                {
                    yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(w);Assert.That(w.Projectiles.Count(),Is.EqualTo(1));
                    w.LoadMap(100000001);yield return null;yield return null;
                    Assert.That(w.Projectiles,Is.Empty);Assert.That(GameObject.Find("SkillProjectile"),Is.Null);Assert.That(p.AmmunitionCount,Is.EqualTo(96));Assert.That(w.SkillManager.GetSkillLevel(id),Is.EqualTo(20));
                    yield return PlayerCombatSceneSmokeTests.CaptureScreen("-ranged-skill-after-travel");
                }
            }
        }
    }
}
