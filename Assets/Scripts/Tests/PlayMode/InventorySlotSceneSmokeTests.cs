using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class InventorySlotSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        private string savePath;
        [SetUp] public void Watch()
        {
            diagnostics.Clear();Application.logMessageReceived+=Log;
            savePath=Path.Combine(Application.temporaryCachePath,"inventory-slots-"+Guid.NewGuid().ToString("N")+".json");
        }
        private void Log(string message,string stack,LogType type) { if(type!=LogType.Log)diagnostics.Add(type+": "+message); }
        [TearDown] public void Cleanup()
        {
            Application.logMessageReceived-=Log;
            foreach(string path in new[]{savePath,savePath+".bak",savePath+".tmp"})if(File.Exists(path))File.Delete(path);
            Assert.That(diagnostics,Is.Empty);
        }
        private static InventoryStack Stack(int slot,int id,int count) => new InventoryStack{Slot=slot,ItemId=id,Quantity=count};
        private static InventorySlotInteraction Cell(int category,int slot) => Object.FindObjectsByType<InventorySlotInteraction>(FindObjectsSortMode.None).Single(c=>c.Category==category&&c.Slot==slot);
        private static string[] Contents(Inventory bag) => bag.GetStacks().Select(s=>$"{s.Category}:{s.Slot}:{s.ItemId}:{s.Quantity}").ToArray();
        private static PointerEventData PointAt(GameObject target)
        {
            var rect=target.GetComponent<RectTransform>();var canvas=target.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            Assert.That(hits,Is.Not.Empty,target.name);Assert.That(hits[0].gameObject,Is.EqualTo(target),target.name);
            e.pointerPressRaycast=hits[0];return e;
        }
        private static PointerEventData Begin(InventorySlotInteraction source)
        {
            var e=PointAt(source.gameObject);e.pointerDrag=source.gameObject;e.eligibleForClick=true;
            ExecuteEvents.Execute(source.gameObject,e,ExecuteEvents.beginDragHandler);
            Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Not.Null);Assert.That(e.eligibleForClick,Is.False);
            return e;
        }
        private static void Drag(int category,int sourceSlot,int destinationSlot)
        {
            var source=Cell(category,sourceSlot);var destination=Cell(category,destinationSlot);var e=Begin(source);
            e.position=PointAt(destination.gameObject).position;
            ExecuteEvents.Execute(source.gameObject,e,ExecuteEvents.dragHandler);
            // The drag icon must never steal the destination's real raycast.
            PointAt(destination.gameObject);
            ExecuteEvents.Execute(destination.gameObject,e,ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source.gameObject,e,ExecuteEvents.endDragHandler);
            Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Null);
        }

        [UnityTest] public IEnumerator DraggingSelectedStackActionsAndShopSalesSurviveAFreshSavedSession()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var player=world.Player;
            Assert.That(player.Inventory.Restore(new[]{Stack(1,2000000,70),Stack(2,2000000,60),Stack(3,2000003,10),Stack(1,1302000,1),Stack(2,1302000,1)}),Is.True);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");yield return InventorySceneSmokeTests.Click("InventoryCategory_2");
            Assert.That(Object.FindObjectsByType<InventorySlotInteraction>(FindObjectsSortMode.None).Length,Is.EqualTo(Inventory.SlotsPerCategory));
            // Exercise actual pointer routing with the canvas at 640x480, including an empty target.
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-inventory-moved",640,480,()=>Drag(2,1,24));
            yield return null;yield return null;
            Assert.That(player.Inventory.GetStack(2,1),Is.Null);Assert.That(player.Inventory.GetStack(2,24).Quantity,Is.EqualTo(70));
            Drag(2,24,3);yield return null;yield return null;
            Assert.That(player.Inventory.GetStack(2,24).ItemId,Is.EqualTo(2000003));
            Drag(2,3,2);yield return null;yield return null;
            Assert.That(player.Inventory.GetStack(2,2).Quantity,Is.EqualTo(100));Assert.That(player.Inventory.GetStack(2,3).Quantity,Is.EqualTo(30));
            player.SetHPMP(1,player.MaxMP);
            yield return InventorySceneSmokeTests.Click(Cell(2,3).name);yield return InventorySceneSmokeTests.Click("ItemAction");
            Assert.That(player.Inventory.GetStack(2,3).Quantity,Is.EqualTo(29));Assert.That(player.Inventory.GetStack(2,2).Quantity,Is.EqualTo(100));
            Assert.That(player.CurrentHP,Is.GreaterThan(1));
            yield return InventorySceneSmokeTests.Click("InventoryCategory_1");
            yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click(Cell(1,2).name);yield return InventorySceneSmokeTests.Click("ItemAction");
            Assert.That(player.Inventory.GetStack(1,1).ItemId,Is.EqualTo(1302000));Assert.That(player.Inventory.GetStack(1,2),Is.Null);
            Object.FindFirstObjectByType<InventoryView>().Show(false);
            world.LoadMap(100000102);yield return null;yield return null;
            var npc=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==OfflineEconomy.ShopNpcId);
            float ground=manager.FootholdService.GetGroundBelow(npc.X,npc.Y-5);
            player.Position=new LogicVector(npc.X/100,-ground/100+Player.Height/2);player.Velocity=LogicVector.Zero;
            yield return InventorySceneSmokeTests.Click("TradeToggle");yield return InventorySceneSmokeTests.Click("ShopCategory_2");
            yield return InventorySceneSmokeTests.Click("SellItem_2000000_Slot_3");yield return InventorySceneSmokeTests.Click("ShopSell");
            Assert.That(player.Inventory.GetStack(2,3).Quantity,Is.EqualTo(28));Assert.That(player.Inventory.GetStack(2,2).Quantity,Is.EqualTo(100));
            Assert.That(player.Mesos,Is.EqualTo(25));
            var controller=manager.GetComponent<LocalProgressController>();controller.SaveFilePath=savePath;
            yield return InventorySceneSmokeTests.Click("SaveLocalProgress");var expected=Contents(player.Inventory);
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;
            controller=manager.GetComponent<LocalProgressController>();controller.SaveFilePath=savePath;
            Assert.That(controller.TryLoad(out var result),Is.True,result);
            Assert.That(Contents(manager.World.Player.Inventory),Is.EqualTo(expected));
            yield return InventorySceneSmokeTests.Click("InventoryToggle");yield return InventorySceneSmokeTests.Click("InventoryCategory_2");
            yield return null;yield return null;
            Assert.That(Cell(2,24).ItemId,Is.EqualTo(2000003));Assert.That(Cell(2,1).ItemId,Is.Zero);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-inventory-restored",640,480);
        }

        [UnityTest] public IEnumerator InterruptedDragsNeverMoveOrDiscardItems()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var bag=world.Player.Inventory;
            bag.AddItem(2000000,5);yield return InventorySceneSmokeTests.Click("InventoryToggle");yield return InventorySceneSmokeTests.Click("InventoryCategory_2");
            yield return null;yield return null;
            var source=Cell(2,1);var e=Begin(source);bag.AddItem(2000000,1);
            Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Null);
            ExecuteEvents.Execute(Cell(2,24).gameObject,e,ExecuteEvents.dropHandler);
            Assert.That(bag.GetStack(2,1).Quantity,Is.EqualTo(6));Assert.That(bag.GetStack(2,24),Is.Null);
            yield return null;yield return null;
            source=Cell(2,1);e=Begin(source);
            e.position=new Vector2(1,Screen.height-1);ExecuteEvents.Execute(source.gameObject,e,ExecuteEvents.endDragHandler);
            Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Null);Assert.That(bag.GetStack(2,1).Quantity,Is.EqualTo(6));
            source=Cell(2,1);Begin(source);Object.FindFirstObjectByType<InventoryView>().Show(false);
            Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Null);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");yield return null;yield return null;
            Begin(Cell(2,1));world.LoadMap(100000000);Assert.That(GameObject.Find("DraggedInventoryItem"),Is.Null);
            Assert.That(bag.GetStack(2,1).Quantity,Is.EqualTo(6));Assert.That(bag.GetStack(2,24),Is.Null);
        }
    }
}
