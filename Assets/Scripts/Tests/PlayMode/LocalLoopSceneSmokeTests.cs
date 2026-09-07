using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
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
    public class LocalLoopSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        private string savePath;
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; savePath=Path.Combine(Application.temporaryCachePath,"local-loop-"+Guid.NewGuid().ToString("N")+".json"); }
        private void OnLog(string message,string stack,LogType type) { if(type!=LogType.Log)diagnostics.Add(type+": "+message); }
        [TearDown] public void Cleanup()
        {
            Application.logMessageReceived-=OnLog;
            foreach(string file in new[]{savePath,savePath+".bak",savePath+".tmp"})if(File.Exists(file))File.Delete(file);
            Assert.That(diagnostics,Is.Empty);
        }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed {get;set;} public bool IsRightPressed {get;set;}
            public bool IsJumpPressed {get;set;} public bool IsAttackPressed {get;set;}
            public bool IsUpPressed {get;set;} public bool IsDownPressed {get;set;}
        }
        [UnityTest] public IEnumerator EarnLootTradeAtLunaAndRestoreTheSavedCharacterInAFreshScene()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single); yield return null; yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>(); manager.enabled=false;
            var world=manager.World; var player=world.Player; var input=new Input();
            typeof(GameWorld).GetField("inputProvider",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,input);
            world.LoadMap(100010000); Step(world,100); yield return null; yield return null;
            player.SetBaseDamage(100000); // Test fixture only: shorten ten real combat kills.
            var defeated=new List<Monster>(); world.MonsterDied+=defeated.Add;
            for(int i=0;i<10;i++)
            {
                var mob=world.Monsters.First(m=>m.MonsterId==100100 || m.MonsterId==100101); defeated.Clear(); player.ResetMovementForMap(); player.Position=new LogicVector(mob.Position.X-.65f,mob.Position.Y+Player.Height/2);
                player.Velocity=LogicVector.Zero; player.SetHPMP(player.MaxHP,player.MaxMP);
                input.IsRightPressed=true; Step(world,1); input.IsRightPressed=false;
                input.IsAttackPressed=true; Step(world,1); input.IsAttackPressed=false;
                Assert.That(defeated.Count,Is.GreaterThan(0),"Combat kill "+i); mob=defeated[0];
                if(i==0)
                {
                    yield return null; yield return null;
                    var drops=Object.FindObjectsByType<DroppedItemView>(FindObjectsSortMode.None);
                    Assert.That(drops.Length,Is.GreaterThanOrEqualTo(2));
                    foreach(var drop in drops) Assert.That(drop.GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
                    var camera=Camera.main; var previous=camera.transform.position; float size=camera.orthographicSize;
                    try { camera.transform.position=new Vector3((player.Position.X+mob.Position.X)/2+.12f,player.Position.Y,-10); camera.orthographicSize=1; RecoverySceneSmokeTests.SaveCameraImage(camera,"-local-loot",640,640); }
                    finally { camera.transform.position=previous; camera.orthographicSize=size; }
                }
                player.ResetMovementForMap(); player.Position=new LogicVector(mob.Position.X,mob.Position.Y+Player.Height/2); player.Velocity=LogicVector.Zero;
                Step(world,2); Step(world,90);
            }
            Assert.That(player.Mesos,Is.GreaterThanOrEqualTo(50)); Assert.That(player.Level,Is.GreaterThan(1));
            world.LoadMap(100000102); Step(world,80); yield return null; yield return null;
            var npc=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==OfflineEconomy.ShopNpcId);
            float ground=manager.FootholdService.GetGroundBelow(npc.X,npc.Y-5);
            player.ResetMovementForMap(); player.Position=new LogicVector(npc.X/100,-ground/100+Player.Height/2); player.Velocity=LogicVector.Zero; Step(world,1);
            var menu=Object.FindFirstObjectByType<LocalPlayMenu>(); yield return InventorySceneSmokeTests.Click("LocalPlayToggle"); yield return null;
            yield return InventorySceneSmokeTests.Click("OpenLocalShop"); yield return null;
            Assert.That(GameObject.Find("LocalShopPanel"),Is.Not.Null);
            Assert.That(Camera.main.backgroundColor,Is.EqualTo(Color.black),"Small interiors have the source black surround.");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-local-shop",checkLayout:()=>{
                var shop=GameObject.Find("LocalShopPanel").GetComponent<RectTransform>();
                Assert.That(shop.anchoredPosition.x,Is.EqualTo(0).Within(.1));Assert.That(shop.anchoredPosition.y,Is.EqualTo(0).Within(.1),"An untouched shop recenters after a narrow viewport has clamped it.");
            });
            int money=player.Mesos, potions=player.Inventory.GetItemCount(2000000);
            yield return InventorySceneSmokeTests.Click("BuyItem_2000000"); yield return InventorySceneSmokeTests.Click("ShopBuy"); yield return null;
            Assert.That(player.Mesos,Is.EqualTo(money-50)); Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(potions+1));
            yield return InventorySceneSmokeTests.Click("ShopCategory_4"); yield return null;
            int material=player.Inventory.HasItem(4000019)?4000019:4000000;
            int count=player.Inventory.GetItemCount(material); money=player.Mesos;
            yield return InventorySceneSmokeTests.Click("SellItem_"+material); yield return InventorySceneSmokeTests.Click("ShopSell"); yield return null;
            Assert.That(player.Inventory.GetItemCount(material),Is.EqualTo(count-1)); Assert.That(player.Mesos,Is.GreaterThan(money));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-local-sell");
            Assert.That(world.RequestPracticeSupplies(out _),Is.True);
            Assert.That(world.RequestPracticeMagicSkills(out _),Is.True); Assert.That(world.UseInventoryItem(1372005,out _),Is.True);
            var bar=Object.FindFirstObjectByType<SkillBar>(); bar.AssignSkillToSlot(2001004,0);
            var controller=manager.GetComponent<LocalProgressController>(); controller.SaveFilePath=savePath;
            yield return InventorySceneSmokeTests.Click("SaveLocalProgress"); yield return null;
            Assert.That(File.Exists(savePath),Is.True); var saved=world.CaptureProgress();
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single); yield return null; yield return null;
            manager=Object.FindFirstObjectByType<GameManager>(); manager.enabled=false; controller=manager.GetComponent<LocalProgressController>(); controller.SaveFilePath=savePath;
            Assert.That(manager.World.Player.Mesos,Is.Zero);
            menu=Object.FindFirstObjectByType<LocalPlayMenu>(); yield return InventorySceneSmokeTests.Click("LocalPlayToggle"); yield return null;
            yield return InventorySceneSmokeTests.Click("LoadLocalProgress"); yield return null; yield return null;
            player=manager.World.Player;
            Assert.That(manager.World.CurrentMapId,Is.EqualTo(100000102)); Assert.That(player.Mesos,Is.EqualTo(saved.Player.Mesos));
            Assert.That(player.Level,Is.EqualTo(saved.Player.Level)); Assert.That(player.Experience,Is.EqualTo(saved.Player.Experience));
            Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(saved.Player.Bag.Where(s=>s.ItemId==2000000).Sum(s=>s.Quantity)));
            Assert.That(player.GetEquippedItems().Values,Does.Contain(1372005)); Assert.That(manager.World.RequestPracticeSupplies(out _),Is.False);
            Assert.That(Object.FindFirstObjectByType<SkillBar>().CaptureSlots()[0],Is.EqualTo(2001004));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-local-loaded");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-local-narrow",800,600);
        }
        private static void Step(GameWorld world,int count) { for(int i=0;i<count;i++){world.ProcessInput();world.UpdatePhysics(.008f);} }
    }
}
