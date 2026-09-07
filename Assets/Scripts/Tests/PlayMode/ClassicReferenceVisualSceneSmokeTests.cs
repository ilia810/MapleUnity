using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
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
    public sealed class ClassicReferenceVisualSceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
        [TearDown] public void Check() { Application.logMessageReceived -= Log; Assert.That(errors, Is.Empty); }
        private static GameWorld Pause()
        {
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            for (int i = 0; i < 100; i++) manager.World.UpdatePhysics(.008f);
            return manager.World;
        }
        private static void Equip(GameWorld world, int id)
        {
            world.Player.Inventory.TryAddItem(id, 1);
            Assert.That(world.UseInventoryItem(id, out var message), Is.True, id + ": " + message);
        }
        private static void Beside(GameWorld world, WorldLabelAnchor npc, float offset = -.5f)
        {
            world.Player.ResetMovementForMap();
            world.Player.Position = new LogicVector(npc.Feet.x + offset, npc.Feet.y + Player.Height / 2);
            world.Player.IsGrounded = false;
            for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
        }
        private static void Invoke(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTest] public IEnumerator HeadbandsHelmetsAndAccessoriesKeepSourceAnchorsInWorldAndPortraits()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var world = Pause(); var player = world.Player;
            player.Level = 100; player.JobId = 100; player.STR = player.DEX = player.INT = player.LUK = 100;
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000, 1012000, 1022000, 1032000 }) Equip(world, id);
            yield return null; yield return null;
            var actor = GameObject.Find("Player").GetComponent<MapleCharacterRenderer>();
            var root = GameObject.Find("Player").transform.Find("VisualRoot");
            SpriteRenderer Layer(string name) => root.Find(name).GetComponent<SpriteRenderer>();
            foreach (string slot in new[] { "FaceAccessory", "EyeAccessory", "Earring" })
                Assert.That(Layer("Equipment_" + slot + "_default").sprite, Is.Not.Null, slot);
            Assert.That(Layer("Equipment_EyeAccessory_default").bounds.center.y,
                Is.GreaterThan(Layer("Body").bounds.center.y), "Glasses must attach to the head, not the navel.");
            Assert.That(Layer("Equipment_Earring_default").sortingOrder, Is.LessThan(Layer("Head").sortingOrder));
            Assert.That(player.FaceAnimation.TrySetExpression(CharacterExpression.Smile), Is.True);
            for (int i = 0; i < 10; i++) world.UpdatePhysics(.008f);
            yield return null;
            Assert.That(Layer("Equipment_FaceAccessory_default").sprite.name, Does.Contain("/smile/"));
            var saved = world.CaptureProgress(); Assert.That(world.TryRestoreProgress(saved, out var restored), Is.True, restored);
            foreach (var slot in new[] { EquipSlot.FaceAccessory, EquipSlot.EyeAccessory, EquipSlot.Earring })
                Assert.That(player.GetEquippedItems().ContainsKey(slot), Is.True);
            player.Position = new LogicVector(player.Position.X - 2, player.Position.Y);
            for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
            yield return null; yield return null;
            foreach (int hat in new[] { 1002014, 1002000, 1002025 })
            {
                Equip(world, hat); yield return null;
                actor.enabled = false; Set(actor, "currentState", CharacterState.Stand); Set(actor, "currentFrame", 0); Invoke(actor, "UpdateSprites");
                Assert.That(Layer("HairOverHead").sprite != null, Is.EqualTo(hat == 1002014));
                if (hat == 1002014) Assert.That(Layer("HairOverHead").sortingOrder, Is.GreaterThan(Layer("Equipment_Hat_default").sortingOrder));
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-hat-" + hat + "-front", 1366, 768);
                InventorySceneSmokeTests.Capture(player, "-hat-" + hat + "-front-close");
                Set(actor, "currentState", CharacterState.Ladder); Invoke(actor, "UpdateSprites");
                Assert.That(Layer("Equipment_FaceAccessory_default").sprite, Is.Null);
                Assert.That(Layer("Equipment_EyeAccessory_default").sprite, Is.Null);
                if (hat == 1002025) Assert.That(Layer("Hair").sprite, Is.Null);
                else Assert.That(Layer("Hair").sprite.name, Does.Contain(hat == 1002000 ? "backHairBelowCap" : "backHair"));
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-hat-" + hat + "-back", 1366, 768);
                InventorySceneSmokeTests.Capture(player, "-hat-" + hat + "-back-close");
                actor.enabled = true; actor.UpdateAppearance();
            }
            Equip(world, 1002014);
            world.LoadMap(100000102); yield return null; yield return null;
            Beside(world, WorldLabelAnchor.Active.Single(a => a.Kind == WorldLabelAnchor.ActorKind.Npc));
            Object.FindFirstObjectByType<LocalPlayMenu>().OpenShop(); yield return null; yield return null;
            var portrait = GameObject.Find("ShopPlayer").GetComponent<ClassicPlayerPortrait>(); portrait.Present();
            Assert.That(portrait.transform.Find("Equipment_EyeAccessory_default").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(portrait.transform.Find("HairOverHead").GetSiblingIndex(), Is.GreaterThan(portrait.transform.Find("Equipment_Hat_default").GetSiblingIndex()));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-accessory-shop-portrait", 1366, 768);
        }

        [UnityTest] public IEnumerator ReferenceMapsRenderWithTheirOriginalWindowsAndLayeredActors()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var world = Pause(); var player = world.Player;
            player.Level = 100; player.JobId = 100; player.STR = player.DEX = 100;
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000, 1002014, 1022000 }) Equip(world, id);
            world.RequestPracticeSupplies(out _);
            var bag = Object.FindFirstObjectByType<InventoryView>();
            var skills = Object.FindFirstObjectByType<SkillMenu>();
            var stats = Object.FindFirstObjectByType<CharacterProgressionView>();
            var menu = Object.FindFirstObjectByType<LocalPlayMenu>();
            var talk = Object.FindFirstObjectByType<ClassicNpcDialogue>();
            var tooltip = ClassicTooltipView.For(bag.transform);
            // The shop catalog is only implemented for Luna. Freeze a real populated shop for
            // the two visual reference previews; this never enables transactions at other NPCs.
            world.LoadMap(100000102); yield return null; yield return null;
            Beside(world, WorldLabelAnchor.Active.Single(a => a.Kind == WorldLabelAnchor.ActorKind.Npc));
            menu.OpenShop(); yield return null; yield return null;
            var shop = GameObject.Find("LocalShopPanel"); Assert.That(shop, Is.Not.Null);
            menu.enabled = false; shop.SetActive(false);
            int[] maps = { 200010300, 110020001, 101000000, 200000002, 200000000, 200080700, 211000101, 101000300, 102040001 };
            for (int index = 0; index < maps.Length; index++)
            {
                bag.Show(false); skills.Show(false); stats.Show(false); talk.Close(); tooltip.Hide(); shop.SetActive(false);
                world.LoadMap(maps[index]); yield return null; yield return null;
                Object.FindFirstObjectByType<ClassicMinimapView>().SetMode(index < 2 ? 1 : 2);
                player.MaxHP = 100000; player.SetHPMP(player.MaxHP, player.MaxMP);
                for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
                var npcs = WorldLabelAnchor.Active.Where(a => a.Kind == WorldLabelAnchor.ActorKind.Npc).ToArray();
                if (index == 0 && world.Monsters.Count > 0)
                {
                    var monster = world.Monsters.OrderBy(m => m.Position.Y).First();
                    player.ResetMovementForMap(); player.Position = new LogicVector(monster.Position.X + .5f, monster.Position.Y + .5f);
                    for (int i = 0; i < 100; i++) world.UpdatePhysics(.008f);
                }
                if (index == 1) { skills.Practice(true); player.JobId = 220; skills.SetPlayer(player); skills.Show(true); }
                if (index == 4 || index == 5) { bag.ShowBag(true); }
                if (index == 8) { player.JobId = 100; stats.Show(true); }
                if (index == 2 || index == 7)
                {
                    Assert.That(npcs.Length, Is.GreaterThan(0));
                    var npc = index == 7 ? npcs.FirstOrDefault(a => a.Label.Contains("Cherry")) ?? npcs[0] : npcs.FirstOrDefault(a => a.Label.Contains("Betty")) ?? npcs[0];
                    Beside(world, npc, index == 2 ? -.1f : .1f); yield return null;
                    player.SetHPMP(player.MaxHP, player.MaxMP);
                    yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-ground-" + maps[index], 1366, 768);
                    Assert.That(talk.TryOpen(talk.SpawnFor(npc)), Is.True, npc.Label + " feet=" + npc.Feet + " player=" + player.Position + " state=" + player.State);
                    // A read-only dialogue layout fixture; unported NPC scripts remain unavailable.
                    Invoke(talk, "SayError", index == 7 ? "It looks like there's plenty of room for this ride. Please have your ticket ready so I can let you in. The ride will be long, but you'll get to your destination just fine." : "Would you do me a favor? Take my report to Cherry, she's the sailor who voyages back and forth to Ossyria. She'll know exactly where to bring it. What do you say, will you help me out?");
                    Assert.That(GameObject.Find("NpcDialoguePanel").GetComponent<RectTransform>().rect.height, Is.EqualTo(211));
                }
                if (index == 3 || index == 6)
                {
                    Assert.That(npcs.Length, Is.GreaterThan(0)); var npc = npcs.FirstOrDefault(a => a.Label == (index == 3 ? "Edel the Fairy" : "Rumi")) ?? npcs[0]; Beside(world, npc);
                    shop.SetActive(true);
                    shop.GetComponentInChildren<ClassicNpcPortrait>().Bind(npc.GetComponent<ClassicNpcAnimator>(), 54, 80, 88, 74);
                }
                yield return null; yield return null;
                Assert.That(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.sprite != null), Is.GreaterThan(15));
                Action layout = () => {
                    if (index == 5) tooltip.ShowItem(bag, new Vector2(740, 385), player, 2030000);
                    if (index == 6) tooltip.ShowItem(menu, new Vector2(640, 385), player, 1072002);
                    if (index == 3 || index == 6)
                    {
                        var margins = Camera.main.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name.StartsWith("InteriorMargin"));
                        Assert.That(margins.Count(r => r.enabled), Is.EqualTo(4));
                    }
                };
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-reference-" + (index + 1).ToString("D2"), 1366, 768, layout);
                yield return null; yield return null;
            }
            shop.SetActive(false); menu.enabled = true; tooltip.Hide();
        }

        [UnityTest] public IEnumerator NativeCaptionsAndCompactWindowsKeepLongContentInsideTheirFrames()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var world = Pause(); var player = world.Player;
            player.Name = "A very long adventurer name for layout checking"; player.JobId = 221;
            player.MaxHP = 123456789; player.SetHPMP(player.MaxHP, player.MaxMP);
            var stats = Object.FindFirstObjectByType<CharacterProgressionView>(); stats.Show(true);
            var bag = Object.FindFirstObjectByType<InventoryView>(); bag.ShowBag(true);
            yield return null; yield return null;
            for (int category = 1; category <= 5; category++)
            {
                GameObject.Find("InventoryCategory_" + category).GetComponent<Button>().onClick.Invoke();
                yield return null;
                foreach (var art in GameObject.Find("InventoryPanel").GetComponentsInChildren<Image>().Where(i => i.name == "CategoryLabel"))
                {
                    Assert.That(art.color, Is.EqualTo(Color.white));
                    Assert.That(art.rectTransform.rect.size, Is.EqualTo(art.sprite.rect.size), "Tab selection must not stretch the differently sized native caption.");
                }
            }
            var name = GameObject.Find("StatName").GetComponent<Text>();
            Assert.That(name.text, Does.EndWith("…")); Assert.That(name.preferredWidth, Is.LessThanOrEqualTo(name.rectTransform.rect.width + .1f));
            Assert.That(GameObject.Find("StatJob").GetComponent<Text>().text, Is.EqualTo("MAGICIAN"));
            Assert.That(StatusBar.JobName(221), Is.EqualTo("Mage (Ice/Lightning)"));
            foreach (string key in new[] { "Evasion", "CritRate", "CritDamage" })
            {
                var caption = GameObject.Find(key + "Label").GetComponent<Text>();
                Assert.That(caption.preferredWidth * caption.transform.localScale.x, Is.LessThanOrEqualTo(58.1f));
            }
            bag.Show(false); stats.Show(true);
            Assert.That(GameObject.Find("HP Text").GetComponent<Text>().text, Does.EndWith("…"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-long-window-content", 1366, 768);
            bag.Show(false); stats.Show(false);
            var npc = WorldLabelAnchor.Active.First(a => a.Kind == WorldLabelAnchor.ActorKind.Npc && a.NpcId == 1012111);
            Beside(world, npc); yield return null;
            var talk = Object.FindFirstObjectByType<ClassicNpcDialogue>(); Assert.That(talk.TryOpen(talk.SpawnFor(npc)), Is.True);
            Invoke(talk, "SayError", string.Join("\n", Enumerable.Repeat("A long dialogue must scroll inside the paper and keep its buttons available.", 24)));
            yield return null; yield return null;
            var speech = GameObject.Find("NpcSpeech").GetComponent<ScrollRect>();
            Assert.That(speech.content.rect.height, Is.GreaterThan(speech.viewport.rect.height));
            Assert.That(speech.verticalScrollbar.gameObject.activeSelf, Is.True);
            speech.verticalNormalizedPosition = 0;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-long-dialogue-small", 640, 480, () => {
                Assert.That(speech.verticalNormalizedPosition, Is.EqualTo(0).Within(.001f));
                Assert.That(GameObject.Find("NpcOkay").GetComponent<Button>().interactable, Is.True);
                Assert.That(GameObject.Find("NpcDialoguePanel").GetComponent<RectTransform>().rect.height, Is.EqualTo(211));
            });
            talk.Close();
        }
    }
}
