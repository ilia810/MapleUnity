using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using LogicVector=MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicShopTooltipSceneSmokeTests
    {
        private readonly List<string> errors=new List<string>();
        [SetUp] public void Watch(){errors.Clear();Application.logMessageReceived+=Log;}
        private void Log(string text,string stack,LogType type){if(type!=LogType.Log)errors.Add(type+": "+text);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(errors,Is.Empty);}
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed {get;set;} public bool IsRightPressed {get;set;}
            public bool IsUpPressed {get;set;} public bool IsDownPressed {get;set;}
            public bool IsJumpPressed {get;set;} public bool IsAttackPressed {get;set;}
        }
        private static void Step(GameWorld world,int count){for(int i=0;i<count;i++){world.ProcessInput();world.UpdatePhysics(.008f);}}
        private static GameWorld Pause()
        {
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;
            for(int i=0;i<100;i++)manager.World.UpdatePhysics(.008f);return manager.World;
        }
        private static void InCanvas(RectTransform r)
        {
            var canvas=(RectTransform)r.GetComponentInParent<Canvas>().rootCanvas.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);
            foreach(var v in corners){var p=canvas.InverseTransformPoint(v);Assert.That(p.x,Is.InRange(canvas.rect.xMin-.1f,canvas.rect.xMax+.1f));Assert.That(p.y,Is.InRange(canvas.rect.yMin-.1f,canvas.rect.yMax+.1f));}
        }
        [UnityTest] public IEnumerator NativeShopRowsScrollAndTradeWithAnIndependentEquippedStandingPortrait()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();var player=world.Player;
            world.RequestPracticeSupplies(out _);foreach(int id in new[]{1040002,1060002,1072001,1302000})Assert.That(world.UseInventoryItem(id,out _),Is.True);
            world.LoadMap(100000102);yield return null;yield return null;
            var npc=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc);
            player.ResetMovementForMap();player.Position=new LogicVector(npc.Feet.x,npc.Feet.y+Player.Height/2);player.IsGrounded=false;
            for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
            var input=new Input{IsRightPressed=true};typeof(GameWorld).GetField("inputProvider",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,input);
            Step(world,12);yield return null;Assert.That(player.State,Is.EqualTo(PlayerState.Walking));
            int elapsed=player.StanceAnimation.ElapsedMilliseconds;var state=player.BodyStance;var position=player.Position;
            var menu=Object.FindFirstObjectByType<LocalPlayMenu>();menu.OpenShop();yield return null;yield return null;
            var portrait=GameObject.Find("ShopPlayer").GetComponent<ClassicPlayerPortrait>();portrait.Present();
            Assert.That(portrait.transform.Find("Body").GetComponent<Image>().sprite.name,Does.Contain("/stand1/0/"));
            Assert.That(portrait.transform.Find("Equipment_Top_mail").GetComponent<Image>().sprite.name,Does.Contain("1040002"));
            Assert.That(player.BodyStance,Is.EqualTo(state));Assert.That(player.StanceAnimation.ElapsedMilliseconds,Is.EqualTo(elapsed));Assert.That(player.Position,Is.EqualTo(position));
            Assert.That(GameObject.Find("Player").transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>().sprite.name,Does.Contain("/walk1/"));
            Assert.That(portrait.GetComponentsInChildren<SpriteRenderer>().Length,Is.Zero,"The portrait composition must never draw a second character into the world.");
            Assert.That(world.UseInventoryItem(1402009,out _),Is.True);yield return null;portrait.Present();
            Assert.That(portrait.GetComponentsInChildren<Image>().Any(i=>i.name.StartsWith("Equipment_Weapon")&&i.sprite.name.Contains("1402009")),Is.True);
            Assert.That(portrait.transform.Find("Body").GetComponent<Image>().sprite.name,Does.Contain("/stand2/0/"));
            Assert.That(player.StanceAnimation.ElapsedMilliseconds,Is.EqualTo(elapsed));
            input.IsRightPressed=false;Step(world,100);player.TryGainMesos(1000);yield return null;
            var first=GameObject.Find("BuyItem_2000000");Assert.That(first.transform.Find("PriceCoin").GetComponent<Image>().sprite.name,Does.Contain("Shop/meso"));
            Assert.That(first.transform.Find("Price").GetComponent<Text>().text,Is.EqualTo("50 Mesos"));
            yield return InventorySceneSmokeTests.Click("BuyItem_2000000");yield return null;
            Assert.That(GameObject.Find("BuyItem_2000000").transform.Find("Selection").GetComponent<Image>().sprite.name,Does.Contain("Shop/select"));
            int before=player.Inventory.GetItemCount(2000000);yield return InventorySceneSmokeTests.Click("ShopBuy");Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(before+1));
            Assert.That(GameObject.Find("ShopBuyUp").GetComponent<Button>().interactable,Is.False);
            yield return InventorySceneSmokeTests.Click("ShopBuyDown");yield return null;Assert.That(GameObject.Find("BuyItem_2000000"),Is.Null);
            var row=GameObject.Find("BuyItem_2000001");ExecuteEvents.Execute(row,new PointerEventData(EventSystem.current){scrollDelta=Vector2.up},ExecuteEvents.scrollHandler);yield return null;
            Assert.That(GameObject.Find("BuyItem_2000000"),Is.Not.Null,"Wheel input over an item must reach its column.");
            for(int i=0;i<3;i++){yield return InventorySceneSmokeTests.Click("ShopBuyDown");yield return null;}
            Assert.That(GameObject.Find("ShopBuyDown").GetComponent<Button>().interactable,Is.False);Assert.That(GameObject.Find("BuyItem_1072001"),Is.Not.Null);
            yield return InventorySceneSmokeTests.Click("BuyItem_1302000");yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shop-native-detail",1366,768,()=>InCanvas(GameObject.Find("LocalShopPanel").GetComponent<RectTransform>()));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shop-native-small",640,480,()=>{
                InCanvas(GameObject.Find("LocalShopPanel").GetComponent<RectTransform>());
                foreach(var image in portrait.GetComponentsInChildren<Image>())InCanvas(image.rectTransform);
            });
            yield return InventorySceneSmokeTests.Click("LeaveShop");Assert.That(menu.ShopVisible,Is.False);
            world.LoadMap(100000000);yield return null;yield return null;Assert.That(menu.ShopVisible,Is.False);
        }
        [UnityTest] public IEnumerator EquipmentTooltipsFitAllCornersRefreshRequirementsAndPassClicksThrough()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();var player=world.Player;
            world.RequestPracticeSupplies(out _);Assert.That(world.UseInventoryItem(1302000,out _),Is.True);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");
            var tooltip=Object.FindFirstObjectByType<ClassicTooltipView>();var owner=GameObject.Find("InventoryPanel");
            tooltip.ShowItem(owner,new Vector2(400,300),player,1372005);yield return null;
            Assert.That(GameObject.Find("ItemTooltip").transform.Find("Requirements").GetComponent<Text>().text,Does.Contain("#ff5454>REQ LEVEL : 8"));
            player.Level=10;yield return null;yield return null;
            Assert.That(GameObject.Find("ItemTooltip").transform.Find("Requirements").GetComponent<Text>().text,Does.Contain("#b7c3dc>REQ LEVEL : 8"),"A stationary tooltip must refresh when requirements change.");
            foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(640,480)})
            {
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-equipment-tooltip-"+size.x,size.x,size.y,()=>{
                    foreach(var point in new[]{new Vector2(2,2),new Vector2(size.x-2,2),new Vector2(2,size.y-2),new Vector2(size.x-2,size.y-2)})
                    {
                        tooltip.ShowItem(owner,point,player,1402009);Canvas.ForceUpdateCanvases();
                        foreach(string name in new[]{"ItemTooltip","EquippedTooltip"})
                        {
                            var card=GameObject.Find(name).GetComponent<RectTransform>();InCanvas(card);
                            var jobs=card.Find("JobEligibility").GetComponentsInChildren<Text>();Assert.That(jobs.Length,Is.EqualTo(6));
                            Assert.That(jobs.All(t=>t.preferredHeight<=t.rectTransform.rect.height+.1f),Is.True,"Job eligibility must remain a single row.");
                            Assert.That(jobs.All(t=>t.preferredWidth<=t.rectTransform.rect.width+.1f),Is.True,"Each class label needs its measured width.");
                            var req=card.Find("Requirements").GetComponent<Text>();Assert.That(req.preferredHeight,Is.LessThanOrEqualTo(req.rectTransform.rect.height+.1f));
                        }
                    }
                    Assert.That(tooltip.transform.Find("ClassicTooltip").GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),Is.True);
                });
            }
            tooltip.ShowItem(owner,new Vector2(320,240),player,2000000);yield return null;
            Assert.That(GameObject.Find("ItemTooltip").transform.Find("ItemDetails").GetComponent<Text>().text,Does.Not.Contain("\\n"),"NX description line escapes must render as real line breaks.");
            Assert.That(GameObject.Find("EquippedTooltip"),Is.Null,"Hovering a consumable clears equipment comparison.");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-consumable-tooltip",1366,768);
            yield return InventorySceneSmokeTests.Click("CloseInventory");yield return null;Assert.That(tooltip.Owner,Is.Null);
        }
    }
}
