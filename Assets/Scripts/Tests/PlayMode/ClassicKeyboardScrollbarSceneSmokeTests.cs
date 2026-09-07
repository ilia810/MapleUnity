using System.Collections;
using System.Collections.Generic;
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

namespace MapleClient.Tests.PlayMode
{
    public class ClassicKeyboardScrollbarSceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string text, string stack, LogType type) { if (type != LogType.Log) errors.Add(type + ": " + text); }
        [TearDown] public void Check() { Application.logMessageReceived -= Log; Assert.That(errors, Is.Empty); }
        private static GameWorld Pause()
        {
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            for (int i = 0; i < 100; i++) manager.World.UpdatePhysics(.008f); return manager.World;
        }
        private static Vector2 Point(GameObject target)
        {
            var r = target.GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        }
        private static PointerEventData Begin(string name)
        {
            var source = GameObject.Find(name); source.GetComponentInParent<ClassicWindow>()?.Focus(); Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = Point(source), pointerDrag = source,
                button = PointerEventData.InputButton.Left, eligibleForClick = true };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(source), name);
            ExecuteEvents.Execute(source, pointer, ExecuteEvents.beginDragHandler);
            Assert.That(pointer.eligibleForClick, Is.False, name); return pointer;
        }
        private static IEnumerator Ready(string name)
        {
            // A newly focused window needs a rendered frame before GraphicRaycaster's depths change.
            yield return null; yield return null;
            Assert.That(GameObject.Find(name), Is.Not.Null, name);
            GameObject.Find(name).GetComponentInParent<ClassicWindow>()?.Focus(); yield return null; Canvas.ForceUpdateCanvases();
        }
        internal static IEnumerator Drag(string source, string destination)
        {
            yield return Ready(source); var pointer = Begin(source);
            yield return Ready(destination); Drop(pointer, destination);
        }
        private static void Drop(PointerEventData pointer, string name)
        {
            var destination = GameObject.Find(name); destination.GetComponentInParent<ClassicWindow>()?.Focus(); Canvas.ForceUpdateCanvases();
            pointer.position = Point(destination); ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(destination), name);
            ExecuteEvents.Execute(destination, pointer, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }
        private static void InCanvas(RectTransform rect)
        {
            var canvas = (RectTransform)rect.GetComponentInParent<Canvas>().rootCanvas.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = canvas.InverseTransformPoint(corner);
                Assert.That(p.x, Is.InRange(canvas.rect.xMin - .1f, canvas.rect.xMax + .1f), rect.name);
                Assert.That(p.y, Is.InRange(canvas.rect.yMin - .1f, canvas.rect.yMax + .1f), rect.name);
            }
        }
        private static void KeyboardBounds()
        {
            Canvas.ForceUpdateCanvases(); var keyboard = GameObject.Find("QuickslotSettings").GetComponent<RectTransform>(); InCanvas(keyboard);
            foreach (var button in keyboard.GetComponentsInChildren<Button>()) InCanvas(button.GetComponent<RectTransform>());
            foreach (var key in keyboard.GetComponentsInChildren<ClassicKeyboardKey>())
            {
                var label = key.transform.Find("KeyLabel").GetComponent<Image>(); Assert.That(label.sprite != null || key.transform.Find("KeyLabelFallback") != null, Is.True);
                var icon = key.transform.Find("BindingIcon").GetComponent<Image>(); Assert.That(icon.rectTransform.rect.size, Is.EqualTo(new Vector2(32, 32)));
            }
            Assert.That(keyboard.localScale.x, Is.EqualTo(keyboard.localScale.y));
        }
        [UnityTest] public IEnumerator NativeKeyboardSharesRealBindingsAndCoexistsWithBagAndSkillBook()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var world = Pause(); Assert.That(world.RequestPracticeSupplies(out _), Is.True); Assert.That(world.RequestPracticeMagicSkills(out _), Is.True);
            var bar = Object.FindFirstObjectByType<SkillBar>(); var bag = Object.FindFirstObjectByType<InventoryView>(); var skills = Object.FindFirstObjectByType<SkillMenu>();
            bag.ShowBag(true); skills.Show(true); yield return null; yield return null;
            GameObject.Find("InventoryPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(-510, 70));
            GameObject.Find("SkillPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(495, 70));
            yield return InventorySceneSmokeTests.Click("SkillTier_1"); yield return null;
            yield return InventorySceneSmokeTests.Click("InventoryCategory_2"); yield return null;
            yield return InventorySceneSmokeTests.Click("Shortcuts"); yield return null;
            var keyboard = GameObject.Find("QuickslotSettings");
            Assert.That(keyboard.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(629, 373)));
            Assert.That(keyboard.transform.Find("KeyboardBackground").GetComponent<Image>().sprite.name, Does.EndWith("KeyConfig/backgrnd"));
            Assert.That(keyboard.GetComponentsInChildren<ClassicKeyboardKey>().Length, Is.EqualTo(KeyboardMap.Keys.Count));
            Assert.That(bag.BagVisible && skills.Visible, Is.True);
            int before = world.Player.Inventory.GetItemCount(2000000);
            yield return Drag("ItemRow_2000000", "ShortcutBinding_2");
            Assert.That(bar.BindingAt(2).Id, Is.EqualTo(2000000)); Assert.That(world.Player.Inventory.GetItemCount(2000000), Is.EqualTo(before));
            yield return Drag("SkillRow_2001004", "ShortcutBinding_0"); Assert.That(bar.BindingAt(0).Id, Is.EqualTo(2001004));
            yield return Drag("BindJump", "ShortcutBinding_6"); Assert.That(bar.BindingAt(6).Id, Is.EqualTo(53));
            Assert.That(bar.ActionPressed(QuickslotAction.Jump, _ => false), Is.False, "Editing a key must not execute its action.");
            yield return Drag("ShortcutBinding_0", "ShortcutBinding_3");
            Assert.That(bar.BindingAt(3).Id, Is.EqualTo(2001004)); Assert.That(bar.BindingAt(0).Id, Is.EqualTo(2000000));
            yield return Drag("SkillSlot_4", "ShortcutBinding_1");
            Assert.That(bar.BindingAt(1).Id, Is.EqualTo(52)); Assert.That(bar.BindingAt(4).Kind, Is.EqualTo(QuickslotKind.Empty));
            yield return null; yield return null;
            var aliasIcon = GameObject.Find("KeyboardAlias_3").transform.Find("BindingIcon").GetComponent<Image>().sprite;
            Assert.That(aliasIcon.name, Is.EqualTo(GameObject.Find("ShortcutBinding_3").transform.Find("BindingIcon").GetComponent<Image>().sprite.name));
            Assert.That(aliasIcon.name, Is.EqualTo(GameObject.Find("SkillSlot_3").GetComponent<SkillSlot>().Icon.sprite.name));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-keyboard-wide", 1366, 768, KeyboardBounds);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-keyboard-small", 640, 480, KeyboardBounds);
            yield return null; yield return null;
            keyboard.GetComponent<ClassicWindow>().Focus(); yield return null; Canvas.ForceUpdateCanvases();
            var title = keyboard.transform.Find("DragTitle").gameObject; var windowRect = keyboard.GetComponent<RectTransform>(); var position = windowRect.anchoredPosition;
            var move = new PointerEventData(EventSystem.current) { position = Point(title), delta = new Vector2(30, -15), button = PointerEventData.InputButton.Left };
            var titleHits = new List<RaycastResult>(); EventSystem.current.RaycastAll(move, titleHits); Assert.That(titleHits[0].gameObject, Is.EqualTo(title));
            ExecuteEvents.Execute(title, move, ExecuteEvents.beginDragHandler); ExecuteEvents.Execute(title, move, ExecuteEvents.dragHandler); ExecuteEvents.Execute(title, move, ExecuteEvents.endDragHandler);
            yield return null; Assert.That(windowRect.anchoredPosition, Is.Not.EqualTo(position));
            keyboard.GetComponent<ClassicWindow>().Focus(); Object.FindFirstObjectByType<ClassicWindowManager>().CloseFrontmost(); yield return null;
            Assert.That(keyboard.activeSelf, Is.False); Assert.That(bag.BagVisible && skills.Visible, Is.True);
            bar.ShowShortcuts(3); yield return null;
            Assert.That(GameObject.Find("SelectedKeyboardKey").GetComponent<Text>().text, Does.StartWith("PgUp:"));
            yield return InventorySceneSmokeTests.Click("ApplyShortcuts"); Assert.That(keyboard.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator KeyboardClearDefaultsAndStaleDragsPreserveInput()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var world = Pause(); var bar = Object.FindFirstObjectByType<SkillBar>(); bar.ShowShortcuts(2); yield return null; yield return null;
            yield return InventorySceneSmokeTests.Click("BindJump"); Assert.That(bar.BindingAt(2).Id, Is.EqualTo(53));
            yield return Ready("ShortcutBinding_2"); var stale = Begin("ShortcutBinding_2"); bar.AssignActionToSlot(QuickslotAction.Attack, 2);
            yield return Ready("ShortcutBinding_0"); Drop(stale, "ShortcutBinding_0");
            Assert.That(bar.BindingAt(0).Kind, Is.EqualTo(QuickslotKind.Empty)); Assert.That(bar.BindingAt(2).Id, Is.EqualTo(52));
            yield return InventorySceneSmokeTests.Click("KeyboardKey_I"); var original = bar.CaptureBindings();
            yield return InventorySceneSmokeTests.Click("BindJump"); Assert.That(bar.CaptureBindings().Select(b => b.Id), Is.EqualTo(original.Select(b => b.Id)));
            Assert.That(bar.BindingAtKey(KeyboardKey.I).Id, Is.EqualTo(53));
            yield return InventorySceneSmokeTests.Click("ClearAllShortcuts"); Assert.That(bar.CaptureBindings().All(b => b.Kind == QuickslotKind.Empty), Is.True);
            yield return InventorySceneSmokeTests.Click("DefaultShortcuts"); Assert.That(bar.BindingAt(4).Id, Is.EqualTo(52));
            Assert.That(bar.BindingAt(3).Id, Is.EqualTo(2000000)); Assert.That(bar.BindingAt(7).Id, Is.EqualTo(2000003));
            yield return Ready("BindAttack"); var drag = Begin("BindAttack"); yield return InventorySceneSmokeTests.Click("CloseShortcuts");
            Assert.That(GameObject.Find("DraggedQuickslot"), Is.Null); Drop(drag, "SkillSlot_0"); Assert.That(bar.BindingAt(0).Kind, Is.EqualTo(QuickslotKind.Empty));
            bar.ShowShortcuts(3); yield return null; yield return null;
            world.Player.Inventory.AddItem(2000000, 2); world.Player.SetHPMP(1, world.Player.MaxMP);
            bar.HandleKeys(k => k == KeyCode.PageUp || k == KeyCode.Alpha4); Assert.That(world.Player.Inventory.GetItemCount(2000000), Is.EqualTo(1));
            var key = GameObject.Find("KeyboardAlias_3");
            ExecuteEvents.Execute(key, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right }, ExecuteEvents.pointerClickHandler);
            Assert.That(bar.BindingAtKey(KeyboardKey.Alpha4).Kind, Is.EqualTo(QuickslotKind.Empty));
            Assert.That(bar.BindingAt(3).Id, Is.EqualTo(2000000), "Clearing a number alias leaves its tray key intact.");
        }
        private static void NativeScrollbar(Scrollbar bar, bool movable)
        {
            bar.GetComponent<ClassicScrollbarSkin>().Layout(); Canvas.ForceUpdateCanvases();
            var track = bar.GetComponent<Image>(); Assert.That(track.sprite.name, Does.EndWith("VScr4/" + (movable ? "enabled" : "disabled") + "/base"));
            Assert.That(track.type, Is.EqualTo(Image.Type.Tiled)); Assert.That(track.rectTransform.rect.width, Is.EqualTo(15));
            Assert.That(bar.handleRect.gameObject.activeSelf, Is.EqualTo(movable));
            if (movable)
            {
                Assert.That(bar.handleRect.rect.size.x, Is.EqualTo(15).Within(.01)); Assert.That(bar.handleRect.rect.size.y, Is.EqualTo(25).Within(.01));
                Assert.That(bar.spriteState.highlightedSprite.name, Does.EndWith("VScr4/enabled/thumb1"));
            }
        }
        [UnityTest] public IEnumerator NativeScrollbarPiecesStayUnstretchedAndDragRealShopAndStoryContent()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null; var world = Pause();
            var bag = Object.FindFirstObjectByType<InventoryView>(); bag.ShowBag(true); yield return null; yield return null;
            NativeScrollbar(GameObject.Find("InventoryScroll").GetComponent<Scrollbar>(), false);
            Assert.That(GameObject.Find("InventoryScrollUp").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("InventoryScrollDown").GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(15, 13)));
            bag.ShowBag(false); world.LoadMap(100000102); yield return null; yield return null;
            var npc = WorldLabelAnchor.Active.Single(a => a.Kind == WorldLabelAnchor.ActorKind.Npc);
            world.Player.ResetMovementForMap(); world.Player.Position = new MapleClient.GameLogic.Vector2(npc.Feet.x, npc.Feet.y + Player.Height / 2);
            for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
            Object.FindFirstObjectByType<LocalPlayMenu>().OpenShop(); yield return null; yield return null;
            var buy = GameObject.Find("ShopBuyScroll").GetComponent<Scrollbar>(); NativeScrollbar(buy, true);
            NativeScrollbar(GameObject.Find("ShopSellScroll").GetComponent<Scrollbar>(), false);
            var thumb = buy.handleRect.gameObject;
            var pointer = new PointerEventData(EventSystem.current) { position = Point(thumb), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits); Assert.That(hits[0].gameObject, Is.EqualTo(thumb));
            ExecuteEvents.Execute(buy.gameObject, pointer, ExecuteEvents.beginDragHandler);
            var slide = (RectTransform)buy.handleRect.parent;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, slide.TransformPoint(new Vector2(slide.rect.center.x, slide.rect.yMin + 12.5f)));
            ExecuteEvents.Execute(buy.gameObject, pointer, ExecuteEvents.dragHandler); ExecuteEvents.Execute(buy.gameObject, pointer, ExecuteEvents.endDragHandler);
            yield return null; yield return null;
            Assert.That(GameObject.Find("BuyItem_2000000"), Is.Null); Assert.That(GameObject.Find("BuyItem_1072001"), Is.Not.Null);
            NativeScrollbar(buy, true); Assert.That(GameObject.Find("ShopBuyDown").GetComponent<Button>().interactable, Is.False);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-scrollbars-shop", 1366, 768, () => NativeScrollbar(buy, true));
            yield return InventorySceneSmokeTests.Click("LeaveShop");
            world.LoadMap(100000000); yield return null; yield return null;
            while (world.Player.Level < 10) world.Player.AddExperience(world.Player.ExperienceToNextLevel - world.Player.Experience);
            var bruce = WorldLabelAnchor.Active.Single(a => a.Kind == WorldLabelAnchor.ActorKind.Npc && a.NpcId == 1012111);
            world.Player.ResetMovementForMap(); world.Player.Position = new MapleClient.GameLogic.Vector2(bruce.Feet.x, bruce.Feet.y + Player.Height / 2); world.Player.IsGrounded = true;
            Assert.That(world.StartQuest(world.CurrentMap.NpcSpawns.Single(n => n.NpcId == 1012111), 2088, out var message), Is.True, message);
            Object.FindFirstObjectByType<ClassicQuestView>().OpenQuest(2088); yield return null; yield return null;
            var story = GameObject.Find("QuestStoryScroll").GetComponent<Scrollbar>(); NativeScrollbar(story, true);
            float before = story.value; yield return InventorySceneSmokeTests.Click("QuestStoryScrollDown"); yield return null;
            Assert.That(story.value, Is.LessThan(before)); Assert.That(GameObject.Find("QuestStory").GetComponent<ScrollRect>().content.anchoredPosition.y, Is.GreaterThan(0));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-scrollbars-quest", 1366, 768, () => NativeScrollbar(story, true));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-scrollbars-small", 640, 480, () => { NativeScrollbar(story, true); InCanvas(story.GetComponent<RectTransform>()); });
        }
    }
}
