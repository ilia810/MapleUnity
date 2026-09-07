using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public enum PrimaryAttribute { STR, DEX, INT, LUK }
    public sealed class FirstJobChoice
    {
        public int JobId { get; }
        public string Name { get; }
        public int Level { get; }
        public int WeaponId { get; }
        public int AmmunitionId { get; }
        public int FirstSkillId => JobId==100?1001004:JobId==200?2001004:JobId==300?3001004:JobId==400?4001344:5001003;
        public FirstJobChoice(int id,string name,int level,int weapon,int ammo=0)
        { JobId=id;Name=name;Level=level;WeaponId=weapon;AmmunitionId=ammo; }
    }
    // The source client sends AP/SP requests and receives server rewards. These are explicit
    // offline rewards, not reconstructed server formulas. See Tools/Research/Progression-source-findings.md.
    public static class OfflineProgression
    {
        public const int AttributeCap=999, AbilityPointCap=995, SkillPointCap=598;
        public static IReadOnlyList<FirstJobChoice> FirstJobs { get; } = Array.AsReadOnly(new[] {
            new FirstJobChoice(100,"Swordsman",10,1302000), new FirstJobChoice(200,"Magician",8,1372005),
            new FirstJobChoice(300,"Bowman",10,1452002,2060000), new FirstJobChoice(400,"Thief",10,1472000,2070000),
            new FirstJobChoice(500,"Pirate",10,1492000,2330000) });
        public static int Family(int job) { int family=job/100;return family>=1&&family<=5?family*100:0; }
        public static FirstJobChoice Job(int id) => FirstJobs.FirstOrDefault(j=>j.JobId==id);
        public static bool IsAttribute(PrimaryAttribute stat) => stat>=PrimaryAttribute.STR&&stat<=PrimaryAttribute.LUK;
    }
    public partial class Player
    {
        internal bool LocalProgressionEnabled { get; set; } = true;
        private int abilityPoints, highestRewardedLevel=1;
        private int[] skillPointPools=new int[5];
        public int AbilityPoints => abilityPoints;
        public bool HasChosenFirstJob { get; private set; }
        public int SkillPointsForJob(int job) { int family=OfflineProgression.Family(job);return family==0?0:skillPointPools[family/100-1]; }
        public int SkillPoints => SkillPointsForJob(JobId);
        public event Action ProgressionChanged;
        public bool CanChangeProgression => LocalProgressionEnabled&&!IsDead&&!IsBasicAttacking&&State!=PlayerState.Climbing;
        public int BaseAttribute(PrimaryAttribute stat)
        {
            switch(stat){case PrimaryAttribute.STR:return baseSTR;case PrimaryAttribute.DEX:return baseDEX;
                case PrimaryAttribute.INT:return baseINT;case PrimaryAttribute.LUK:return baseLUK;default:return 0;}
        }
        public bool CanSpendAbilityPoint(PrimaryAttribute stat) => CanChangeProgression&&OfflineProgression.IsAttribute(stat)&&
            abilityPoints>0&&BaseAttribute(stat)<OfflineProgression.AttributeCap;
        public bool TrySpendAbilityPoint(PrimaryAttribute stat,out string message)
        {
            if(!CanChangeProgression){message="Allocate points while alive and idle in local play.";return false;}
            if(!OfflineProgression.IsAttribute(stat)){message="Choose STR, DEX, INT or LUK.";return false;}
            if(abilityPoints<=0){message="Earn a level to gain AP.";return false;}
            if(BaseAttribute(stat)>=OfflineProgression.AttributeCap){message="That base attribute has reached its cap.";return false;}
            switch(stat){case PrimaryAttribute.STR:baseSTR++;break;case PrimaryAttribute.DEX:baseDEX++;break;
                case PrimaryAttribute.INT:baseINT++;break;case PrimaryAttribute.LUK:baseLUK++;break;}
            abilityPoints--;OnStatsChanged();ProgressionChanged?.Invoke();message=$"Added 1 {stat}.";return true;
        }
        private void AwardLevelProgression()
        {
            if(!LocalProgressionEnabled||level<=highestRewardedLevel)return;
            highestRewardedLevel=level;
            abilityPoints=Math.Min(OfflineProgression.AbilityPointCap,abilityPoints+5);
            int family=OfflineProgression.Family(JobId);
            if(family!=0&&level>OfflineProgression.Job(family).Level)
                skillPointPools[family/100-1]=Math.Min(OfflineProgression.SkillPointCap,skillPointPools[family/100-1]+3);
            ProgressionChanged?.Invoke();
        }
        internal void ConsumeSkillPoint(int family) { skillPointPools[family/100-1]--;ProgressionChanged?.Invoke(); }
        internal void AdvanceFirstJob(int job)
        {
            JobId=job;HasChosenFirstJob=true;
            skillPointPools[job/100-1]=Math.Min(OfflineProgression.SkillPointCap,skillPointPools[job/100-1]+1);
            OnStatsChanged();ProgressionChanged?.Invoke();
        }
        private bool CanRestoreProgression(PlayerProgress p,int version) => version==1 ||
            p.AbilityPoints>=0&&p.AbilityPoints<=OfflineProgression.AbilityPointCap&&p.HighestRewardedLevel>=1&&
            p.HighestRewardedLevel<=ExperienceTable.LevelCap&&p.SkillPointPools!=null&&p.SkillPointPools.Length==5&&
            p.SkillPointPools.All(n=>n>=0&&n<=OfflineProgression.SkillPointCap);
        private void RestoreProgression(PlayerProgress p,int version)
        {
            abilityPoints=version==1?0:p.AbilityPoints;
            highestRewardedLevel=version==1?p.Level:p.HighestRewardedLevel;
            skillPointPools=version==1?new int[5]:(int[])p.SkillPointPools.Clone();
            HasChosenFirstJob=version==1?p.Job!=0:p.HasChosenFirstJob;
        }
    }
    public partial class GameWorld
    {
        public event Action<int> FirstJobAdvanced;
        private Dictionary<int,int> FirstJobSupplies(FirstJobChoice job)
        {
            var result=new Dictionary<int,int>{{job.WeaponId,1}};
            if(job.AmmunitionId!=0)result.Add(job.AmmunitionId,100);
            return result;
        }
        public bool CanAdvanceFirstJob(int jobId,out string message)
        {
            var job=OfflineProgression.Job(jobId);
            if(!IsLocal||!player.CanChangeProgression){message="Choose a job while alive and idle in local play.";return false;}
            if(job==null){message="Choose one of the five first jobs.";return false;}
            if(player.JobId!=0||player.HasChosenFirstJob){message="This character already has a job. Later advancements are not available yet.";return false;}
            if(player.Level<job.Level){message=$"{job.Name} requires level {job.Level}.";return false;}
            var supplies=FirstJobSupplies(job);
            if(supplies.Keys.Any(id=>player.GetItemInfo(id)==null)||skillManager==null||
                assetProvider?.SkillData?.GetSkill(job.FirstSkillId)?.Levels?.ContainsKey(1)!=true)
            {message="The starter equipment or skill data is unavailable.";return false;}
            if(!player.Inventory.CanExchange(null,supplies)){message="Make room for the starter equipment and ammunition.";return false;}
            message="";return true;
        }
        public bool TryAdvanceFirstJob(int jobId,out string message)
        {
            if(!CanAdvanceFirstJob(jobId,out message))return false;
            var job=OfflineProgression.Job(jobId);
            if(!player.Inventory.TryExchange(null,FirstJobSupplies(job))){message="Make room for the starter equipment.";return false;}
            player.AdvanceFirstJob(jobId);
            FirstJobAdvanced?.Invoke(jobId);
            message=$"Became a {job.Name}! Equip the starter weapon in I, then spend your first SP in K.";return true;
        }
        public bool TrySpendAbilityPoint(PrimaryAttribute stat,out string message)
        {
            if(!IsLocal){message="Point allocation is available in local play.";return false;}
            return player.TrySpendAbilityPoint(stat,out message);
        }
    }
}
