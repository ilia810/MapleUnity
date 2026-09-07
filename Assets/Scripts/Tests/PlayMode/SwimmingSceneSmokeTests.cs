using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class SwimmingSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void WatchLogs(){diagnostics.Clear();Application.logMessageReceived+=OnLog;}
        private void OnLog(string message,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+message);}
        [TearDown] public void CheckLogs(){Application.logMessageReceived-=OnLog;Assert.That(diagnostics,Is.Empty);}
        private sealed class Input:IInputProvider
        {
            public bool IsLeftPressed{get;set;} public bool IsRightPressed{get;set;} public bool IsUpPressed{get;set;}
            public bool IsDownPressed{get;set;} public bool IsJumpPressed{get;set;} public bool IsAttackPressed{get;set;}
        }
        private static void Step(GameWorld w,int ticks=1){for(int i=0;i<ticks;i++){w.ProcessInput();w.UpdatePhysics(.008f);}}
        [UnityTest]
        public IEnumerator AquaRoadUsesRealSwimmingArtDirectionalInputPauseAttacksAndDryMapRecovery()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var game=Object.FindFirstObjectByType<GameManager>();game.enabled=false;var w=game.World;var p=w.Player;var input=new Input();
            typeof(GameWorld).GetField("inputProvider",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,input);
            Step(w,100);Assert.That(w.RequestPracticeSupplies(out _),Is.True);
            foreach(int id in new[]{1040002,1060002,1072001,1302000})Assert.That(w.UseInventoryItem(id,out _),Is.True);
            w.LoadMap(230000000);yield return null;yield return null;
            Assert.That(w.CurrentMap.IsUnderwater,Is.True);Step(w,120);Assert.That(p.IsGrounded,Is.True);
            var prompt=GameObject.Find("Prompt_SwimmingControls");Assert.That(prompt,Is.Not.Null);
            Assert.That(prompt.GetComponentInChildren<Text>().text,Does.Contain("movement controls to swim"));
            input.IsJumpPressed=true;Step(w,4);input.IsJumpPressed=false;input.IsRightPressed=true;input.IsUpPressed=true;
            Step(w,20);yield return null;yield return null;
            Assert.That(p.State,Is.EqualTo(PlayerState.Swimming));Assert.That(p.IsGrounded,Is.False);
            Assert.That(p.Velocity.X,Is.GreaterThan(0));Assert.That(p.Velocity.Y,Is.GreaterThan(0));
            var visual=GameObject.Find("Player").transform.Find("VisualRoot");var body=visual.Find("Body").GetComponent<SpriteRenderer>();
            var shirt=visual.Find("Equipment_Top_mail").GetComponent<SpriteRenderer>();
            var head=visual.Find("Head").GetComponent<SpriteRenderer>();
            Assert.That(body.sprite.name,Does.Contain("/fly/"));Assert.That(head.sprite,Is.Not.Null);
            Assert.That(shirt.sprite.name,Does.Contain("/fly/"));
            var position=p.Position;long ticks=p.SwimAnimationTicks;var sprite=body.sprite;
            yield return new WaitForSeconds(.35f);
            Assert.That(p.Position,Is.EqualTo(position));Assert.That(p.SwimAnimationTicks,Is.EqualTo(ticks));Assert.That(body.sprite,Is.SameAs(sprite));
            for(int frame=0;frame<2;frame++)
            {
                for(int tries=0;tries<100&&!body.sprite.name.Contains("/fly/"+frame+"/");tries++){Step(w);yield return null;}
                Assert.That(body.sprite.name,Does.Contain("/fly/"+frame+"/"));
                Assert.That(shirt.sprite.name,Does.Contain("/fly/"+frame+"/"));
                InventorySceneSmokeTests.Capture(p,"-swimming-frame-"+frame);
            }
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-swimming-aqua-road");
            input.IsAttackPressed=true;Step(w);input.IsAttackPressed=false;Assert.That(p.IsBasicAttacking,Is.True);
            var attack=p.BasicAttack;input.IsRightPressed=false;input.IsLeftPressed=true;input.IsUpPressed=false;
            Step(w,10);yield return null;yield return null;
            Assert.That(p.FacingRight,Is.EqualTo(attack.FacingRight));
            Assert.That(body.sprite.name,Does.Contain("/"+CharacterStances.Name(attack.Stance)+"/"));
            InventorySceneSmokeTests.Capture(p,"-swimming-attack");
            for(int i=0;i<160&&p.IsBasicAttacking;i++)Step(w);Step(w,8);yield return null;yield return null;
            Assert.That(p.IsBasicAttacking,Is.False);Assert.That(p.Velocity.X,Is.LessThan(0));Assert.That(p.FacingRight,Is.False);
            input.IsLeftPressed=false;w.LoadMap(100000000);yield return null;yield return null;Step(w,100);yield return null;yield return null;
            Assert.That(w.CurrentMap.IsUnderwater,Is.False);Assert.That(p.State,Is.EqualTo(PlayerState.Standing));Assert.That(p.SwimAnimationTicks,Is.Zero);
            Assert.That(p.GetEquippedItems().Values,Does.Contain(1040002));Assert.That(body.sprite.name,Does.Contain("/stand1/"));
            yield return new WaitForSeconds(.4f);Assert.That(GameObject.Find("Prompt_SwimmingControls"),Is.Null);
            input.IsRightPressed=true;Step(w,25);Assert.That(p.Velocity.X,Is.GreaterThan(0));Assert.That(p.State,Is.EqualTo(PlayerState.Walking));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-swimming-return-henesys");
        }
    }
}

