using System;
using System.Linq;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameLogic.Core
{
    public partial class GameWorld
    {
        public static int[] SupportPracticeSkills(int job)
        {
            switch (job)
            {
                case 100: return new[] { 1001003 };
                case 200: return new[] { 2001002, 2001003 };
                case 110: return new[] { 1101006, 1001003 };
                case 130: return new[] { 1301006, 1301007, 1001003 };
                case 210: return new[] { 2101001, 2001002, 2001003 };
                case 220: return new[] { 2201001, 2001002, 2001003 };
                case 300: return new[] { 3001003 };
                case 400: return new[] { 4001003 };
                case 410: return new[] { 4101004 };
                case 420: return new[] { 4201003 };
                default: return Array.Empty<int>();
            }
        }

        public bool RequestSupportPractice(int job, out string message)
        {
            message = "Choose support practice while alive and idle in local play.";
            if (networkClient != null || skillManager == null || !player.CanChangeProgression) return false;
            var ids = SupportPracticeSkills(job);
            if (ids.Length == 0) return false;
            var entries = ids.Select(id => assetProvider.SkillData.GetSkill(id)).ToArray();
            if (entries.Any(s => s?.IsSourceData != true || !SourceSkillRules.IsSupportBuff(s.SkillId) ||
                s.ActionDelays == null || s.ActionDelays.Length == 0 || !s.Levels.ContainsKey(1)))
            { message = "Support skill data is unavailable."; return false; }
            // Explicit practice preset, not a job advancement or a repeatable supply
            // reward. Keep earned EXP, resources, AP/SP, equipment and existing skills.
            player.JobId = job;
            foreach (var skill in entries) skillManager.SetSkillLevel(skill.SkillId, Math.Max(1, skillManager.GetSkillLevel(skill.SkillId)));
            message = "Level-1 support practice ready. Cast from the book or quickslots; potions restore MP.";
            return true;
        }
    }
}
