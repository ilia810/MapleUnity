using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MapleClient.GameLogic.Skills;
using MapleClient.GameLogic.Interfaces;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView.UI
{
    public class SkillBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject skillSlotPrefab;
        [SerializeField] private Transform skillSlotsContainer;
        [SerializeField] private int maxSkillSlots = 8;
        
        private GameManager gameManager;
        private List<SkillSlot> skillSlots;
        private Dictionary<KeyCode, int> hotkeys; // Maps key to skill ID
        
        private void Start()
        {
            gameManager = FindObjectOfType<GameManager>();
            skillSlots = new List<SkillSlot>();
            hotkeys = new Dictionary<KeyCode, int>();
            
            // Create UI if not already present
            if (skillSlotsContainer == null)
            {
                CreateSkillBarUI();
            }
            
            InitializeSkillSlots();
            SetupDefaultHotkeys();
        }
        
        private void CreateSkillBarUI()
        {
            // Create container
            GameObject container = new GameObject("SkillBar");
            container.transform.SetParent(transform);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            
            // Position at bottom center of screen
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 50);
            containerRect.sizeDelta = new Vector2(400, 50);
            
            // Add horizontal layout group
            HorizontalLayoutGroup layoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 5;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            skillSlotsContainer = container.transform;
        }
        
        private void InitializeSkillSlots()
        {
            // Create skill slot prefab if not provided
            if (skillSlotPrefab == null)
            {
                skillSlotPrefab = CreateSkillSlotPrefab();
            }
            
            // Create skill slots
            for (int i = 0; i < maxSkillSlots; i++)
            {
                GameObject slotObj = Instantiate(skillSlotPrefab, skillSlotsContainer);
                SkillSlot slot = slotObj.GetComponent<SkillSlot>();
                if (slot == null)
                {
                    slot = slotObj.AddComponent<SkillSlot>();
                }
                
                slot.Initialize(i, KeyCode.Alpha1 + i); // F1-F8 or 1-8
                skillSlots.Add(slot);
            }
        }
        
        private GameObject CreateSkillSlotPrefab()
        {
            GameObject prefab = new GameObject("SkillSlot");
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(45, 45);
            
            // Background
            Image bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Icon container
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(prefab.transform);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-4, -4);
            iconRect.anchoredPosition = Vector2.zero;
            Image icon = iconObj.AddComponent<Image>();
            icon.preserveAspect = true;
            
            // Cooldown overlay
            GameObject cooldownObj = new GameObject("Cooldown");
            cooldownObj.transform.SetParent(prefab.transform);
            RectTransform cdRect = cooldownObj.AddComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.sizeDelta = Vector2.zero;
            cdRect.anchoredPosition = Vector2.zero;
            Image cdImage = cooldownObj.AddComponent<Image>();
            cdImage.color = new Color(0, 0, 0, 0.7f);
            cdImage.type = Image.Type.Filled;
            cdImage.fillMethod = Image.FillMethod.Radial360;
            cdImage.fillOrigin = (int)Image.Origin360.Top;
            cdImage.fillClockwise = false;
            cdImage.fillAmount = 0;
            
            // Hotkey text
            GameObject hotkeyObj = new GameObject("Hotkey");
            hotkeyObj.transform.SetParent(prefab.transform);
            RectTransform hotkeyRect = hotkeyObj.AddComponent<RectTransform>();
            hotkeyRect.anchorMin = new Vector2(0, 1);
            hotkeyRect.anchorMax = new Vector2(0, 1);
            hotkeyRect.sizeDelta = new Vector2(20, 20);
            hotkeyRect.anchoredPosition = new Vector2(10, -10);
            Text hotkeyText = hotkeyObj.AddComponent<Text>();
            hotkeyText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            hotkeyText.fontSize = 12;
            hotkeyText.color = Color.white;
            hotkeyText.alignment = TextAnchor.MiddleCenter;
            
            // Level text
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(prefab.transform);
            RectTransform levelRect = levelObj.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(1, 0);
            levelRect.anchorMax = new Vector2(1, 0);
            levelRect.sizeDelta = new Vector2(20, 20);
            levelRect.anchoredPosition = new Vector2(-10, 10);
            Text levelText = levelObj.AddComponent<Text>();
            levelText.font = Font.CreateDynamicFontFromOSFont("Arial", 10);
            levelText.fontSize = 10;
            levelText.color = Color.yellow;
            levelText.alignment = TextAnchor.MiddleCenter;
            
            return prefab;
        }
        
        private void SetupDefaultHotkeys()
        {
            // Setup default hotkey mappings
            for (int i = 0; i < skillSlots.Count && i < 8; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                hotkeys[key] = -1; // No skill assigned initially
            }
        }
        
        private void Update()
        {
            if (gameManager?.SkillManager == null)
                return;
                
            // Check for hotkey presses
            foreach (var kvp in hotkeys)
            {
                if (Input.GetKeyDown(kvp.Key) && kvp.Value > 0)
                {
                    UseSkill(kvp.Value);
                }
            }
            
            // Update cooldown displays
            UpdateCooldowns();
        }
        
        private void UseSkill(int skillId)
        {
            var skillManager = gameManager.SkillManager;
            var result = skillManager.UseSkill(skillId);
            
            if (!result.Success)
            {
                // Show error message (could be integrated with chat system later)
                Debug.Log($"Skill failed: {result.ErrorMessage}");
            }
        }
        
        private void UpdateCooldowns()
        {
            var skillManager = gameManager.SkillManager;
            
            foreach (var slot in skillSlots)
            {
                if (slot.SkillId > 0)
                {
                    // Update cooldown display
                    // This would be implemented in the SkillSlot component
                    slot.UpdateCooldown();
                }
            }
        }
        
        public void AssignSkillToSlot(int skillId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillSlots.Count)
                return;
                
            var slot = skillSlots[slotIndex];
            slot.SetSkill(skillId);
            
            // Update hotkey mapping
            hotkeys[slot.Hotkey] = skillId;
        }
        
        public void LoadSkillsForCurrentJob()
        {
            if (gameManager?.SkillManager == null)
                return;
                
            var availableSkills = gameManager.SkillManager.GetAvailableSkills();
            
            // Auto-assign first few skills to slots (temporary)
            int slotIndex = 0;
            foreach (var skill in availableSkills.Values)
            {
                if (slotIndex >= skillSlots.Count)
                    break;
                    
                if (!skill.IsPassive)
                {
                    AssignSkillToSlot(skill.SkillId, slotIndex);
                    slotIndex++;
                }
            }
        }
    }
    
    public class SkillSlot : MonoBehaviour
    {
        private int slotIndex;
        private KeyCode hotkey;
        private int skillId;
        
        private Image iconImage;
        private Image cooldownImage;
        private Text hotkeyText;
        private Text levelText;
        
        public int SkillId => skillId;
        public KeyCode Hotkey => hotkey;
        
        public void Initialize(int index, KeyCode key)
        {
            slotIndex = index;
            hotkey = key;
            
            // Get UI components
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
            cooldownImage = transform.Find("Cooldown")?.GetComponent<Image>();
            hotkeyText = transform.Find("Hotkey")?.GetComponent<Text>();
            levelText = transform.Find("Level")?.GetComponent<Text>();
            
            // Set hotkey text
            if (hotkeyText != null)
            {
                hotkeyText.text = (index + 1).ToString();
            }
        }
        
        public void SetSkill(int skillId)
        {
            this.skillId = skillId;
            
            // Update UI based on skill info
            if (skillId > 0)
            {
                // In a real implementation, would load skill icon from assets
                if (iconImage != null)
                {
                    iconImage.color = Color.white;
                }
                
                UpdateLevel();
            }
            else
            {
                // Clear slot
                if (iconImage != null)
                {
                    iconImage.color = Color.clear;
                }
                if (levelText != null)
                {
                    levelText.text = "";
                }
            }
        }
        
        public void UpdateCooldown()
        {
            if (cooldownImage == null || skillId <= 0)
                return;
                
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager?.SkillManager == null)
                return;
                
            // This would need to be exposed from SkillManager
            // For now, just hide the cooldown overlay
            cooldownImage.fillAmount = 0;
        }
        
        private void UpdateLevel()
        {
            if (levelText == null || skillId <= 0)
                return;
                
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager?.SkillManager == null)
                return;
                
            int level = gameManager.SkillManager.GetSkillLevel(skillId);
            levelText.text = level > 0 ? level.ToString() : "";
        }
    }
}