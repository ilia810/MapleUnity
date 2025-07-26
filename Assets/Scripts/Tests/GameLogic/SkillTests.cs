using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Skills;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;

namespace Tests.GameLogic
{
    [TestFixture]
    public class SkillTests
    {
        private Player player;
        private SkillManager skillManager;
        private MockAssetProvider assetProvider;
        
        [SetUp]
        public void Setup()
        {
            player = new Player(1, "TestPlayer", 100, 50);
            player.Level = 10;
            player.JobId = 110; // Fighter
            
            assetProvider = new MockAssetProvider();
            skillManager = new SkillManager(player, assetProvider, null);
        }
        
        [Test]
        public void TestLearnSkill()
        {
            // Arrange
            int skillId = 1101004; // Sword Booster
            
            // Act
            bool result = skillManager.LearnSkill(skillId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, skillManager.GetSkillLevel(skillId));
        }
        
        [Test]
        public void TestLearnSkill_AlreadyMaxLevel()
        {
            // Arrange
            int skillId = 1101004;
            skillManager.LearnSkill(skillId);
            for (int i = 0; i < 19; i++) // Level to max (20)
            {
                skillManager.LevelUpSkill(skillId);
            }
            
            // Act
            bool result = skillManager.LevelUpSkill(skillId);
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(20, skillManager.GetSkillLevel(skillId));
        }
        
        [Test]
        public void TestUseSkill_AttackSkill()
        {
            // Arrange
            int skillId = 1101005; // Power Strike
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 50;
            
            // Act
            var result = skillManager.UseSkill(skillId);
            
            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(SkillType.Attack, result.SkillType);
            Assert.Greater(result.Damage, 0);
            Assert.Less(player.CurrentMP, 50); // MP was consumed
        }
        
        [Test]
        public void TestUseSkill_NotEnoughMP()
        {
            // Arrange
            int skillId = 1101005;
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 5; // Not enough MP
            
            // Act
            var result = skillManager.UseSkill(skillId);
            
            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Not enough MP", result.ErrorMessage);
        }
        
        [Test]
        public void TestUseSkill_OnCooldown()
        {
            // Arrange
            int skillId = 1111002; // Combo Attack (has cooldown)
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 100;
            
            // Act
            var result1 = skillManager.UseSkill(skillId);
            var result2 = skillManager.UseSkill(skillId); // Try to use immediately
            
            // Assert
            Assert.IsTrue(result1.Success);
            Assert.IsFalse(result2.Success);
            Assert.AreEqual("Skill is on cooldown", result2.ErrorMessage);
        }
        
        [Test]
        public void TestBuffSkill()
        {
            // Arrange
            int skillId = 1101004; // Sword Booster (buff)
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 50;
            
            // Act
            var result = skillManager.UseSkill(skillId);
            
            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(SkillType.Buff, result.SkillType);
            Assert.IsTrue(skillManager.IsBuffActive(skillId));
            Assert.Greater(result.Duration, 0);
        }
        
        [Test]
        public void TestBuffExpiration()
        {
            // Arrange
            int skillId = 1101004;
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 50;
            skillManager.UseSkill(skillId);
            
            // Act - simulate time passing
            skillManager.Update(1000f); // Update for 1000 seconds (buff should expire)
            
            // Assert
            Assert.IsFalse(skillManager.IsBuffActive(skillId));
        }
        
        [Test]
        public void TestPassiveSkill()
        {
            // Arrange
            int skillId = 1100000; // Improved HP Recovery (passive)
            int initialMaxHP = player.MaxHP;
            
            // Act
            skillManager.LearnSkill(skillId);
            
            // Assert
            Assert.Greater(player.MaxHP, initialMaxHP); // Passive should increase max HP
            Assert.AreEqual(1, skillManager.GetSkillLevel(skillId));
        }
        
        [Test]
        public void TestGetAvailableSkills()
        {
            // Arrange
            player.JobId = 110; // Fighter
            player.Level = 15;
            
            // Act
            var availableSkills = skillManager.GetAvailableSkills();
            
            // Assert
            Assert.Greater(availableSkills.Count, 0);
            Assert.IsTrue(availableSkills.ContainsKey(1101004)); // Sword Booster
            Assert.IsTrue(availableSkills.ContainsKey(1101005)); // Power Strike
        }
        
        [Test]
        public void TestSkillRequirements()
        {
            // Arrange
            int skillId = 1101007; // Rage (requires level 20)
            player.Level = 10; // Too low level
            
            // Act
            bool canLearn = skillManager.CanLearnSkill(skillId);
            
            // Assert
            Assert.IsFalse(canLearn);
            
            // Level up and try again
            player.Level = 20;
            canLearn = skillManager.CanLearnSkill(skillId);
            Assert.IsTrue(canLearn);
        }
        
