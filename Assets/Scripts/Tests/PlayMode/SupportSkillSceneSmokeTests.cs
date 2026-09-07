using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
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
    public class SupportSkillSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message,string stack,LogType type) { if(type!=LogType.Log)diagnostics.Add(type+": "+message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived-=OnLog; Assert.That(diagnostics,Is.Empty); }
        private static void Step(GameWorld world,int count) { for(int i=0;i<count;i++){world.ProcessInput();world.UpdatePhysics(.008f);} }

        [UnityTest] public IEnumerator EarnedMageDefensesCastFromQuickslotsAndRenderTheSourceArmorEcho()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var p=world.Player;Step(world,100);
            world.RequestPracticeSupplies(out _);
            foreach(int id in new[]{1040002,1060002,1072001})Assert.That(world.UseInventoryItem(id,out _),Is.True);
            while(p.Level<8)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(200,out var message),Is.True,message);
            p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return null;
            yield return InventorySceneSmokeTests.Click("SkillRow_2001002");
            for(int i=0;i<3;i++){yield return InventorySceneSmokeTests.Click("SpendSkillPoint_2001002");yield return null;}
            yield return InventorySceneSmokeTests.Click("SkillRow_2001003");
            yield return InventorySceneSmokeTests.Click("SpendSkillPoint_2001003");yield return null;
            Assert.That(p.SkillPoints,Is.Zero);Assert.That(world.SkillManager.GetSkillLevel(2001002),Is.EqualTo(3));
            yield return InventorySceneSmokeTests.Click("SkillRow_2001002");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("17% of contact damage uses MP"));
            yield return InventorySceneSmokeTests.Click("AssignSkill_0");
            yield return InventorySceneSmokeTests.Click("CloseSkillActions");
            yield return InventorySceneSmokeTests.Click("SkillRow_2001003");
            yield return InventorySceneSmokeTests.Click("AssignSkill_1");
            Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return null;
            yield return new WaitForSeconds(3f);
            int mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(world,15);yield return null;yield return null;
            var effect=GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>();Assert.That(effect.sprite,Is.Not.Null);
            Assert.That(GameObject.Find("Buff_2001002").transform.Find("Icon").GetComponent<Image>().sprite,Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-guard-cast");yield return null;yield return null;
            Assert.That(p.CurrentMP,Is.EqualTo(mp-6));
            int hp=p.CurrentHP;Assert.That(p.ReceiveContactDamage(20,false),Is.True);
            Assert.That(p.CurrentHP,Is.EqualTo(hp-17));Assert.That(p.CurrentMP,Is.EqualTo(mp-9));
            Step(world,260);yield return null;
            yield return InventorySceneSmokeTests.Click("SkillSlot_1");Step(world,25);yield return null;yield return null;
            Assert.That(p.WeaponDefense,Is.GreaterThanOrEqualTo(12));
            var echo=GameObject.Find("ArmorEcho");Assert.That(echo,Is.Not.Null);
            var copies=echo.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null).ToArray();
            Assert.That(copies.Length,Is.GreaterThan(3));
            Assert.That(echo.transform.localScale.x,Is.EqualTo(1.4f).Within(.001f));
            Assert.That(copies,Is.All.Matches<SpriteRenderer>(r=>Mathf.Abs(r.color.a-.6f)<.001f));
            var body=echo.transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.That(body.sprite,Is.SameAs(echo.transform.parent.Find("Body").GetComponent<SpriteRenderer>().sprite));
            yield return new WaitForSeconds(.12f);Assert.That(p.ArmorEchoMilliseconds,Is.EqualTo(300));
            InventorySceneSmokeTests.Capture(p,"-magic-armor-closeup");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-magic-armor-cast");yield return null;yield return null;
            Step(world,38);yield return null;Assert.That(echo.activeSelf,Is.False);
            Assert.That(world.SkillManager.IsBuffActive(2001003),Is.True);
            var save=world.CaptureProgress();world.LoadMap(100000001);yield return null;yield return null;
            Assert.That(world.SkillManager.IsBuffActive(2001002),Is.True);
            Assert.That(world.TryRestoreProgress(save,out message),Is.True,message);yield return null;yield return null;
            Assert.That(p.ActiveBuffs,Is.Empty);Assert.That(world.SkillManager.GetSkillLevel(2001002),Is.EqualTo(3));
            Assert.That(GameObject.Find("Buff_2001002"),Is.Null);Assert.That(GameObject.Find("Buff_2001003"),Is.Null);
        }

        [UnityTest] public IEnumerator SupportPresetsRenderBuffsAndHyperBodyExpiresWithoutGrantingResources()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var p=world.Player;Step(world,100);
            foreach(int job in new[]{100,110,210,220,200,130})
            {
                Step(world,100);p.CurrentMP=p.MaxMP;
                Object.FindFirstObjectByType<SkillMenu>().Show(false);
                yield return InventorySceneSmokeTests.OpenPractice();
                yield return InventorySceneSmokeTests.Click("OpenSupportPractice");
                if(job==100){yield return PlayerCombatSceneSmokeTests.CaptureScreen("-support-picker-small",640,480);yield return null;yield return null;}
                int mp=p.CurrentMP,level=p.Level;long exp=p.Experience;
                yield return InventorySceneSmokeTests.Click("SupportPreset_"+job);yield return null;
                Assert.That(p.JobId,Is.EqualTo(job));Assert.That(p.CurrentMP,Is.EqualTo(mp));Assert.That(p.Level,Is.EqualTo(level));Assert.That(p.Experience,Is.EqualTo(exp));
                int id=GameWorld.SupportPracticeSkills(job)[0];
                yield return InventorySceneSmokeTests.Click("SkillRow_"+id);
                Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Not.Contain("action not available"));
                Object.FindFirstObjectByType<SkillMenu>().Show(false);
                yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(world,15);yield return null;yield return null;
                Assert.That(GameObject.Find("Buff_"+id),Is.Not.Null);
                if(job!=100)Assert.That(GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-support-"+job);yield return null;yield return null;
            }
            Step(world,100);p.CurrentMP=p.MaxMP;int hp=p.CurrentHP,baseHp=p.MaxHP,baseMp=p.MaxMP,mpBefore=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_1");Step(world,15);yield return null;yield return null;
            Assert.That(baseHp,Is.EqualTo(100));Assert.That(baseMp,Is.EqualTo(100));
            Assert.That(p.MaxHP,Is.EqualTo(102));Assert.That(p.MaxMP,Is.EqualTo(102));
            Assert.That(p.CurrentHP,Is.EqualTo(hp));Assert.That(p.CurrentMP,Is.EqualTo(mpBefore-20));
            Assert.That(GameObject.Find("Buff_1301007"),Is.Not.Null);Assert.That(GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
            Object.FindFirstObjectByType<CharacterProgressionView>().Show(true);yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-hyper-body-stats");yield return null;yield return null;
            p.Heal(999);p.CurrentMP=p.MaxMP;Step(world,1235);yield return null;yield return null;
            Assert.That(p.MaxHP,Is.EqualTo(baseHp));Assert.That(p.CurrentHP,Is.EqualTo(baseHp));Assert.That(p.CurrentMP,Is.EqualTo(baseMp));
            Assert.That(GameObject.Find("Buff_1301007"),Is.Null);
        }
    }
}
