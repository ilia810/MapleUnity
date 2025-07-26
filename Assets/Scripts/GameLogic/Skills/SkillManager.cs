using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Core;
using SkillType = MapleClient.GameLogic.Interfaces.SkillType;
using BuffType = MapleClient.GameLogic.Interfaces.BuffType;

namespace MapleClient.GameLogic.Skills
{
    public class SkillManager
    {
        private readonly Player player;
        private readonly IAssetProvider assetProvider;
        private readonly INetworkClient networkClient;
        private readonly Dictionary<int, Skill> learnedSkills;
        private readonly List<ActiveBuff> activeBuffs;
        
        public SkillManager(Player player, IAssetProvider assetProvider, INetworkClient networkClient = null)
        {
            this.player = player;
            this.assetProvider = assetProvider;
            this.networkClient = networkClient;
            this.learnedSkills = new Dictionary<int, Skill>();
            this.activeBuffs = new List<ActiveBuff>();
        }
        
        public bool LearnSkill(int skillId)
        {
            if (learnedSkills.ContainsKey(skillId))
                return false;
                
            var skillInfo = assetProvider.SkillData.GetSkill(skillId);
            if (skillInfo == null)
                return false;
                
            if (!CanLearnSkill(skillId))
                return false;
                
            var skill = new Skill(skillInfo, 1);
            learnedSkills[skillId] = skill;
            
            // Apply passive effects immediately
            if (skill.IsPassive)
            {
                ApplyPassiveEffects(skill);
            }
            
            return true;
        }
        
