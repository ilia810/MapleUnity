using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;
using SkillType = MapleClient.GameLogic.Interfaces.SkillType;
using BuffType = MapleClient.GameLogic.Interfaces.BuffType;

namespace MapleClient.GameLogic.Skills
{
    public class Skill
    {
        private readonly SkillInfo skillInfo;
        private int currentLevel;
        private float cooldownRemaining;
        
        public int SkillId => skillInfo.SkillId;
        public SkillInfo Info => skillInfo;
        public string Name => skillInfo.Name;
        public SkillType Type => skillInfo.Type;
        public bool IsPassive => skillInfo.IsPassive;
        public int CurrentLevel => currentLevel;
        public int MaxLevel => skillInfo.MaxLevel > 0 || skillInfo.IsSourceData ? skillInfo.MaxLevel : skillInfo.Levels.Count;
        public bool IsMaxLevel => currentLevel >= MaxLevel;
        public bool IsOnCooldown => cooldownRemaining > 0;
        public float CooldownRemaining => cooldownRemaining;
        
        public Skill(SkillInfo info, int level = 0)
        {
            this.skillInfo = info;
            this.currentLevel = level;
            this.cooldownRemaining = 0;
        }
        
        public bool LevelUp()
        {
            if (currentLevel >= MaxLevel || !skillInfo.Levels.ContainsKey(currentLevel + 1))
                return false;
                
            currentLevel++;
            return true;
        }
        
        public SkillInfo.LevelData GetCurrentLevelData()
        {
            if (currentLevel == 0 || !skillInfo.Levels.ContainsKey(currentLevel))
                return null;
                
            return skillInfo.Levels[currentLevel];
        }
        
        public int GetMPCost()
        {
            var levelData = GetCurrentLevelData();
            return levelData?.MpCost ?? 0;
        }
        
        public int GetDamage()
        {
            var levelData = GetCurrentLevelData();
            return levelData?.Damage ?? 0;
        }
        
        public Dictionary<BuffType, int> GetBuffs()
        {
            var levelData = GetCurrentLevelData();
            return levelData?.Buffs ?? new Dictionary<BuffType, int>();
        }
        
        public void StartCooldown()
        {
            var levelData = GetCurrentLevelData();
            if (levelData != null)
            {
                cooldownRemaining = levelData.Cooldown / 1000f; // Convert ms to seconds
            }
        }
        
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            // Only learned level and cooldown live here. Player owns all active buffs.
            // Update cooldown
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= deltaTime;
                if (cooldownRemaining < 0)
                    cooldownRemaining = 0;
            }
            
        }

        public bool CanUse(int playerMP)
        {
            if (currentLevel == 0)
                return false;
                
            if (IsOnCooldown)
                return false;
                
            if (playerMP < GetMPCost())
                return false;
                
            return true;
        }
    }
}
