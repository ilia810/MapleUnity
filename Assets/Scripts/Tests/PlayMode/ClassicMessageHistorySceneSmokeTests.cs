using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class ClassicMessageHistorySceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string text, string stack, LogType type) { if (type != LogType.Log) errors.Add(type + ": " + text); }
        [TearDown] public void Check() { Application.logMessageReceived -= Log; Assert.That(errors, Is.Empty); }
        private static GameManager Pause()
        {
            var game = Object.FindFirstObjectByType<GameManager>(); game.enabled = false;
            for (int i = 0; i < 100; i++) game.World.UpdatePhysics(.008f); return game;
        }
        private static Vector2 Point(GameObject target)
        {
            var rect = (RectTransform)target.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }
        private static void Hit(GameObject target, PointerEventData e)
        {
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(target));
        }
        private static void Bounds()
        {
            var history = Object.FindFirstObjectByType<ClassicMessageHistory>(); history.Layout(); Canvas.ForceUpdateCanvases();
            var canvas = (RectTransform)history.GetComponentInParent<Canvas>().rootCanvas.transform;
            foreach (string name in new[] { "LocalMessageHistory", "ToggleMessageHistory", "MessagePrevious", "MessageNext", "MessageEnvelope" })
            {
                var rect = GameObject.Find(name).GetComponent<RectTransform>(); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var p = canvas.InverseTransformPoint(corner);
                    Assert.That(p.x, Is.InRange(canvas.rect.xMin - .1f, canvas.rect.xMax + .1f), name);
                    Assert.That(p.y, Is.InRange(canvas.rect.yMin - .1f, canvas.rect.yMax + .1f), name);
                }
            }
            var bar = GameObject.Find("MessageHistoryScroll").GetComponent<Scrollbar>(); bar.GetComponent<ClassicScrollbarSkin>().Layout();
            // Canvas scaling can introduce subpixel rounding in RectTransform dimensions.
            Assert.That(bar.handleRect.rect.width, Is.EqualTo(15).Within(.001f));
            Assert.That(bar.handleRect.rect.height, Is.EqualTo(25).Within(.001f));
            var message = history.transform.Find("HudMessage").GetComponent<Text>();
            Assert.That(message.preferredWidth, Is.LessThanOrEqualTo(message.rectTransform.rect.width + .1f));
            var envelope = history.transform.Find("MessageEnvelope/MemoIcon").GetComponent<ClassicEnvelopeGraphic>();
            var envelopeMesh = envelope.canvasRenderer.GetMesh();
            try
            {
                Assert.That(envelopeMesh, Is.Not.Null);
                Assert.That(envelopeMesh.vertexCount, Is.GreaterThan(4), "Envelope needs a rendered glyph, not just its empty button well.");
                Assert.That(envelope.canvasRenderer.cull, Is.False);
            }
            finally { Object.Destroy(envelopeMesh); }
        }
        [UnityTest] public IEnumerator NativeMessageStripWrapsColorsAndScrollsActualHistoryWithoutJumpingOnNewMessages()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Pause(); var status = Object.FindFirstObjectByType<StatusBar>(); var history = Object.FindFirstObjectByType<ClassicMessageHistory>();
            Assert.That(history.transform.Find("MessageChannel").GetComponent<Image>().sprite.name, Does.EndWith("StatusBar.img/base/chatTarget"));
            Assert.That(history.transform.Find("MessageEnvelope").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("MessagePrevious").GetComponent<Button>().interactable, Is.False);
            game.World.Player.AddExperience(4); yield return null;
            Assert.That(history.Latest, Does.Contain("EXP"));
            game.World.AddDroppedItem(2000000, 2, game.World.Player.Position); game.World.UpdatePhysics(.008f); yield return null;
            Assert.That(history.Latest, Does.Contain("Red Potion")); Assert.That(history.Latest, Does.Contain("2"));
            var text = GameObject.Find("HistoryMessages").GetComponent<Text>();
            Assert.That(text.text, Does.Contain("<color=#" + ColorUtility.ToHtmlStringRGB(ClassicNotifications.ExperienceColor) + ">"));
            for (int i = 0; i < 12; i++) status.PostMessage("Message " + i + ": " + string.Join(" ", Enumerable.Repeat("A longer local message that wraps across the history area.", 3)));
            yield return null; yield return null;
            var scroll = GameObject.Find("LocalMessageHistory").GetComponent<ScrollRect>();
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(history.ScrollPosition, Is.EqualTo(0).Within(.001));
            yield return InventorySceneSmokeTests.Click("MessagePrevious"); yield return null;
            Assert.That(history.ScrollPosition, Is.GreaterThan(0));
            var point = scroll.content.anchoredPosition;
            status.PostMessage("A new message arrives while you read older lines."); yield return null;
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(point.y).Within(.1));
            Assert.That(history.ScrollPosition, Is.GreaterThan(0));
            var wheelTarget = GameObject.Find("HudMessageHover");
            var wheel = new PointerEventData(EventSystem.current) { position = Point(wheelTarget), scrollDelta = Vector2.up };
            Hit(wheelTarget, wheel); float before = history.ScrollPosition;
            ExecuteEvents.Execute(wheelTarget, wheel, ExecuteEvents.scrollHandler); yield return null;
            Assert.That(history.ScrollPosition, Is.GreaterThan(before));
            scroll.verticalNormalizedPosition = 0; status.PostMessage("<b>This stays literal</b>\nThe second line stays readable."); yield return null;
            Assert.That(text.text, Does.Contain(ClassicTooltipView.Escape("<b>This stays literal</b>")));
            Assert.That(text.text, Does.Contain("\nThe second line")); Assert.That(history.ScrollPosition, Is.EqualTo(0).Within(.001));
            Assert.That(history.transform.Find("MessageChannel").gameObject.activeSelf, Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-history-wide", 1366, 768, Bounds);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-history-small", 640, 480, Bounds);
            yield return null; yield return null;
            Object.FindFirstObjectByType<PlayerCombatFeedback>().ShowActionMessage("This action cannot be used here. " + string.Join(" ", Enumerable.Repeat("Long messages stay inside the strip.", 8)));
            yield return null;
            Assert.That(history.transform.Find("MessageChannel").gameObject.activeSelf, Is.False);
            Assert.That(history.transform.Find("HudMessageNotice").GetComponent<Image>().enabled, Is.True);
            Assert.That(history.transform.Find("HudMessage").GetComponent<Text>().text, Does.EndWith("…"));
            Assert.That(text.text, Does.Contain("Long messages stay inside the strip."));
            var toast = GameObject.Find("EventNotifications").GetComponentsInChildren<Text>().Last();
            Assert.That(toast.text, Does.EndWith("…")); Assert.That(toast.preferredHeight, Is.LessThanOrEqualTo(40));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-notice-wide", 1366, 768, Bounds);
            yield return null; yield return null;
            yield return InventorySceneSmokeTests.Click("ToggleMessageHistory"); yield return null;
            Assert.That(GameObject.Find("LocalMessageHistory"), Is.Null);
            Assert.That(GameObject.Find("ToggleMessageHistory").GetComponent<Image>().sprite.name, Does.EndWith("BtMax/normal/0"));
            status.PostMessage("New hidden message."); yield return null;
            Assert.That(GameObject.Find("LocalMessageHistory"), Is.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-history-collapsed", 1366, 768);
            yield return null; yield return null;
            yield return InventorySceneSmokeTests.Click("ToggleMessageHistory"); yield return null;
            Assert.That(GameObject.Find("HistoryMessages").GetComponent<Text>().text, Does.Contain("New hidden message."));
        }
        [UnityTest] public IEnumerator HistoryResizeThumbAndRetentionKeepTheHudUsableAndDoNotBlockOtherWindows()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Pause(); var status = Object.FindFirstObjectByType<StatusBar>(); var history = Object.FindFirstObjectByType<ClassicMessageHistory>();
            for (int i = 0; i < 55; i++) status.PostMessage("Record " + i.ToString("00") + " — local system history.");
            yield return null; yield return null;
            Assert.That(history.Count, Is.EqualTo(50)); var text = GameObject.Find("HistoryMessages").GetComponent<Text>();
            Assert.That(text.text, Does.Not.Contain("Record 04")); Assert.That(text.text, Does.Contain("Record 05"));
            var grip = GameObject.Find("MessageHistoryResize"); float oldHeight = history.Height;
            var pointer = new PointerEventData(EventSystem.current) { position = Point(grip), button = PointerEventData.InputButton.Left, eligibleForClick = true };
            Hit(grip, pointer);
            Assert.That(Object.FindFirstObjectByType<ClassicCursorView>().StateAt(pointer.position), Is.EqualTo(ClassicCursorView.Grabbable));
            ExecuteEvents.Execute(grip, pointer, ExecuteEvents.beginDragHandler); pointer.position += Vector2.up * 100;
            ExecuteEvents.Execute(grip, pointer, ExecuteEvents.dragHandler); ExecuteEvents.Execute(grip, pointer, ExecuteEvents.endDragHandler); yield return null;
            Assert.That(history.Height, Is.GreaterThan(oldHeight + 90));
            var scrollbar = GameObject.Find("MessageHistoryScroll").GetComponent<Scrollbar>();
            var thumb = scrollbar.handleRect.gameObject;
            pointer = new PointerEventData(EventSystem.current) { position = Point(thumb), button = PointerEventData.InputButton.Left };
            Hit(thumb, pointer); ExecuteEvents.Execute(scrollbar.gameObject, pointer, ExecuteEvents.beginDragHandler);
            var slide = (RectTransform)scrollbar.handleRect.parent;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, slide.TransformPoint(new Vector2(slide.rect.center.x, slide.rect.yMax - 12.5f)));
            ExecuteEvents.Execute(scrollbar.gameObject, pointer, ExecuteEvents.dragHandler); ExecuteEvents.Execute(scrollbar.gameObject, pointer, ExecuteEvents.endDragHandler); yield return null;
            Assert.That(history.ScrollPosition, Is.GreaterThan(.99f));
            status.PostMessage("Record 55 — new entry while reading the oldest retained record."); yield return null;
            Assert.That(history.ScrollPosition, Is.GreaterThan(.99f)); Assert.That(history.Count, Is.EqualTo(50));
            Assert.That(text.text, Does.Not.Contain("Record 05")); Assert.That(text.text, Does.Contain("Record 06"));
            var bag = Object.FindFirstObjectByType<InventoryView>(); bag.ShowBag(true); Object.FindFirstObjectByType<SkillMenu>().Show(true);
            yield return null; yield return null;
            Assert.That(bag.BagVisible, Is.True); Assert.That(Object.FindFirstObjectByType<SkillMenu>().Visible, Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-history-resized", 1366, 768, Bounds);
            history.SetHeight(9999); yield return PlayerCombatSceneSmokeTests.CaptureScreen("-message-history-tall-small", 640, 480, Bounds);
            yield return null; yield return null;
            game.World.LoadMap(100000102); yield return null; yield return null;
            Assert.That(history.Count, Is.EqualTo(50)); Assert.That(text.text, Does.Contain("Record 55"));
        }
    }
}
