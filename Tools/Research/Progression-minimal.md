# Progression source investigation

Issue: the Unity offline loop has EXP/levels and freely granted practice skills, but no AP/SP or earned first-job advancement. The C++ client delegates progression to a server. Identify exactly which AP/SP restrictions and beginner SP accounting are client-derived, and whether local server/NPC scripts or NX data establish first-job eligibility, point rewards, and HP/MP growth. Investigate supported first-job skills and prerequisite chains: avoid spending scarce SP on an unimplemented effect. Report evidence paths/lines and missing authority; do not guess canonical server behavior. Preserve existing practice saves/kits.


## Assets/Scripts/GameLogic/Core/Player.cs : 20-65
```
        private MapData normalTerrainMap;
        private MapData movementMap;
        private int normalTerrainRevision = -1;
        private double movementAccumulator;
        private bool updatingPhysics;
        private bool jumpRequested;
        private bool jumpDownRequested;
        private bool crouchRequested;
        private LadderInfo requestedLadder;
        private bool stopClimbingRequested;
        public bool CanDropThroughPlatform => normalMovement?.CanDrop == true && IsGrounded;
        public bool CanClimb => (normalMovement?.ClimbCooldownMilliseconds ?? 0) == 0;
        public int ClimbCooldownMilliseconds => normalMovement?.ClimbCooldownMilliseconds ?? 0;
        
        // View listener management
        private readonly List<IPlayerViewListener> viewListeners = new List<IPlayerViewListener>();
        public bool HasViewListeners => viewListeners.Count > 0;
        
        // Services
        private readonly IFootholdService footholdService;
        
        // Player dimensions (in units)
        public const float Height = 0.6f;
        private const float PLAYER_HEIGHT = Height; // 60 pixels / 100
        private const float PLAYER_WIDTH = 0.3f;  // 30 pixels / 100
        
        // Movement state
        private float actualWalkSpeed;
        private float actualJumpPower;
        private bool jumpKeyPressed = false; // Track jump key state for subsequent jumps
        
        // Special movement
        private bool hasDoubleJump = false;
        private bool hasFlashJump = false;
        private int jumpCount = 0; // 0 = no jumps used, 1 = double jump used
        private float flashJumpCooldown = 0f;
        private const float FLASH_JUMP_COOLDOWN = 1f; // 1 second cooldown
        private const float FLASH_JUMP_DISTANCE = 1.5f; // 150 pixels / 100
        
        // Movement modifiers
        private readonly List<IMovementModifier> movementModifiers = new List<IMovementModifier>();

        public int Id { get; set; }
        public string Name { get; set; } = "Player";
        
        private Vector2 position;
```


## Assets/Scripts/GameLogic/Core/Player.cs : 190-208
```
        public int Accuracy {
            get => Math.Min(999, baseAccuracy + EquipmentBonus(StatType.Accuracy) + Passives.Accuracy) +
                (int)(Math.Min(999, DEX) * .8f + Math.Min(999, LUK) * .5f);
            set => baseAccuracy = value;
        }
        private int baseAvoidability = 0;
        public int Avoidability { get => baseAvoidability + EquipmentBonus(StatType.Avoidability) + Passives.Avoidability; set => baseAvoidability = value; }
        private int baseSpeed = 100;
        public int Speed { get => WithBuff(baseSpeed + EquipmentBonus(StatType.Speed), BuffType.Speed, 140); set => baseSpeed = value; }
        private int baseJumpPower = 120;
        public int JumpPower { get => WithBuff(baseJumpPower + EquipmentBonus(StatType.Jump), BuffType.Jump, 123); set => baseJumpPower = value; }
        public int JobId { get; set; } = 0; // Beginner

        private bool isMovingLeft;
        private bool isMovingRight;
        private int pendingSwimFacing;
        public long SwimAnimationTicks { get; private set; }
        private bool isClimbingUp;
        private bool isClimbingDown;
```


## Assets/Scripts/GameLogic/Core/Player.cs : 848-868
```
        public void AddExperience(long amount)
        {
            if (amount <= 0 || IsDead || level >= ExperienceTable.LevelCap) return;
            experience += Math.Min(amount, long.MaxValue - experience);
            while (level < ExperienceTable.LevelCap && experience >= ExperienceTable.RequiredForLevel(level))
            {
                experience -= ExperienceTable.RequiredForLevel(level);
                level++;
                // Local play restores current HP/MP. Job-specific stat growth remains a separate port.
                hp = maxHp; mp = maxMp;
                LeveledUp?.Invoke(level);
            }
            if (level == ExperienceTable.LevelCap) experience = 0;
            ExperienceGained?.Invoke(amount);
        }

        public void Heal(int amount)
        {
            if (!IsDead && amount > 0) hp += Math.Min(amount, maxHp - hp);
        }

```


