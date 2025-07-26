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
        private float buffTimeRemaining;
        private bool isActive;
        
        public int SkillId => skillInfo.SkillId;
        public string Name => skillInfo.Name;
        public SkillType Type => skillInfo.Type;
        public bool IsPassive => skillInfo.IsPassive;
        public int CurrentLevel => currentLevel;
        public int MaxLevel => skillInfo.MaxLevel;
        public bool IsMaxLevel => currentLevel >= skillInfo.MaxLevel;
        public bool IsOnCooldown => cooldownRemaining > 0;
        public bool IsBuffActive => isActive && buffTimeRemaining > 0;
        public float CooldownRemaining => cooldownRemaining;
        public float BuffTimeRemaining => buffTimeRemaining;
        
        public Skill(SkillInfo info, int level = 0)
        {
            this.skillInfo = info;
            this.currentLevel = level;
            this.cooldownRemaining = 0;
            this.buffTimeRemaining = 0;
            this.isActive = false;
        }
        
        public bool LevelUp()
        {
            if (currentLevel >= skillInfo.MaxLevel)
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
        
        public void ActivateBuff()
        {
            var levelData = GetCurrentLevelData();
            if (levelData != null && Type == SkillType.Buff)
            {
                isActive = true;
                buffTimeRemaining = levelData.Duration / 1000f; // Convert ms to seconds
            }
        }
        
        public void DeactivateBuff()
        {
            isActive = false;
            buffTimeRemaining = 0;
        }
        
        public void Update(float deltaTime)
        {
            // Update cooldown
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= deltaTime;
                if (cooldownRemaining < 0)
                    cooldownRemaining = 0;
            }
            
            // Update buff duration
            if (isActive && buffTimeRemaining > 0)
            {
                buffTimeRemaining -= deltaTime;
                if (buffTimeRemaining <= 0)
                {
                    DeactivateBuff();
                }
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