        [Test]
        public void TestCooldownUpdate()
        {
            // Arrange
            int skillId = 1111002; // Skill with 10 second cooldown
            skillManager.LearnSkill(skillId);
            player.CurrentMP = 100;
            skillManager.UseSkill(skillId);
            
            // Act
            skillManager.Update(5f); // 5 seconds passed
            var result1 = skillManager.UseSkill(skillId);
            
            skillManager.Update(6f); // 6 more seconds (total 11)
            var result2 = skillManager.UseSkill(skillId);
            
            // Assert
            Assert.IsFalse(result1.Success); // Still on cooldown
            Assert.IsTrue(result2.Success); // Cooldown expired
        }
        
        [Test]
        public void TestSkillDamageCalculation()
        {
            // Arrange
            int skillId = 1101005; // Power Strike
            skillManager.LearnSkill(skillId);
            player.STR = 50;
            player.WeaponAttack = 30;
            
            // Act
            var result1 = skillManager.UseSkill(skillId);
            
            // Level up skill
            skillManager.LevelUpSkill(skillId);
            var result2 = skillManager.UseSkill(skillId);
            
            // Assert
            Assert.Greater(result2.Damage, result1.Damage); // Higher level = more damage
        }
        
        private class MockAssetProvider : IAssetProvider
        {
            private MockSkillDataProvider skillProvider = new MockSkillDataProvider();
            
            public IItemDataProvider ItemData => null;
            public IMobDataProvider MobData => null;
            public ISkillDataProvider SkillData => skillProvider;
            public INpcDataProvider NpcData => null;
            public IMapDataProvider MapData => null;
            public ICharacterDataProvider CharacterData => null;
            public ISoundDataProvider SoundData => null;
            
            public void Initialize() { }
            public void Shutdown() { }
            
            private class MockSkillDataProvider : ISkillDataProvider
            {
                private Dictionary<int, SkillInfo> skills = new Dictionary<int, SkillInfo>();
                
                public MockSkillDataProvider()
                {
                    // Create mock skills
                    CreateMockSkill(1100000, "Improved HP Recovery", SkillType.Passive, 0, 0);
                    CreateMockSkill(1101004, "Sword Booster", SkillType.Buff, 0, 120);
                    CreateMockSkill(1101005, "Power Strike", SkillType.Attack, 15, 0);
                    CreateMockSkill(1101007, "Rage", SkillType.Buff, 0, 180, requiredLevel: 20);
                    CreateMockSkill(1111002, "Combo Attack", SkillType.Attack, 20, 10);
                }
                
                private void CreateMockSkill(int id, string name, SkillType type, int mpCost, int cooldown, int requiredLevel = 10)
                {
                    var skill = new SkillInfo
                    {
                        SkillId = id,
                        Name = name,
                        Type = type,
                        MaxLevel = 20,
                        IsPassive = type == SkillType.Passive,
                        JobId = id / 10000 * 100,
                        Levels = new Dictionary<int, SkillInfo.LevelData>()
                    };
                    
                    for (int i = 1; i <= 20; i++)
                    {
                        skill.Levels[i] = new SkillInfo.LevelData
                        {
                            MpCost = mpCost + i,
                            Damage = type == SkillType.Attack ? 100 + i * 20 : 0,
                            Duration = type == SkillType.Buff ? cooldown * 1000 : 0,
                            Cooldown = cooldown * 1000,
                            AttackCount = 1,
                            MobCount = type == SkillType.Attack ? 3 : 0,
                            Buffs = new Dictionary<BuffType, int>()
                        };
                        
                        if (type == SkillType.Buff && id == 1101004) // Sword Booster
                        {
                            skill.Levels[i].Buffs[BuffType.Speed] = 2 + i / 10;
                        }
                    }
                    
                    skills[id] = skill;
                }
                
                public SkillInfo GetSkill(int skillId)
                {
                    skills.TryGetValue(skillId, out var skill);
                    return skill;
                }
                
                public Dictionary<int, SkillInfo> GetSkillsForJob(int jobId)
                {
                    var result = new Dictionary<int, SkillInfo>();
                    foreach (var kvp in skills)
                    {
                        if (kvp.Value.JobId == jobId)
                            result[kvp.Key] = kvp.Value;
                    }
                    return result;
                }
                
                public bool SkillExists(int skillId)
                {
                    return skills.ContainsKey(skillId);
                }
            }
        }
    }
}