## Assets/Scripts/GameLogic/Core/LocalProgress.cs : 1-115
```
using System;
using System.Linq;
using System.Collections.Generic;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Core
{
    [Serializable] public sealed class SavedEquipment { public int Slot, ItemId; }
    [Serializable] public sealed class SavedSkill { public int Id, Level; }
    [Serializable] public sealed class PlayerProgress
    {
        public string Name;
        public int Level, Job, Gender, HP, MP, Mesos;
        public long Experience;
        public int MaxHP, MaxMP, Str, Dex, Int, Luk, WeaponAttack, MagicAttack, WeaponDefense, MagicDefense, Accuracy, Avoidability, Speed, Jump;
        public InventoryStack[] Bag;
        public SavedEquipment[] Equipment;
    }
    [Serializable] public sealed class LocalProgress
    {
        public int Version = 1;
        public int MapId;
        public PlayerProgress Player;
        public SavedSkill[] Skills;
        public int[] Hotbar = new int[0];
        public bool PracticeSupplies, PracticeFighter, PracticeMagic;
        public int[] RangedKits;
    }
    public partial class Player
    {
        public PlayerProgress CaptureProgress() => new PlayerProgress {
            Name = Name, Level = level, Job = JobId, Gender = Gender, HP = hp, MP = mp, Mesos = Mesos, Experience = experience,
            MaxHP = baseMaxHp, MaxMP = baseMaxMp, Str = baseSTR, Dex = baseDEX, Int = baseINT, Luk = baseLUK,
            WeaponAttack = baseWeaponAttack, MagicAttack = baseMagicAttack, WeaponDefense = baseWeaponDefense,
            MagicDefense = baseMagicDefense, Accuracy = baseAccuracy, Avoidability = baseAvoidability, Speed = baseSpeed, Jump = baseJumpPower,
            Bag = inventory.GetStacks().ToArray(), Equipment = equippedItems.Select(e => new SavedEquipment { Slot = (int)e.Key, ItemId = e.Value }).ToArray()
        };
        public bool CanRestoreProgress(PlayerProgress p)
        {
            if (p == null || p.Name == null || p.Name.Length > 64 || p.Level < 1 || p.Level > ExperienceTable.LevelCap ||
                p.Job < 0 || p.Job > 9999 || p.Gender < 0 || p.Gender > 1 || p.Experience < 0 ||
                (p.Level == ExperienceTable.LevelCap ? p.Experience != 0 : p.Experience >= ExperienceTable.RequiredForLevel(p.Level)) ||
                p.Mesos < 0 || p.MaxHP < 1 || !inventory.CanRestore(p.Bag) || p.Equipment == null || p.Equipment.Length > 8) return false;
            var stats = new[] { p.MaxHP, p.MaxMP, p.HP, p.MP, p.Str, p.Dex, p.Int, p.Luk, p.WeaponAttack, p.MagicAttack,
                p.WeaponDefense, p.MagicDefense, p.Accuracy, p.Avoidability, p.Speed, p.Jump };
            if (stats.Any(s => s < 0 || s > 100000)) return false;
            var slots = new HashSet<int>(); var unique = new HashSet<int>(p.Bag.Select(s => s.ItemId));
            foreach (var e in p.Equipment)
            {
                if (e == null || !slots.Add(e.Slot) || !SupportedSlot((EquipSlot)e.Slot)) return false;
                var item = GetItemInfo(e.ItemId);
                if (item?.EquipmentSlot != (EquipSlot)e.Slot || (item.IsOneOfAKind && !unique.Add(e.ItemId))) return false;
            }
            bool hasShield = slots.Contains((int)EquipSlot.Shield), hasBottom = slots.Contains((int)EquipSlot.Bottom);
            return !p.Equipment.Any(e => (hasShield && GetItemInfo(e.ItemId).IsTwoHanded) || (hasBottom && GetItemInfo(e.ItemId).IsOverall));
        }
        internal void RestoreProgress(PlayerProgress p)
        {
            Name = p.Name; level = p.Level; JobId = p.Job; Gender = p.Gender; experience = p.Experience; Mesos = p.Mesos;
            baseMaxHp = p.MaxHP; baseMaxMp = p.MaxMP; baseSTR = p.Str; baseDEX = p.Dex; baseINT = p.Int; baseLUK = p.Luk;
            baseWeaponAttack = p.WeaponAttack; baseMagicAttack = p.MagicAttack; baseWeaponDefense = p.WeaponDefense;
            baseMagicDefense = p.MagicDefense; baseAccuracy = p.Accuracy; baseAvoidability = p.Avoidability; baseSpeed = p.Speed; baseJumpPower = p.Jump;
            equippedItems = p.Equipment.ToDictionary(e => (EquipSlot)e.Slot, e => e.ItemId);
            statBuffs.Clear(); movementModifiers.Clear(); invulnerableMilliseconds = 0;
            HasPracticeDamageOverride = false; baseDamage = 20;
            inventory.Restore(p.Bag); RecalculateEquipment(); SetHPMP(p.HP, p.MP);
            FaceAnimation.Reset(); ResetMovementForMap(); Velocity = Vector2.Zero;
            MesosChanged?.Invoke(); BuffsChanged?.Invoke();
        }
    }
    public partial class GameWorld
    {
        public LocalProgress CaptureProgress() => !IsLocal || currentMap == null ? null : new LocalProgress {
            MapId = CurrentMapId, Player = player.CaptureProgress(),
            Skills = skillManager?.LearnedSkills.Select(s => new SavedSkill { Id = s.SkillId, Level = s.CurrentLevel }).ToArray() ?? new SavedSkill[0],
            PracticeSupplies = practiceSuppliesGranted, PracticeFighter = practiceSkillsGranted, PracticeMagic = practiceMagicGranted,
            RangedKits = rangedPracticeKits.OrderBy(i => i).ToArray()
        };
        public bool TryRestoreProgress(LocalProgress save, out string message)
        {
            message = "The save is incompatible or contains unavailable data. Your current session is unchanged.";
            if (!IsLocal || save == null || save.Version != 1 || save.MapId < 0 || !player.CanRestoreProgress(save.Player) ||
                save.Skills == null || save.Skills.Length > 500 || save.RangedKits == null || save.RangedKits.Length > 4 ||
                save.RangedKits.Any(k => RangedPracticeWeapon(k) == 0) || save.RangedKits.Distinct().Count() != save.RangedKits.Length ||
                save.Skills.Any(s => s == null || skillManager == null || !skillManager.CanSetSkillLevel(s.Id, s.Level)) ||
                save.Skills.Select(s => s.Id).Distinct().Count() != save.Skills.Length ||
                save.Hotbar == null || save.Hotbar.Length > 12 || save.Hotbar.Any(id => id != 0 && !save.Skills.Any(s => s.Id == id))) return false;
            var destination = mapLoader is Interfaces.IMapPreviewLoader preview ? preview.PreviewMap(save.MapId) : mapLoader.GetMap(save.MapId);
            if (destination == null || !new PlayerSpawnManager(null).TryFindSafeSpawn(destination, out _))
            { message = "The saved map has no available safe entrance. Your current session is unchanged."; return false; }
            // Commit only after every record and the destination have passed validation.
            player.RestoreProgress(save.Player);
            skillManager?.RestoreLearnedSkills(save.Skills);
            practiceSuppliesGranted = save.PracticeSupplies; practiceSkillsGranted = save.PracticeFighter; practiceMagicGranted = save.PracticeMagic;
            rangedPracticeKits.Clear(); foreach (int kit in save.RangedKits) rangedPracticeKits.Add(kit);
            currentMap = destination; OnMapLoaded(); PlayerRecovered?.Invoke();
            message = "Loaded progress at the saved map's entrance. Temporary effects have ended."; return true;
        }
    }
}
```


