using MapleClient.GameLogic.Interfaces;
using GameData;
using UnityEngine;

namespace MapleClient.GameData
{
    public static class SkillSprites
    {
        public static Sprite Icon(SkillInfo info, ISkillDataProvider provider = null) =>
            info == null ? null : Load(info.IconFile, info.IconPath, false, provider);
        public static Sprite Frame(string file, string path, ISkillDataProvider provider = null) => Load(file, path, true, provider);
        private static Sprite Load(string file, string path, bool shifted, ISkillDataProvider provider)
        {
            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(path)) return null;
            if (file == "unity") return ((provider ?? NXDataManagerSingleton.Instance.DataManager.SkillData) as SkillCatalog)?.GetSprite(path);
            var node = NXAssetLoader.Instance.GetNxFile(file)?.GetNode(path);
            return shifted ? SpriteLoader.LoadSpriteWithShift(node, Vector2.zero, file + "/" + path) : SpriteLoader.LoadSprite(node, file + "/" + path);
        }
    }
}
