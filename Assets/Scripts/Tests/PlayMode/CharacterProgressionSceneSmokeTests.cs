using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
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
    public class CharacterProgressionSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        private string savePath;
        [SetUp] public void Watch()
        {
            diagnostics.Clear();Application.logMessageReceived+=Log;
            savePath=Path.Combine(Application.temporaryCachePath,"progression-"+Guid.NewGuid().ToString("N")+".json");
        }
        private void Log(string message,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+message);}
        [TearDown] public void Cleanup()
        {
            Application.logMessageReceived-=Log;
            foreach(string path in new[]{savePath,savePath+".bak",savePath+".tmp"})if(File.Exists(path))File.Delete(path);
            Assert.That(diagnostics,Is.Empty);
        }
        private static void Step(GameWorld world,int ticks=100)
        {for(int i=0;i<ticks;i++){world.ProcessInput();world.UpdatePhysics(.008f);}}
        [UnityTest] public IEnumerator ACombatLevelUpUnlocksApAFirstJobAndSpendableSkillsThatSurviveSaving()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var player=world.Player;Step(world);
            while(player.Level<7)player.AddExperience(player.ExperienceToNextLevel);
            player.AddExperience(player.ExperienceToNextLevel-4);
            Assert.That(player.Level,Is.EqualTo(7));Assert.That(player.AbilityPoints,Is.EqualTo(30));
            world.SpawnMonsterForTesting(100101,new LogicVector(player.Position.X+.5f,player.Position.Y-Player.Height/2));
            var monster=world.Monsters.Single();monster.Template.BodyAttack=false;monster.SetMovementPattern(MovementPattern.Stationary);
            player.SetBaseDamage(100000); // Shorten one real combat kill; rewards still use the world's normal EXP path.
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(world);
            combat.PerformBasicAttack(player,world.Monsters.ToList(),1);Step(world);player.ClearPracticeDamageOverride();
            Assert.That(monster.IsDead,Is.True);Assert.That(player.Level,Is.EqualTo(8));Assert.That(player.AbilityPoints,Is.EqualTo(35));
            yield return InventorySceneSmokeTests.Click("StatsToggle");yield return InventorySceneSmokeTests.Click("OpenCharacterProgression");
            yield return null;yield return null;
            Assert.That(GameObject.Find("ProgressionReadout").GetComponent<Text>().text,Does.Contain("AP: 35"));
            Assert.That(GameObject.Find("AdvanceJob_100").GetComponent<Button>().interactable,Is.False);
            yield return InventorySceneSmokeTests.Click("SpendAP_INT");yield return InventorySceneSmokeTests.Click("SpendAP_INT");
            Assert.That(player.BaseAttribute(PrimaryAttribute.INT),Is.EqualTo(17));Assert.That(player.AbilityPoints,Is.EqualTo(33));
            yield return InventorySceneSmokeTests.Click("AdvanceJob_200");
            Assert.That(player.JobId,Is.EqualTo(200));Assert.That(player.SkillPoints,Is.EqualTo(1));Assert.That(player.Inventory.GetItemCount(1372005),Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-character-progression",640,480,()=>
            {
                var text=GameObject.Find("ProgressionMessage").GetComponent<Text>();
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.1f));
            });
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("SkillTier_1");
            yield return InventorySceneSmokeTests.Click("SkillRow_2001005");yield return null;yield return null;
            Assert.That(GameObject.Find("SpendSkillPoint_2001005").GetComponent<Button>().interactable,Is.False);
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Requires"));
            yield return InventorySceneSmokeTests.Click("SkillRow_2001004");yield return InventorySceneSmokeTests.Click("SpendSkillPoint");
            yield return InventorySceneSmokeTests.Click("AssignSkill_0");Assert.That(player.SkillPoints,Is.Zero);
            player.AddExperience(player.ExperienceToNextLevel);Assert.That(player.SkillPoints,Is.EqualTo(3));
            yield return InventorySceneSmokeTests.Click("SkillRow_2001005");yield return InventorySceneSmokeTests.Click("SpendSkillPoint");
            yield return InventorySceneSmokeTests.Click("AssignSkill_1");
            yield return InventorySceneSmokeTests.Click("SkillRow_2001004");yield return InventorySceneSmokeTests.Click("SpendSkillPoint");yield return null;yield return null;
            Assert.That(GameObject.Find("SkillSlot_0").transform.Find("Level").GetComponent<Text>().text,Is.EqualTo("2"));
            Assert.That(GameObject.Find("SkillPoints").GetComponent<Text>().text,Is.EqualTo("1"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-earned-skills",640,480);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");yield return InventorySceneSmokeTests.Click("ItemRow_1372005");
            yield return InventorySceneSmokeTests.Click("ItemAction");yield return InventorySceneSmokeTests.Click("CloseInventory");Step(world);
            int mp=player.CurrentMP,cost=world.SkillManager.LearnedSkills.Single(s=>s.SkillId==2001004).GetCurrentLevelData().MpCost;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");Assert.That(player.CurrentMP,Is.EqualTo(mp-cost));Step(world);
            var controller=manager.GetComponent<LocalProgressController>();controller.SaveFilePath=savePath;
            yield return InventorySceneSmokeTests.Click("LocalPlayToggle");yield return InventorySceneSmokeTests.Click("SaveLocalProgress");
            var saved=world.CaptureProgress();Assert.That(saved.Version,Is.EqualTo(5));
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;controller=manager.GetComponent<LocalProgressController>();controller.SaveFilePath=savePath;
            yield return InventorySceneSmokeTests.Click("LocalPlayToggle");yield return InventorySceneSmokeTests.Click("LoadLocalProgress");yield return null;yield return null;
            player=manager.World.Player;
            Assert.That(player.AbilityPoints,Is.EqualTo(saved.Player.AbilityPoints));Assert.That(player.SkillPoints,Is.EqualTo(1));
            Assert.That(player.JobId,Is.EqualTo(200));Assert.That(player.HasChosenFirstJob,Is.True);
            Assert.That(manager.World.SkillManager.GetSkillLevel(2001004),Is.EqualTo(2));Assert.That(manager.World.SkillManager.GetSkillLevel(2001005),Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<SkillBar>().CaptureSlots().Take(2),Is.EqualTo(new[]{2001004,2001005}));
            yield return InventorySceneSmokeTests.Click("CloseLocalPlay");yield return InventorySceneSmokeTests.Click("StatsToggle");yield return InventorySceneSmokeTests.Click("OpenCharacterProgression");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-progression-loaded",640,480);
        }
    }
}
