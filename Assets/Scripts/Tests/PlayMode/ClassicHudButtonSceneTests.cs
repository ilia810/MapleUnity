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
    public sealed class ClassicHudButtonSceneTests
    {
        [UnityTest]
        public IEnumerator ReferenceButtonsRestoreAfterPressAndKeepTheirActions()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            Object.FindFirstObjectByType<GameManager>().enabled = false;
            var input = EventSystem.current;
            // Drive real EventSystem handlers deterministically, without the desktop
            // cursor changing selection between rendered state captures.
            var module = input.currentInputModule;
            bool moduleEnabled = module != null && module.enabled;
            if (module != null) module.enabled = false;
            try
            {
                var buttons = new List<ClassicHudButton>();
                foreach (string name in new[] { "CashShop", "LocalPlayToggle", "Shortcuts" })
                {
                    var b = GameObject.Find(name).GetComponent<ClassicHudButton>();
                    Assert.That(b, Is.Not.Null, name);
                    foreach (var art in new[] { b.image.sprite, b.spriteState.highlightedSprite,
                        b.spriteState.pressedSprite, b.spriteState.disabledSprite })
                        Assert.That(art, Is.Not.Null, name + " must import every button state.");
                    buttons.Add(b);
                }
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-normal", 800, 600);
                var pointer = new PointerEventData(input) { button = PointerEventData.InputButton.Left };
                foreach (var b in buttons)
                {
                    input.SetSelectedGameObject(null);
                    b.OnPointerEnter(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.spriteState.highlightedSprite));
                    b.OnPointerDown(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.spriteState.pressedSprite));
                    b.OnPointerUp(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.spriteState.highlightedSprite),
                        "Releasing inside a mouse-selected button must restore its hover artwork.");
                    b.OnPointerExit(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.image.sprite),
                        "A mouse click must not leave a sticky keyboard highlight.");
                    b.OnPointerEnter(pointer); b.OnPointerDown(pointer);
                    b.OnPointerExit(pointer); b.OnPointerUp(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.image.sprite), "Release outside resets the face.");

                    input.SetSelectedGameObject(null);
                    input.SetSelectedGameObject(b.gameObject, new BaseEventData(input));
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.spriteState.selectedSprite), "Keyboard focus is visible.");
                    input.SetSelectedGameObject(null);
                    int activations = 0;
                    UnityEngine.Events.UnityAction count = () => activations++;
                    b.onClick.AddListener(count);
                    b.OnPointerClick(new PointerEventData(input) { button = PointerEventData.InputButton.Right });
                    b.interactable = false;
                    b.OnPointerEnter(pointer); b.OnPointerDown(pointer); b.OnPointerUp(pointer); b.OnPointerClick(pointer);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.spriteState.disabledSprite));
                    Assert.That(activations, Is.Zero, "Right clicks and disabled buttons must not invoke actions.");
                    b.OnPointerExit(pointer); b.interactable = true; b.onClick.RemoveListener(count);
                    b.OnPointerEnter(pointer); b.OnPointerDown(pointer);
                    b.gameObject.SetActive(false); b.gameObject.SetActive(true);
                    Assert.That(b.image.overrideSprite, Is.EqualTo(b.image.sprite), "Hiding while pressed clears the state.");
                }
                foreach (var b in buttons) b.OnPointerEnter(pointer);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-hover", 800, 600);
                foreach (var b in buttons) b.OnPointerDown(pointer);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-pressed", 800, 600);
                foreach (var b in buttons) { b.OnPointerUp(pointer); b.OnPointerExit(pointer); b.interactable = false; }
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-disabled", 800, 600);
                foreach (var b in buttons) b.interactable = true;
                input.SetSelectedGameObject(null);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-small", 640, 480);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buttons-wide", 1366, 768);

                yield return InventorySceneSmokeTests.Click("LocalPlayToggle");
                Assert.That(GameObject.Find("LocalPlayPanel"), Is.Not.Null);
                yield return InventorySceneSmokeTests.Click("LocalPlayToggle");
                Assert.That(GameObject.Find("LocalPlayPanel"), Is.Null);
                yield return InventorySceneSmokeTests.Click("Shortcuts");
                Assert.That(GameObject.Find("CloseShortcuts"), Is.Not.Null);
                yield return InventorySceneSmokeTests.Click("CloseShortcuts");
                yield return InventorySceneSmokeTests.Click("CashShop");
                Assert.That(GameObject.Find("HudMessage").GetComponent<Text>().text, Does.Contain("Cash Shop is unavailable"));
                input.SetSelectedGameObject(buttons[1].gameObject, new BaseEventData(input));
                buttons[1].OnSubmit(new BaseEventData(input));
                Assert.That(GameObject.Find("LocalPlayPanel"), Is.Not.Null, "Keyboard submit opens the adventure menu.");
                Assert.That(buttons[1].image.overrideSprite, Is.EqualTo(buttons[1].spriteState.pressedSprite));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(buttons[1].image.overrideSprite, Is.EqualTo(buttons[1].spriteState.selectedSprite));
            }
            finally { if (module != null) module.enabled = moduleEnabled; }
        }
    }
}
