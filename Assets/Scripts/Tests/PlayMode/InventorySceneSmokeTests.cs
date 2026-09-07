using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
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
    public class InventorySceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
        }
        [UnityTest]
        public IEnumerator PracticeInventoryUsesRealPotionsAndRendersEquippedPartsAcrossActionsAndTravel()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var player = world.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            Step(world, 100);
            var inventory = Object.FindFirstObjectByType<InventoryView>(); inventory.Show(true);
            yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-inventory-empty");
            yield return Click("PracticeSupplies"); yield return null; yield return null;
            Assert.That(world.CanRequestPracticeSupplies, Is.False);
            Assert.That(player.Inventory.GetAllItems().Count, Is.EqualTo(13));
            Assert.That(world.RequestPracticeSupplies(out _), Is.False);
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(10));
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000 })
            {
                Assert.That(GameObject.Find("ItemRow_" + id).transform.Find("Icon").GetComponent<Image>().sprite, Is.Not.Null);
                yield return Click("ItemRow_" + id); yield return null;
                yield return Click("ItemAction"); yield return null; yield return null;
                Assert.That(player.GetEquippedItems().Values, Does.Contain(id));
                Assert.That(player.Inventory.GetItemCount(id), Is.Zero);
            }
            Assert.That(player.WeaponDefense, Is.EqualTo(17)); Assert.That(player.GetBaseDamage(), Is.EqualTo(37));
            yield return OpenEquipment(); yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-inventory-equipped");
            inventory.Show(false);
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            var shirt = visual.Find("Equipment_Top_mail").GetComponent<SpriteRenderer>();
            var sleeve = visual.Find("Equipment_Top_mailArm").GetComponent<SpriteRenderer>();
            Assert.That(shirt.sprite, Is.Not.Null); Assert.That(sleeve.sprite, Is.Not.Null);
            Assert.That(sleeve.sortingOrder, Is.GreaterThan(visual.Find("Arm").GetComponent<SpriteRenderer>().sortingOrder));
            Assert.That(visual.Find("DefaultBottom").GetComponent<SpriteRenderer>().sprite, Is.Null);
            Assert.That(visual.Find("Equipment_Weapon_weapon").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            Capture(player, "-equipment-standing");

            input.IsRightPressed = true; Step(world, 12); yield return null; yield return null;
            Assert.That(shirt.sprite.name, Does.Contain("/walk1/")); Capture(player, "-equipment-walking");
            input.IsRightPressed = false; Step(world, 15);
            input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false;
            yield return null; yield return null;
            Assert.That(sleeve.sprite.name, Does.Contain("/" + CharacterStances.Name(player.BasicAttack.Stance) + "/")); Capture(player, "-equipment-attack");
            for (int tick = 0; tick < 150 && player.IsBasicAttacking; tick++) Step(world, 1);
            Assert.That(player.IsBasicAttacking, Is.False, "The authored attack must finish before checking a separate jump.");
            input.IsJumpPressed = true; Step(world, 10); input.IsJumpPressed = false;
            yield return null; yield return null;
            Assert.That(shirt.sprite.name, Does.Contain("/jump/")); Capture(player, "-equipment-jump");

            inventory.Show(true); yield return null; yield return OpenBag(); yield return null;
            yield return Click("InventoryCategory_2");
            player.TakeDamage(60); player.CurrentMP = 10;
            yield return Click("ItemRow_2000001"); yield return null; yield return Click("ItemAction"); yield return null;
            Assert.That(player.CurrentHP, Is.EqualTo(player.MaxHP)); Assert.That(player.CurrentMP, Is.EqualTo(10));
            Assert.That(player.Inventory.GetItemCount(2000001), Is.EqualTo(4));
            yield return Click("ItemRow_2000003"); yield return null; yield return Click("ItemAction"); yield return null;
            Assert.That(player.CurrentMP, Is.EqualTo(player.MaxMP)); Assert.That(player.Inventory.GetItemCount(2000003), Is.EqualTo(9));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-inventory-potions");
            yield return Click("ItemAction"); yield return null; Assert.That(player.Inventory.GetItemCount(2000003), Is.EqualTo(9), "Full MP must not waste a potion.");

            yield return OpenEquipment(); yield return null; yield return Click("EquipmentRow_1040002"); yield return null;
            yield return Click("ItemAction"); yield return null; yield return null;
            Assert.That(shirt.sprite, Is.Null); Assert.That(sleeve.sprite, Is.Null);
            Assert.That(player.Inventory.GetItemCount(1040002), Is.EqualTo(1)); Assert.That(player.WeaponDefense, Is.EqualTo(14));
            yield return OpenBag(); yield return null; yield return Click("ItemRow_1040002"); yield return null; yield return Click("ItemAction"); yield return null;
            inventory.Show(false); world.LoadMap(100000001); yield return null; yield return null;
            Assert.That(player.GetEquippedItems().Count, Is.EqualTo(4)); Assert.That(player.WeaponDefense, Is.EqualTo(17));
            Assert.That(world.CanRequestPracticeSupplies, Is.False); Assert.That(shirt.sprite, Is.Not.Null);
        }
        internal static IEnumerator OpenBag()
        {
            var bag=Object.FindFirstObjectByType<InventoryView>();
            if(!bag.BagVisible)yield return Click("InventoryToggle");
            GameObject.Find("InventoryPanel").GetComponent<ClassicWindow>().Focus();
            yield return Click("InventoryCategory_1");
        }
        internal static IEnumerator OpenEquipment()
        {
            if(!Object.FindFirstObjectByType<InventoryView>().EquipmentVisible)yield return Click("EquipmentToggle");
            GameObject.Find("EquipmentPanel").GetComponent<ClassicWindow>().Focus();yield return null;
        }
        internal static IEnumerator OpenPractice()
        {
            if(GameObject.Find("PracticePanel")!=null){GameObject.Find("PracticePanel").GetComponent<ClassicWindow>().Focus();yield break;}
            if(GameObject.Find("LocalPlayPanel")==null)yield return Click("LocalPlayToggle");
            yield return Click("OpenPractice");
        }
        internal static IEnumerator RightClick(GameObject row)
        {
            Assert.That(row,Is.Not.Null,"Select a row before opening its actions.");
            row.GetComponentInParent<ClassicWindow>()?.Focus();Canvas.ForceUpdateCanvases();
            var r=row.GetComponent<RectTransform>();var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))};
            ExecuteEvents.Execute(row,pointer,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        internal static IEnumerator Click(string name)
        {
            if(name=="PracticeSupplies"||name=="PracticeSkills"||name=="PracticeMagicSkills"||name.StartsWith("RangedPreset_"))yield return OpenPractice();
            if((name=="OpenCharacterProgression"||name=="SaveLocalProgress")&&GameObject.Find("LocalPlayPanel")==null)yield return Click("LocalPlayToggle");
            yield return null;yield return null;
            if(name.StartsWith("SkillRow_"))
            {
                int id=int.Parse(name.Substring("SkillRow_".Length));
                yield return Click("SkillTier_"+MapleClient.GameLogic.Skills.SkillRules.JobTier(Object.FindFirstObjectByType<GameManager>().SkillManager.GetSkillInfo(id).JobId));
                yield return null;yield return null;
                for(int i=0;GameObject.Find(name)==null&&i<30;i++){yield return Click("NextSkills");yield return null;yield return null;}
            }
            if(name=="ItemAction")yield return RightClick(Object.FindFirstObjectByType<InventoryView>().SelectedRow);
            if(name=="CastSkill"||name=="SpendSkillPoint"||name.StartsWith("AssignSkill_"))
                yield return RightClick(Object.FindFirstObjectByType<SkillMenu>().SelectedRow);
            Assert.That(GameObject.Find(name),Is.Not.Null,name);
            var button=GameObject.Find(name).GetComponent<Button>();Assert.That(button.interactable,Is.True,name);
            // Put the requested independent window in front before testing its control's hit routing.
            button.GetComponentInParent<ClassicWindow>()?.Focus();yield return null;Canvas.ForceUpdateCanvases();
            var rect=button.GetComponent<RectTransform>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),$"{name}: pointer={pointer.position}, screen={Screen.width}x{Screen.height}");
            Assert.That(hits[0].gameObject,Is.EqualTo(button.gameObject),name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void Step(GameWorld world, int count) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }
        internal static void Capture(Player player, string suffix)
        {
            var camera = Camera.main; var position = camera.transform.position; float size = camera.orthographicSize;
            try { camera.transform.position = new Vector3(player.Position.X, player.Position.Y, -10); camera.orthographicSize = .8f; RecoverySceneSmokeTests.SaveCameraImage(camera, suffix, 640, 640); }
            finally { camera.transform.position = position; camera.orthographicSize = size; }
        }
    }
}