## Assets/Scripts/GameLogic/Skills/SkillManager.cs : 20-92
```
        
        public SkillManager(Player player, IAssetProvider assetProvider, INetworkClient networkClient = null)
        {
            this.player = player;
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
                if (skill.IsPassive && SourceSkillRules.CanUse(player.JobId, skill.SkillId) && skill.GetCurrentLevelData() != null)
                    SourceSkillRules.ApplyPassive(ref result, skill.SkillId, skill.GetCurrentLevelData(), player.EquippedWeaponType, player.MaxHP, player.CurrentHP);
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
        
        public SkillUseResult UseSkill(int skillId)
```


## Assets/Scripts/GameLogic/Skills/SkillManager.cs : 201-233
```
        public Dictionary<int, SkillInfo> GetAvailableSkills()
        {
            var allJobSkills = SourceSkillRules.JobBranches(player.JobId).Distinct()
                .SelectMany(job => assetProvider.SkillData.GetSkillsForJob(job)).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
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
            if (!SourceSkillRules.CanUse(player.JobId, skillId))
                return false;
                
            return skillInfo.Levels?.ContainsKey(1) == true &&
                (skillInfo.RequiredSkills == null || skillInfo.RequiredSkills.All(r => GetSkillLevel(r.Key) >= r.Value));
        }
        
        public void Update(float deltaTime)
        {
```


