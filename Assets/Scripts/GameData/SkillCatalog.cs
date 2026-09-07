using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>One catalog for original metadata and validated custom definitions.</summary>
    public sealed class SkillCatalog : ISkillDataProvider, ISkillEffectSource
    {
        private readonly NxSkillDataProvider source;
        private readonly Dictionary<int, SkillInfo> custom = new Dictionary<int, SkillInfo>();
        private readonly Dictionary<int, Dictionary<string, Sprite>> art = new Dictionary<int, Dictionary<string, Sprite>>();
        public SkillCastEffects CastEffects { get; }
        public SkillCatalog(NxSkillDataProvider source, SkillCastEffects effects = null)
        { this.source = source; CastEffects = effects ?? new SkillCastEffects(); }
        public bool TryRegister(CustomSkillAsset asset, out string error)
        {
            error = "A custom skill cannot replace an existing ID.";
            if (asset == null || custom.ContainsKey(asset.Id) || source.SkillExists(asset.Id)) return false;
            if (!asset.Compile(source, CastEffects, out var info, out var sprites, out error)) return false;
            custom.Add(info.SkillId, info); art.Add(info.SkillId, sprites); return true;
        }
        public bool RemoveCustom(int id) { art.Remove(id); return custom.Remove(id); }
        public SkillInfo GetSkill(int id) => custom.TryGetValue(id, out var info) ? info : source.GetSkill(id);
        public bool SkillExists(int id) => GetSkill(id) != null;
        public Dictionary<int, SkillInfo> GetSkillsForJob(int job)
        {
            var result = source.GetSkillsForJob(job);
            foreach (var pair in custom) if (pair.Value.JobId == job) result.Add(pair.Key, pair.Value);
            return result;
        }
        public Sprite GetSprite(string path)
        {
            if (path == null) return null;
            int slash = path.IndexOf('/');
            return slash > 0 && int.TryParse(path.Substring(0, slash), out int id) && art.TryGetValue(id, out var sprites) &&
                sprites.TryGetValue(path, out var sprite) ? sprite : null;
        }
    }
}
