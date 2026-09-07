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
        private readonly SkillCastEffects castEffects;
        private readonly IAssetProvider assetProvider;
        private readonly INetworkClient networkClient;
        private readonly SortedDictionary<int, Skill> learnedSkills;
        private MapleClient.GameLogic.Data.BasicAttackMotion castMotion;
        public event System.Action SkillsChanged;
        public IEnumerable<Skill> LearnedSkills => learnedSkills.Values;
        internal System.Func<SkillInfo, SkillInfo.LevelData, bool> StartAttackSkill;
        
        public SkillManager(Player player, IAssetProvider assetProvider, INetworkClient networkClient = null, SkillCastEffects effects = null)
        {
            this.player = player;
            castEffects = effects ?? (assetProvider?.SkillData as ISkillEffectSource)?.CastEffects ?? new SkillCastEffects();
            this.assetProvider = assetProvider;
            this.networkClient = networkClient;
            this.learnedSkills = new SortedDictionary<int, Skill>();
            player.PassiveStatsSource = CalculatePassives;
        }

        // SkillBook::set_skill replaces a learned level; it never applies cumulative base-stat edits.
        // This also accepts a server/restored book entry from another job; it stays inactive there.
        public bool SetSkillLevel(int id, int level)
        {
            if (level == 0) { bool removed = learnedSkills.Remove(id); if (removed) Changed(); return removed; }
            var info = assetProvider.SkillData.GetSkill(id);
            if (info == null || level < 0 || info.Levels?.ContainsKey(level) != true ||
                level > (info.MaxLevel > 0 || info.IsSourceData ? info.MaxLevel : info.Levels.Count)) return false;
            learnedSkills[id] = new Skill(info, level); Changed(); return true;
        }
        public bool CanSetSkillLevel(int id, int level)
        {
            var info = assetProvider.SkillData.GetSkill(id);
            return level > 0 && info?.Levels?.ContainsKey(level) == true &&
                level <= (info.MaxLevel > 0 || info.IsSourceData ? info.MaxLevel : info.Levels.Count);
        }
        internal void RestoreLearnedSkills(SavedSkill[] entries)
        {
            castMotion?.Cancel(); castMotion = null; learnedSkills.Clear();
            foreach (var entry in entries) learnedSkills[entry.Id] = new Skill(assetProvider.SkillData.GetSkill(entry.Id), entry.Level);
            Changed();
        }
        private void Changed() { player.NotifySkillStatsChanged(); SkillsChanged?.Invoke(); }
        private PassiveStats CalculatePassives()
        {
            var result = new PassiveStats();
            foreach (var skill in learnedSkills.Values)
                if (skill.IsPassive && SkillRules.JobMatches(player.JobId, skill.Info) && skill.GetCurrentLevelData() != null)
                    SkillRules.ApplyPassive(ref result, skill.Info, skill.GetCurrentLevelData(), player);
            return result;
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
                
            return SetSkillLevel(skillId, 1);
        }
        
        public bool LevelUpSkill(int skillId)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill))
                return false;
                
            if (skill.IsMaxLevel || !CanLearnSkill(skillId))
                return false;
                
            bool result = skill.LevelUp();
            
            if (result) Changed();
            
            return result;
        }
        
        public SkillInfo GetSkillInfo(int id) => assetProvider.SkillData.GetSkill(id);

        public SkillUseResult UseSkill(int skillId)
        {
            if (player.IsDead) return Failed("Cannot use skills while defeated");
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return Failed("Skill not learned");
            if (skill.IsPassive) return Failed("Cannot use passive skills");
            if (networkClient != null) return Failed("Skill casting is currently available in local play");
            var info = skill.Info; var data = skill.GetCurrentLevelData(); var behavior = info.Behavior;
            if (!SkillValidation.Level(info, data, castEffects, out var error)) return Failed(error);
            if (behavior.IsAttack && player.IsHidden) return Failed("Cancel concealment before attacking (right-click its buff icon).");
            if (!SkillRules.JobMatches(player.JobId, info)) return Failed("This skill belongs to another job");
            if (player.IsBasicAttacking || player.State == PlayerState.Climbing) return Failed("Finish the current action before casting");
            if (behavior.BlocksCrouching && player.State == PlayerState.Crouching) return Failed("Stand up before casting a ranged skill");
            if (!behavior.WeaponMatches(player.EquippedWeaponType) || info.RequiredWeaponType != 0 && player.EquippedWeaponType != info.RequiredWeaponType)
                return Failed("This skill requires a different weapon");
            if (player.CurrentHP <= data.HpCost) return Failed("Not enough HP");
            if (!skill.CanUse(player.CurrentMP)) return Failed(skill.IsOnCooldown ? "Skill is on cooldown" : "Not enough MP");
            if (behavior.UsesAmmunition && (!player.HasUsableAmmunition || player.AmmunitionCount < data.BulletConsume))
                return Failed($"Need {data.BulletConsume} ammunition in the active stack");

            // All data, effect handlers and resource requirements are validated before
            // preparing an action. NX and authored skills follow this same transaction.
            if (behavior.IsAttack)
            {
                if (StartAttackSkill?.Invoke(info, data) != true) return Failed("This skill needs a supported weapon and a local combat scene");
            }
            else if (info.ActionDelays?.Length > 0)
            {
                if (!player.PlaySkillAnimation(info)) return Failed("Skill action could not start");
                castMotion = player.BasicAttack;
            }
            else player.ShowSkillUse(info);
            player.SpendSkillHp(data.HpCost); player.CurrentMP -= data.MpCost;
            skill.StartCooldown();
            castEffects.Apply(player, info, data);
            return new SkillUseResult { Success = true, SkillType = info.Type, SkillId = skillId,
                // Damage is resolved by Combat at impact, never by a legacy return-value formula.
                Damage = 0, AttackCount = behavior.UsesAmmunition ? data.BulletCount : data.AttackCount,
                MobCount = data.MobCount, Duration = data.Duration, Buffs = data.Buffs };
        }
        private static SkillUseResult Failed(string message) => new SkillUseResult { Success = false, ErrorMessage = message };
        
        public int GetSkillLevel(int skillId)
        {
            return learnedSkills.TryGetValue(skillId, out var skill) ? skill.CurrentLevel : 0;
        }
        
        public bool IsBuffActive(int skillId)
        {
            return player.ActiveBuffs.Any(buff => buff.SourceId == skillId);
        }

        public bool TryCancelBuff(int sourceId)
        {
            if (networkClient != null || player.IsDead || !IsBuffActive(sourceId)) return false;
            player.RemoveStatBuffs(sourceId); return true;
        }
        
        public Dictionary<int, SkillInfo> GetAvailableSkills()
        {
            var allJobSkills = SourceSkillRules.JobBranches(player.JobId).Distinct()
                .SelectMany(job => assetProvider.SkillData.GetSkillsForJob(job)).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
            var availableSkills = new Dictionary<int, SkillInfo>();
            
            foreach (var kvp in allJobSkills)
            {
                // Keep prerequisite-locked skills visible so normal progression can explain the chain.
                if (SkillRules.JobMatches(player.JobId,kvp.Value) && (kvp.Value.Levels?.ContainsKey(1)==true || learnedSkills.ContainsKey(kvp.Key)))
                {
                    availableSkills[kvp.Key] = kvp.Value;
                }
            }
            
            return availableSkills;
        }

        public bool CanSpendSkillPoint(int id,out string message)
        {
            var info=assetProvider?.SkillData?.GetSkill(id);
            int family=OfflineProgression.Family(player.JobId),current=GetSkillLevel(id);
            if(networkClient!=null||!player.CanChangeProgression){message="Spend SP while alive and idle in local play.";return false;}
            if(info==null||family==0||info.JobId!=family||!SkillRules.JobMatches(player.JobId,info))
            {message="Choose a skill from your current first job.";return false;}
            var next = info.Levels?.ContainsKey(current+1)==true ? info.Levels[current+1] : info.Levels?.Values.FirstOrDefault();
            if(info.IsInvisible||!SkillValidation.Level(info,next,castEffects,out _))
            {message="This skill's effect is not implemented yet; your SP is preserved.";return false;}
            if(!CanLearnSkill(id)){message="Learn the required skill levels first.";return false;}
            if(!CanSetSkillLevel(id,current+1)){message="This skill has reached its available maximum level.";return false;}
            if(player.SkillPointsForJob(family)<=0){message="No SP available for this job. Earn another level.";return false;}
            message="";return true;
        }
        public bool TrySpendSkillPoint(int id,out string message)
        {
            if(!CanSpendSkillPoint(id,out message))return false;
            if(learnedSkills.TryGetValue(id,out var skill))
            {
                // Preserve a live cooldown when raising an existing skill.
                if(!skill.LevelUp()){message="This skill cannot be raised.";return false;}
            }
            else learnedSkills[id]=new Skill(assetProvider.SkillData.GetSkill(id),1);
            player.ConsumeSkillPoint(OfflineProgression.Family(player.JobId));Changed();
            message=$"{learnedSkills[id].Name} is now level {GetSkillLevel(id)}.";return true;
        }
        
        public bool CanLearnSkill(int skillId)
        {
            var skillInfo = assetProvider.SkillData.GetSkill(skillId);
            if (skillInfo == null)
                return false;
                
            // Check job
            if (!SkillRules.JobMatches(player.JobId, skillInfo))
                return false;
                
            return skillInfo.Levels?.ContainsKey(1) == true &&
                (skillInfo.RequiredSkills == null || skillInfo.RequiredSkills.All(r => GetSkillLevel(r.Key) >= r.Value));
        }
        
        public void Update(float deltaTime)
        {
            if (player.SkillEffect != null)
            {
                player.SkillEffect.Advance(deltaTime);
                if (player.IsDead || player.SkillEffect.Sample(out _) == null) player.SkillEffect = null;
            }
            if (castMotion != null)
            {
                if (player.IsDead || !ReferenceEquals(player.BasicAttack, castMotion)) castMotion.Cancel();
                castMotion.Advance(deltaTime);
                if (castMotion.IsComplete) castMotion = null;
            }
            // Update all skills
            foreach (var skill in learnedSkills.Values)
            {
                skill.Update(deltaTime);
            }
            
            // Player expires stat buffs on its 8 ms simulation clock.
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