        public bool LevelUpSkill(int skillId)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill))
                return false;
                
            if (skill.IsMaxLevel)
                return false;
                
            bool result = skill.LevelUp();
            
            // Re-apply passive effects with new level
            if (result && skill.IsPassive)
            {
                ApplyPassiveEffects(skill);
            }
            
            return result;
        }
        
        public SkillUseResult UseSkill(int skillId)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill))
            {
                return new SkillUseResult { Success = false, ErrorMessage = "Skill not learned" };
            }
            
            if (skill.IsPassive)
            {
                return new SkillUseResult { Success = false, ErrorMessage = "Cannot use passive skills" };
            }
            
            if (!skill.CanUse(player.CurrentMP))
            {
                if (skill.IsOnCooldown)
                    return new SkillUseResult { Success = false, ErrorMessage = "Skill is on cooldown" };
                else
                    return new SkillUseResult { Success = false, ErrorMessage = "Not enough MP" };
            }
            
            // Consume MP
            player.CurrentMP -= skill.GetMPCost();
            
            // Start cooldown
            skill.StartCooldown();
            
            // Send to network if connected
            if (networkClient != null && networkClient.IsConnected)
            {
                networkClient.SendUseSkill(skillId, (byte)skill.CurrentLevel);
            }
            
            var result = new SkillUseResult
            {
                Success = true,
                SkillType = skill.Type,
                SkillId = skillId
            };
            
            // Handle different skill types
            switch (skill.Type)
            {
                case SkillType.Attack:
                    result.Damage = CalculateDamage(skill);
                    result.AttackCount = skill.GetCurrentLevelData()?.AttackCount ?? 1;
                    result.MobCount = skill.GetCurrentLevelData()?.MobCount ?? 1;
                    break;
                    
                case SkillType.Buff:
                    ApplyBuff(skill);
                    result.Duration = skill.GetCurrentLevelData()?.Duration ?? 0;
                    result.Buffs = skill.GetBuffs();
                    break;
                    
                case SkillType.Recovery:
                    ApplyRecovery(skill);
                    break;
            }
            
            return result;
        }
        
        public int GetSkillLevel(int skillId)
        {
            return learnedSkills.TryGetValue(skillId, out var skill) ? skill.CurrentLevel : 0;
        }
        
        public bool IsBuffActive(int skillId)
        {
            return learnedSkills.TryGetValue(skillId, out var skill) && skill.IsBuffActive;
        }
        
        public Dictionary<int, SkillInfo> GetAvailableSkills()
        {
            var allJobSkills = assetProvider.SkillData.GetSkillsForJob(player.JobId);
            var availableSkills = new Dictionary<int, SkillInfo>();
            
            foreach (var kvp in allJobSkills)
            {
                if (CanLearnSkill(kvp.Key) || learnedSkills.ContainsKey(kvp.Key))
                {
                    availableSkills[kvp.Key] = kvp.Value;
                }
            }
            
            return availableSkills;
        }
        
        public bool CanLearnSkill(int skillId)
        {
            var skillInfo = assetProvider.SkillData.GetSkill(skillId);
            if (skillInfo == null)
                return false;
                
            // Check job
            if (skillInfo.JobId != player.JobId)
                return false;
                
            // Check level requirement (simplified - normally would check skill-specific requirements)
            int requiredLevel = skillId % 100 == 7 ? 20 : 10; // Example: skills ending in 7 require level 20
            if (player.Level < requiredLevel)
                return false;
                
            return true;
        }
        
        public void Update(float deltaTime)
        {
            // Update all skills
            foreach (var skill in learnedSkills.Values)
            {
                skill.Update(deltaTime);
            }
            
            // Update active buffs
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (!buff.Skill.IsBuffActive)
                {
                    RemoveBuff(buff);
                    activeBuffs.RemoveAt(i);
                }
            }
        }
        
        private int CalculateDamage(Skill skill)
        {
            var levelData = skill.GetCurrentLevelData();
            if (levelData == null)
                return 0;
                
            // Basic damage formula (simplified)
            int baseDamage = levelData.Damage;
            int statBonus = player.STR * 4 + player.DEX; // For warrior skills
            int weaponBonus = player.WeaponAttack * 2;
            
            return baseDamage + (statBonus + weaponBonus) * baseDamage / 100;
        }
        
        private void ApplyBuff(Skill skill)
        {
            skill.ActivateBuff();
            
            var buff = new ActiveBuff
            {
                Skill = skill,
                Buffs = skill.GetBuffs()
            };
            
            activeBuffs.Add(buff);
            
            // Apply buff effects to player
            foreach (var kvp in buff.Buffs)
            {
                switch (kvp.Key)
                {
                    case BuffType.WeaponAttack:
                        player.WeaponAttack += kvp.Value;
                        break;
                    case BuffType.Speed:
                        player.Speed += kvp.Value;
                        break;
                    case BuffType.Jump:
                        player.JumpPower += kvp.Value;
                        break;
                    // Add more buff types as needed
                }
            }
        }
        
        private void RemoveBuff(ActiveBuff buff)
        {
            // Remove buff effects from player
            foreach (var kvp in buff.Buffs)
            {
                switch (kvp.Key)
                {
                    case BuffType.WeaponAttack:
                        player.WeaponAttack -= kvp.Value;
                        break;
                    case BuffType.Speed:
                        player.Speed -= kvp.Value;
                        break;
                    case BuffType.Jump:
                        player.JumpPower -= kvp.Value;
                        break;
                }
            }
        }
        
        private void ApplyRecovery(Skill skill)
        {
            var levelData = skill.GetCurrentLevelData();
            if (levelData == null)
                return;
                
            if (levelData.Hp > 0)
                player.Heal(levelData.Hp);
            if (levelData.HpR > 0)
                player.Heal(player.MaxHP * levelData.HpR / 100);
            if (levelData.Mp > 0)
                player.RestoreMana(levelData.Mp);
            if (levelData.MpR > 0)
                player.RestoreMana(player.MaxMP * levelData.MpR / 100);
        }
        
        private void ApplyPassiveEffects(Skill skill)
        {
            // Simple example for HP recovery passive
            if (skill.SkillId == 1100000) // Improved HP Recovery
            {
                player.MaxHP += skill.CurrentLevel * 20;
            }
        }
        
        private class ActiveBuff
        {
            public Skill Skill { get; set; }
            public Dictionary<BuffType, int> Buffs { get; set; }
        }
    }
    
    public class SkillUseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public SkillType SkillType { get; set; }
        public int SkillId { get; set; }
        public int Damage { get; set; }
        public int AttackCount { get; set; }
        public int MobCount { get; set; }
        public int Duration { get; set; }
        public Dictionary<BuffType, int> Buffs { get; set; }
    }
}