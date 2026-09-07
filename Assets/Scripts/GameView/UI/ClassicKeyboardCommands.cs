using System;
using System.IO;
using MapleClient.GameLogic.Core;
using UnityEngine;

namespace MapleClient.GameView.UI
{
    public sealed partial class SkillBar
    {
        public string LastScreenshotPath { get; private set; }
        internal static string CommandName(QuickslotAction action)
        {
            switch (action)
            {
                case QuickslotAction.Equipment: return "Equipment";
                case QuickslotAction.Inventory: return "Item inventory";
                case QuickslotAction.Stats: return "Character stats";
                case QuickslotAction.Skills: return "Skill inventory";
                case QuickslotAction.WorldMap: return "World map";
                case QuickslotAction.Minimap: return "Minimap mode";
                case QuickslotAction.Quests: return "Quest journal";
                case QuickslotAction.Keyboard: return "Keyboard setting";
                case QuickslotAction.Menu: return "Menu / close front window";
                case QuickslotAction.Quickslots: return "Show / hide quickslots";
                case QuickslotAction.Left: return "Move left";
                case QuickslotAction.Right: return "Move right";
                case QuickslotAction.Up: return "Climb up / portal";
                case QuickslotAction.Down: return "Climb down / crouch";
                case QuickslotAction.Talk: return "Talk to nearby NPC";
                case QuickslotAction.Shop: return "Nearby shop";
                case QuickslotAction.Screenshot: return "Screenshot";
                default: return action.ToString();
            }
        }
        private bool ExecuteCommand(QuickslotAction action)
        {
            switch (action)
            {
                case QuickslotAction.Equipment:
                    var gear = GetComponent<InventoryView>(); if (gear != null) gear.ShowEquipment(!gear.EquipmentVisible); return gear != null;
                case QuickslotAction.Inventory:
                    var bag = GetComponent<InventoryView>(); if (bag != null) bag.ShowBag(!bag.BagVisible); return bag != null;
                case QuickslotAction.Stats:
                    var stats = GetComponent<CharacterProgressionView>(); if (stats != null) stats.Show(!stats.Visible); return stats != null;
                case QuickslotAction.Skills:
                    var skills = GetComponent<SkillMenu>(); if (skills != null) skills.Show(!skills.Visible); return skills != null;
                case QuickslotAction.WorldMap:
                    var map = GetComponent<ClassicMinimapView>(); map?.OpenWorldMap(); return map != null;
                case QuickslotAction.Minimap:
                    var mini = GetComponent<ClassicMinimapView>(); if (mini != null) mini.SetMode((mini.Mode + 1) % 3); return mini != null;
                case QuickslotAction.Quests:
                    var quests = GetComponent<ClassicQuestView>(); if (quests != null) quests.Show(!quests.Visible); return quests != null;
                case QuickslotAction.Keyboard: ShowShortcuts(); return true;
                case QuickslotAction.Menu:
                    if (GetComponent<ClassicWindowManager>()?.TryCloseFrontmost() != true) GetComponent<LocalPlayMenu>()?.Show(true); return true;
                case QuickslotAction.Quickslots: ToggleVisible(); return true;
                case QuickslotAction.Talk: GetComponent<ClassicNpcDialogue>()?.TalkNearest(); return true;
                case QuickslotAction.Shop: GetComponent<LocalPlayMenu>()?.OpenShop(); return true;
                case QuickslotAction.Screenshot:
                    try
                    {
                        var folder = Path.Combine(Application.isBatchMode ? Application.temporaryCachePath : Application.persistentDataPath, "Screenshots");
                        Directory.CreateDirectory(folder);
                        LastScreenshotPath = Path.Combine(folder, "Maple-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                        ScreenCapture.CaptureScreenshot(LastScreenshotPath); Notify("Saving screenshot in " + folder + "."); return true;
                    }
                    catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                    { Notify("Could not save a screenshot."); return false; }
                default: return false;
            }
        }
    }
}