## Assets/Scripts/GameLogic/Skills/SourceSkillRules.cs : 16-28
```
    public static class SourceSkillRules
    {
        public static IEnumerable<int> JobBranches(int job)
        {
            yield return 0;
            int level = job == 0 ? 0 : job % 100 == 0 ? 1 : job % 10 == 0 ? 2 : job % 10 == 1 ? 3 : 4;
            if (level >= 1) yield return job / 100 * 100;
            if (level >= 2) yield return job / 10 * 10;
            if (level >= 3) yield return level == 4 ? job - 1 : job;
            if (level >= 4) yield return job;
        }
        public static bool CanUse(int job, int skillId)
        { foreach (int branch in JobBranches(job)) if (branch == skillId / 10000) return true; return false; }
```


## Assets/Scripts/GameLogic/Skills/SourceSkillRules.cs : 68-77
```
                    return;
            }
            bool applies = id == 1100000 && (weapon == 130 || weapon == 140) ||
                id == 1100001 && (weapon == 131 || weapon == 141) || id == 1200001 && (weapon == 132 || weapon == 142) ||
                id == 1300000 && weapon == 143 || id == 1300001 && weapon == 144;
            // The source registry repeats Fighter's Sword Mastery in its Page section.
            // Page skill 1200000 stays without a handler until that source discrepancy is resolved.
            if (applies) { stats.Mastery = .5f + data.Mastery / 100f; stats.Accuracy += data.X; }
        }
    }
```


## C:/HeavenClient/MapleStory-Client/Net/Packets/PlayerPackets.h : 28-51
```
	// Requests a stat increase by spending AP
	// Opcode: SPEND_AP(87)
	class SpendApPacket : public OutPacket
	{
	public:
		SpendApPacket(MapleStat::Id stat) : OutPacket(OutPacket::Opcode::SPEND_AP)
		{
			write_time();
			write_int(MapleStat::codes[stat]);
		}
	};

	// Requests a skill level increase by spending SP
	// Opcode: SPEND_SP(90)
	class SpendSpPacket : public OutPacket
	{
	public:
		SpendSpPacket(int32_t skill_id) : OutPacket(OutPacket::Opcode::SPEND_SP)
		{
			write_time();
			write_int(skill_id);
		}
	};

```


## C:/HeavenClient/MapleStory-Client/IO/UITypes/UISkillBook.cpp : 815-833
```
			for (size_t i = 0; i < skills.size(); i++)
			{
				int32_t skillid = skills[i].get_id();

				if (skillid == SkillId::Id::THREE_SNAILS || skillid == SkillId::Id::HEAL || skillid == SkillId::Id::FEATHER)
					remaining_beginner_sp -= skills[i].get_level();
			}

			beginner_sp = remaining_beginner_sp;
			splabel.change_text(std::to_string(beginner_sp));
		}
		else
		{
			sp = stats.get_stat(MapleStat::Id::SP);
			splabel.change_text(std::to_string(sp));
		}

		change_offset(offset);
		set_skillpoint(false);
```


## C:/HeavenClient/MapleStory-Client/IO/UITypes/UISkillBook.cpp : 946-972
```
	bool UISkillBook::can_raise(int32_t skill_id) const
	{
		Job::Level joblevel = joblevel_by_tab(tab);

		if (joblevel == Job::Level::BEGINNER && beginner_sp <= 0)
			return false;

		if (tab + Buttons::BT_TAB0 != Buttons::BT_TAB0 && sp <= 0)
			return false;

		int32_t level = skillbook.get_level(skill_id);
		int32_t masterlevel = skillbook.get_masterlevel(skill_id);

		if (masterlevel == 0)
			masterlevel = SkillData::get(skill_id).get_masterlevel();

		if (level >= masterlevel)
			return false;

		switch (skill_id)
		{
		case SkillId::Id::ANGEL_BLESSING:
			return false;
		default:
			return check_required(skill_id);
		}
	}
```


## C:/HeavenClient/MapleStory-Client/IO/UITypes/UISkillBook.cpp : 1026-1031
```
	void UISkillBook::spend_sp(int32_t skill_id)
	{
		SpendSpPacket(skill_id).dispatch();

		UI::get().disable();
	}
```
