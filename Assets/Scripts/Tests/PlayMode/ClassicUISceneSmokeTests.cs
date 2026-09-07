using System.Collections;
using System.Collections.Generic;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicUISceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string text,string stack,LogType type) { if(type!=LogType.Log) errors.Add(type+": "+text); }
        [TearDown] public void Check() { Application.logMessageReceived-=Log; Assert.That(errors,Is.Empty); }
        [UnityTest]
        public IEnumerator OriginalArtUsesNativeGeometryAndWindowsRemainUsableAcrossResolutions()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single); yield return null; yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>(); manager.enabled=false;
            var world=manager.World; var player=world.Player;
            for(int i=0;i<150;i++){world.ProcessInput();world.UpdatePhysics(.008f);}
            Assert.That(Object.FindFirstObjectByType<MovementStateUI>(),Is.Null);
            Assert.That(GameObject.Find("CombatStatsPanel").GetComponent<CanvasGroup>().alpha,Is.Zero);
            var hud=GameObject.Find("ClassicHUD").GetComponent<RectTransform>();
            Assert.That(hud.rect.height,Is.EqualTo(71));
            Assert.That(hud.Find("Background").GetComponent<Image>().sprite.rect.size,Is.EqualTo(new Vector2(800,71)));
            var inventoryButton=GameObject.Find("InventoryToggle").GetComponent<Button>();
            Assert.That(inventoryButton.spriteState.highlightedSprite,Is.Not.Null);
            Assert.That(inventoryButton.spriteState.pressedSprite,Is.Not.Null);
            Assert.That(inventoryButton.image.sprite.name,Does.Contain("StatusBar.img/InvenKey/normal/0"));
            player.SetHPMP(player.MaxHP/2,0); player.AddExperience(4);yield return null;
            Assert.That(hud.Find("HPMissing").GetComponent<Image>().fillAmount,Is.EqualTo(1-(float)player.CurrentHP/player.MaxHP).Within(.001));
            Assert.That(hud.Find("MPMissing").GetComponent<Image>().fillAmount,Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-hud",1280,720,()=>AssertHud(hud,1280));
            Assert.That(hud.Find("CharacterJob").GetComponent<Text>().text,Is.EqualTo("Beginner"));
            player.SetHPMP(player.MaxHP,player.MaxMP);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");
            yield return InventorySceneSmokeTests.Click("PracticeSupplies"); yield return null; yield return null;
            Assert.That(GameObject.Find("InventoryPanel").GetComponent<RectTransform>().rect.size,Is.EqualTo(new Vector2(211,289)));
            Assert.That(Object.FindObjectsByType<InventorySlotInteraction>(FindObjectsSortMode.None).Length,Is.EqualTo(30));
            var item=GameObject.Find("ItemRow_1302000").GetComponent<RectTransform>();
            Assert.That(item.rect.size,Is.EqualTo(new Vector2(32,32)));
            yield return InventorySceneSmokeTests.Click("ItemRow_1302000");yield return InventorySceneSmokeTests.Click("ItemAction");
            yield return InventorySceneSmokeTests.Click("InventoryCategory_2"); yield return null; yield return null;
            Assert.That(GameObject.Find("ItemRow_2000000"),Is.Not.Null);
            Assert.That(GameObject.Find("ItemRow_1040002"),Is.Null,"Equipment must not appear in the Use category.");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-inventory");
            yield return InventorySceneSmokeTests.OpenEquipment(); yield return null; yield return null;
            var weapon=GameObject.Find("EquipmentRow_1302000").GetComponent<RectTransform>();
            Assert.That(weapon.anchoredPosition,Is.EqualTo(new Vector2(104,-134)));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-equipment");
            var panel=GameObject.Find("InventoryPanel").GetComponent<RectTransform>();
            var drag=panel.GetComponentInChildren<ClassicWindowDrag>();
            drag.OnBeginDrag(new PointerEventData(EventSystem.current));
            drag.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(5000,5000)});yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-inventory-small",640,480,()=>AssertInCanvas(panel));
            Object.FindFirstObjectByType<InventoryView>().Show(false);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("PracticeSkills");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-skills");
            yield return InventorySceneSmokeTests.Click("SkillRow_1001004");yield return InventorySceneSmokeTests.Click("AssignSkill_7");
            Assert.That(Object.FindFirstObjectByType<SkillBar>().CaptureSlots()[7],Is.EqualTo(1001004));
            yield return InventorySceneSmokeTests.Click("CloseSkills");
            Assert.That(hud.Find("CharacterJob").GetComponent<Text>().text,Is.EqualTo("Fighter"));
            yield return InventorySceneSmokeTests.Click("QuickslotToggle");
            Assert.That(hud.Find("SkillBar").gameObject.activeSelf,Is.False);
            yield return InventorySceneSmokeTests.Click("QuickslotToggle");
            Assert.That(hud.Find("SkillBar").gameObject.activeSelf,Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-hud-small",640,480,()=>AssertHud(hud,640));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-hud-native",800,600,()=>AssertHud(hud,800));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-hud-wide",1366,768,()=>AssertHud(hud,1366));
            // Capture each original Short Cut state: the caption reaches closer to the
            // texture edge than the other HUD buttons and must not enter a stretch band.
            var shortcuts = hud.Find("Shortcuts").GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            shortcuts.OnPointerEnter(pointer);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shortcut-hover",1366,768);
            shortcuts.OnPointerDown(pointer);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shortcut-pressed",1366,768);
            shortcuts.OnPointerUp(pointer); shortcuts.OnPointerExit(pointer);
            shortcuts.interactable = false;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shortcut-disabled",1366,768);
            shortcuts.interactable = true;
            Assert.That(hud.Find("SkillBar/SkillSlot_0/Level").GetComponentsInChildren<Image>().Length,Is.EqualTo(2),
                "The learned level 20 must render both original number glyphs after the slot template is cloned.");
            yield return InventorySceneSmokeTests.Click("CashShop");yield return null;
            Assert.That(hud.Find("HudMessage").GetComponent<Text>().text,Does.Contain("Cash Shop is unavailable"));
            Assert.That(GameObject.Find("HistoryMessages").GetComponent<Text>().text,Does.Contain("Cash Shop is unavailable"));
            yield return InventorySceneSmokeTests.Click("ToggleMessageHistory");yield return null;
            Assert.That(GameObject.Find("LocalMessageHistory"),Is.Null);
            yield return InventorySceneSmokeTests.Click("ToggleMessageHistory");yield return null;
            Assert.That(GameObject.Find("HistoryMessages").GetComponent<Text>().text,Does.Contain("Cash Shop is unavailable"));
            yield return InventorySceneSmokeTests.Click("TradeToggle");
            Assert.That(GameObject.Find("LocalNotice").GetComponent<Text>().text,Does.Contain("Visit Luna"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-menu",800,600);
        }
        [UnityTest]
        public IEnumerator IndependentWindowsKeepTheirPositionsAndEscapeClosesOnlyTheFrontmost()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;
            for(int i=0;i<150;i++){manager.World.ProcessInput();manager.World.UpdatePhysics(.008f);}
            manager.World.RequestPracticeSupplies(out _);manager.World.UseInventoryItem(1302000,out _);
            manager.World.RequestPracticeSkills(out _);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");
            var bag=GameObject.Find("InventoryPanel").GetComponent<RectTransform>();
            var drag=bag.GetComponentInChildren<ClassicWindowDrag>();drag.OnBeginDrag(new PointerEventData(EventSystem.current));
            drag.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(-75,25)});yield return null;
            Vector2 bagPosition=bag.anchoredPosition;
            yield return InventorySceneSmokeTests.Click("EquipmentToggle");yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("StatsToggle");
            var inventory=Object.FindFirstObjectByType<InventoryView>();var skills=Object.FindFirstObjectByType<SkillMenu>();var stats=Object.FindFirstObjectByType<CharacterProgressionView>();
            Assert.That(inventory.BagVisible&&inventory.EquipmentVisible&&skills.Visible&&stats.Visible,Is.True);
            Assert.That(bag.anchoredPosition,Is.EqualTo(bagPosition),"Opening other windows must not reset a dragged bag.");
            var windows=bag.GetComponentInParent<ClassicWindowManager>();
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-multiple-windows",1366,768,()=>{
                // Spread the independently draggable windows for the visual review.
                bag.GetComponent<ClassicWindow>().SetPosition(new Vector2(-400,30));
                GameObject.Find("EquipmentPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(-190,30));
                GameObject.Find("SkillPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(430,30));
                GameObject.Find("CombatStatsPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(100,20));
                Canvas.ForceUpdateCanvases();
                var point=RectTransformUtility.WorldToScreenPoint(bag.GetComponentInParent<Canvas>().worldCamera,bag.TransformPoint(new Vector2(-bag.rect.width/2+12,bag.rect.height/2-9)));
                Assert.That(windows.FocusAt(point),Is.EqualTo(bag.GetComponent<ClassicWindow>()));
                Assert.That(bag.GetSiblingIndex(),Is.EqualTo(bag.parent.childCount-1));
            });
            windows.CloseFrontmost();yield return null;
            Assert.That(inventory.BagVisible,Is.False);Assert.That(inventory.EquipmentVisible&&skills.Visible&&stats.Visible,Is.True);
            windows.CloseFrontmost();yield return null;
            Assert.That(stats.Visible,Is.False);Assert.That(inventory.EquipmentVisible&&skills.Visible,Is.True);
            yield return InventorySceneSmokeTests.Click("InventoryToggle");
            yield return InventorySceneSmokeTests.Click("ItemRow_1402009");yield return null;
            Assert.That(GameObject.Find("EquippedTooltip").activeInHierarchy,Is.True);
            Assert.That(GameObject.Find("EquippedItemDetails").GetComponent<Text>().text,Does.Contain("Weapon attack"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-equipment-tooltip",1366,768);
            inventory.Show(false);skills.Show(false);stats.Show(true);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-classic-stats",1366,768);
        }
        private static void AssertHud(RectTransform hud,int width)
        {
            AssertInCanvas(hud);
            Assert.That(hud.rect.width,Is.EqualTo(800).Within(.1f));
            Assert.That(hud.localScale.x,Is.EqualTo(Mathf.Min(1,width/800f)).Within(.001),"Scale the complete HUD together on smaller displays.");
            Assert.That(hud.GetComponentInParent<Canvas>().pixelPerfect,Is.True);
            Assert.That(((RectTransform)hud.Find("Background")).rect.width,Is.EqualTo(800).Within(.1f));
            var canvas=(RectTransform)hud.GetComponentInParent<Canvas>().rootCanvas.transform;
            var center=canvas.InverseTransformPoint(hud.TransformPoint(hud.rect.center));
            Assert.That(center.x,Is.EqualTo(0).Within(.51f),"Wide-screen HUD must remain centered.");
            Assert.That(((RectTransform)hud.Find("ExperienceBar/Missing")).rect.height,Is.EqualTo(14),"EXP overlay must not cover the lower frame.");
            var gauges=(RectTransform)hud.Find("Gauges");
            foreach(string name in new[]{"CashShop","TradeToggle","LocalPlayToggle","Shortcuts"})
            {
                var button=(RectTransform)hud.Find(name);AssertInCanvas(button);
                Assert.That(BoundsInHud(button,hud).Overlaps(BoundsInHud(gauges,hud)),Is.False,name+" covers a status gauge.");
            }
            var quickslots=(RectTransform)hud.Find("SkillBar");AssertInCanvas(quickslots);
            Assert.That(quickslots.rect.size,Is.EqualTo(new Vector2(151,80)));
            Assert.That(BoundsInHud(quickslots,hud).xMax,Is.EqualTo(hud.rect.xMax).Within(.1),"Quickslots dock to the HUD's right edge.");
            foreach(string label in new[]{"HP Text","MP Text","ExperienceBar/Text"})
                Assert.That(BoundsInHud((RectTransform)hud.Find(label),hud).yMin,
                    Is.GreaterThan(BoundsInHud((RectTransform)hud.Find("ExperienceBar"),hud).yMax),"Gauge values belong above their fills.");
            var buttons=new List<Rect>();
            foreach(string name in new[]{"EquipmentToggle","InventoryToggle","StatsToggle","SkillsToggle","TradeToggle","QuickslotToggle","CashShop","LocalPlayToggle","Shortcuts"})
            {
                var button=(RectTransform)hud.Find(name);AssertInCanvas(button);
                var bounds=BoundsInHud(button,hud);
                foreach(var other in buttons)Assert.That(bounds.Overlaps(other),Is.False,name+" overlaps another control.");
                buttons.Add(bounds);
            }
            for(int i=0;i<8;i++)
            {
                var slot=(RectTransform)quickslots.Find("SkillSlot_"+i);
                Assert.That(slot.anchoredPosition,Is.EqualTo(new Vector2(7+i%4*35,-7-i/4*34)));
                Assert.That(slot.rect.size,Is.EqualTo(new Vector2(32,32)));
                Assert.That(((RectTransform)slot.Find("Icon")).rect.size,Is.EqualTo(new Vector2(32,32)));
                Assert.That(((RectTransform)slot.Find("Cooldown")).rect.size,Is.EqualTo(new Vector2(28,28)));
            }
        }
        private static Rect BoundsInHud(RectTransform r,RectTransform hud)
        {
            var corners=new Vector3[4];r.GetWorldCorners(corners);
            var min=hud.InverseTransformPoint(corners[0]);var max=hud.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private static void AssertInCanvas(RectTransform r)
        {
            var canvas=r.GetComponentInParent<Canvas>().rootCanvas;var cr=(RectTransform)canvas.transform;
            var corners=new Vector3[4];r.GetWorldCorners(corners);
            foreach(var corner in corners){var p=cr.InverseTransformPoint(corner);Assert.That(p.x,Is.InRange(cr.rect.xMin-.1f,cr.rect.xMax+.1f));Assert.That(p.y,Is.InRange(cr.rect.yMin-.1f,cr.rect.yMax+.1f));}
        }
    }
}
