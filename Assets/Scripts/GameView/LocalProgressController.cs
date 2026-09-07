using System.IO;
using MapleClient.GameData;
using MapleClient.GameView.UI;
using UnityEngine;

namespace MapleClient.GameView
{
    public sealed class LocalProgressController : MonoBehaviour
    {
        // Explicit Save/Load keep generated map testing independent of a previous character.
        public string SaveFilePath { get; set; }
        private GameManager manager;
        private void Awake()
        {
            manager = GetComponent<GameManager>();
            SaveFilePath = Path.Combine(Application.persistentDataPath, "offline", "character-v1.json");
        }
        public bool TrySave(out string message)
        {
            var save = manager.World?.CaptureProgress();
            if (save != null)
            {
                var bar = FindFirstObjectByType<SkillBar>();
                save.Hotbar = bar?.CaptureSlots() ?? new int[0];
                save.Quickslots = bar?.CaptureBindings() ?? new MapleClient.GameLogic.Core.QuickslotBinding[0];
            }
            return new LocalSaveStore(SaveFilePath).TryWrite(save, out message);
        }
        public bool TryLoad(out string message)
        {
            if (!new LocalSaveStore(SaveFilePath).TryRead(out var save, out message)) return false;
            if (!manager.World.TryRestoreProgress(save, out message)) return false;
            var bar = FindFirstObjectByType<SkillBar>();
            // GameWorld restores/migrates the complete keyboard atomically with the character.
            bar?.RefreshBindings();
            return true;
        }
    }
}
