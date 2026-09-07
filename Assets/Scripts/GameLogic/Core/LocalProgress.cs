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
        public int AbilityPoints, HighestRewardedLevel;
        public int[] SkillPointPools;
        public bool HasChosenFirstJob;
    }
    [Serializable] public sealed class LocalProgress
    {
        public int Version = 5;
        public KeyboardEntry[] Keyboard = new KeyboardEntry[0];
        public QuickslotBinding[] Quickslots = new QuickslotBinding[0];
        public SavedQuest[] Quests = new SavedQuest[0];
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
            Bag = inventory.GetStacks().ToArray(), Equipment = equippedItems.Select(e => new SavedEquipment { Slot = (int)e.Key, ItemId = e.Value }).ToArray(),
            AbilityPoints=abilityPoints, HighestRewardedLevel=highestRewardedLevel, SkillPointPools=(int[])skillPointPools.Clone(), HasChosenFirstJob=HasChosenFirstJob
        };
        public bool CanRestoreProgress(PlayerProgress p,int version=2)
        {
            if (p == null || !CanRestoreProgression(p,version) || p.Name == null || p.Name.Length > 64 || p.Level < 1 || p.Level > ExperienceTable.LevelCap ||
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
        internal void RestoreProgress(PlayerProgress p,int version)
        {
            RestoreProgression(p,version);
            Name = p.Name; level = p.Level; JobId = p.Job; Gender = p.Gender; experience = p.Experience; Mesos = p.Mesos;
            baseMaxHp = p.MaxHP; baseMaxMp = p.MaxMP; baseSTR = p.Str; baseDEX = p.Dex; baseINT = p.Int; baseLUK = p.Luk;
            baseWeaponAttack = p.WeaponAttack; baseMagicAttack = p.MagicAttack; baseWeaponDefense = p.WeaponDefense;
            baseMagicDefense = p.MagicDefense; baseAccuracy = p.Accuracy; baseAvoidability = p.Avoidability; baseSpeed = p.Speed; baseJumpPower = p.Jump;
            equippedItems = p.Equipment.ToDictionary(e => (EquipSlot)e.Slot, e => e.ItemId);
            statBuffs.Clear(); movementModifiers.Clear(); invulnerableMilliseconds = 0;
            HasPracticeDamageOverride = false; baseDamage = 20;
            inventory.Restore(p.Bag); RecalculateEquipment(); SetHPMP(p.HP, p.MP);
            FaceAnimation.Reset(); ResetMovementForMap(); Velocity = Vector2.Zero;
            MesosChanged?.Invoke(); BuffsChanged?.Invoke(); ProgressionChanged?.Invoke();
        }
    }
    public partial class GameWorld
    {
        public LocalProgress CaptureProgress() => !IsLocal || currentMap == null ? null : new LocalProgress {
            MapId = CurrentMapId, Player = player.CaptureProgress(), Keyboard = Keyboard.Capture(),
            Quests = Quests.Capture(),
            Skills = skillManager?.LearnedSkills.Select(s => new SavedSkill { Id = s.SkillId, Level = s.CurrentLevel }).ToArray() ?? new SavedSkill[0],
            PracticeSupplies = practiceSuppliesGranted, PracticeFighter = practiceSkillsGranted, PracticeMagic = practiceMagicGranted,
            RangedKits = weaponPracticeKits.OrderBy(i => i).ToArray()
        };
        public bool TryRestoreProgress(LocalProgress save, out string message)
        {
            message = "The save is incompatible or contains unavailable data. Your current session is unchanged.";
            if (!IsLocal || save == null || save.Version < 1 || save.Version > 5 || save.MapId < 0 || !player.CanRestoreProgress(save.Player,save.Version) ||
                (save.Version >= 3 && !Quests.CanRestore(save.Quests)) ||
                save.Skills == null || save.Skills.Length > 500 || save.RangedKits == null || save.RangedKits.Length > 4 ||
                save.RangedKits.Any(k => PracticeWeapon(k) == 0) || save.RangedKits.Distinct().Count() != save.RangedKits.Length ||
                save.Skills.Any(s => s == null || skillManager == null || !skillManager.CanSetSkillLevel(s.Id, s.Level)) ||
                save.Skills.Select(s => s.Id).Distinct().Count() != save.Skills.Length ||
                save.Hotbar == null || save.Hotbar.Length > 12 || save.Hotbar.Any(id => id != 0 && !save.Skills.Any(s => s.Id == id)) || !CanRestoreQuickslots(save) || !CanRestoreKeyboard(save)) return false;
            var destination = mapLoader is Interfaces.IMapPreviewLoader preview ? preview.PreviewMap(save.MapId) : mapLoader.GetMap(save.MapId);
            if (destination == null || !new PlayerSpawnManager(null).TryFindSafeSpawn(destination, out _))
            { message = "The saved map has no available safe entrance. Your current session is unchanged."; return false; }
            // Commit only after every record and the destination have passed validation.
            player.RestoreProgress(save.Player,save.Version);
            Quests.Restore(save.Version >= 3 ? save.Quests : new SavedQuest[0]);
            skillManager?.RestoreLearnedSkills(save.Skills);
            RestoreKeyboard(save);
            practiceSuppliesGranted = save.PracticeSupplies; practiceSkillsGranted = save.PracticeFighter; practiceMagicGranted = save.PracticeMagic;
            weaponPracticeKits.Clear(); foreach (int kit in save.RangedKits) weaponPracticeKits.Add(kit);
            currentMap = destination; OnMapLoaded(); PlayerRecovered?.Invoke();
            message = "Loaded progress at the saved map's entrance. Temporary effects have ended."; return true;
        }
    }
}
