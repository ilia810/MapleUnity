using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameData;

namespace MapleClient.GameView.UI
{
    /// <summary>
    /// Manages MapleStory UI elements loaded from NX files
    /// </summary>
    public class MapleUI : MonoBehaviour
    {
        private NXAssetLoader assetLoader;
        
        // UI Containers
        private GameObject statusBarContainer;
        private GameObject skillBarContainer;
        private GameObject inventoryContainer;
        private GameObject systemMenuContainer;
        
        // UI Elements
        private Image hpBar;
        private Image mpBar;
        private Image expBar;
        private Text levelText;
        private Text nameText;
        private Text hpText;
        private Text mpText;
        
        // Quick slots
        private List<Image> quickSlots = new List<Image>();
        private const int QUICK_SLOT_COUNT = 8;
        
        void Awake()
        {
            // NXAssetLoader is a singleton, not a MonoBehaviour
            assetLoader = NXAssetLoader.Instance;
        }
        
        void Start()
        {
            CreateUIContainers();
            LoadUIAssets();
        }
        
        private void CreateUIContainers()
        {
            // Create main UI canvas if it doesn't exist
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UI Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Status bar at bottom
            statusBarContainer = new GameObject("StatusBar");
            statusBarContainer.transform.SetParent(canvas.transform, false);
            RectTransform statusRect = statusBarContainer.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0);
            statusRect.pivot = new Vector2(0.5f, 0);
            statusRect.sizeDelta = new Vector2(0, 60);
            statusRect.anchoredPosition = Vector2.zero;
            
            // Skill bar above status bar
            skillBarContainer = new GameObject("SkillBar");
            skillBarContainer.transform.SetParent(canvas.transform, false);
            RectTransform skillRect = skillBarContainer.AddComponent<RectTransform>();
            skillRect.anchorMin = new Vector2(0.5f, 0);
            skillRect.anchorMax = new Vector2(0.5f, 0);
            skillRect.pivot = new Vector2(0.5f, 0);
            skillRect.sizeDelta = new Vector2(400, 40);
            skillRect.anchoredPosition = new Vector2(0, 65);
            
            // Inventory on right side
            inventoryContainer = new GameObject("Inventory");
            inventoryContainer.transform.SetParent(canvas.transform, false);
            RectTransform invRect = inventoryContainer.AddComponent<RectTransform>();
            invRect.anchorMin = new Vector2(1, 0.5f);
            invRect.anchorMax = new Vector2(1, 0.5f);
            invRect.pivot = new Vector2(1, 0.5f);
            invRect.sizeDelta = new Vector2(176, 300);
            invRect.anchoredPosition = new Vector2(-10, 0);
            inventoryContainer.SetActive(false); // Hidden by default
            
            // System menu on left side
            systemMenuContainer = new GameObject("SystemMenu");
            systemMenuContainer.transform.SetParent(canvas.transform, false);
            RectTransform sysRect = systemMenuContainer.AddComponent<RectTransform>();
            sysRect.anchorMin = new Vector2(0, 1);
            sysRect.anchorMax = new Vector2(0, 1);
            sysRect.pivot = new Vector2(0, 1);
            sysRect.sizeDelta = new Vector2(100, 200);
            sysRect.anchoredPosition = new Vector2(10, -10);
        }
        
        private void LoadUIAssets()
        {
            // Load status bar background from NX
            LoadStatusBar();
            LoadSkillBar();
            LoadSystemMenu();
        }
        
        private void LoadStatusBar()
        {
            // Load main status bar background
            var bgSprite = assetLoader.LoadUIElement("statusBar", "background");
            if (bgSprite != null)
            {
                Image bgImage = statusBarContainer.AddComponent<Image>();
                bgImage.sprite = bgSprite;
                bgImage.type = Image.Type.Sliced;
            }
            
            // Create HP bar
            CreateStatusGauge("hp", new Vector2(50, 20), new Vector2(150, 15));
            
            // Create MP bar
            CreateStatusGauge("mp", new Vector2(50, 5), new Vector2(150, 15));
            
            // Create EXP bar
            CreateExpBar();
            
            // Create level display
            CreateLevelDisplay();
        }
        
        private void CreateStatusGauge(string type, Vector2 position, Vector2 size)
        {
            GameObject gaugeObj = new GameObject($"{type}Gauge");
            gaugeObj.transform.SetParent(statusBarContainer.transform, false);
            
            RectTransform rect = gaugeObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            
            // Background
            var bgSprite = assetLoader.LoadUIElement("gauge", $"{type}Back");
            if (bgSprite != null)
            {
                Image bgImage = gaugeObj.AddComponent<Image>();
                bgImage.sprite = bgSprite;
            }
            
            // Fill bar
            GameObject fillObj = new GameObject($"{type}Fill");
            fillObj.transform.SetParent(gaugeObj.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            var fillSprite = assetLoader.LoadUIElement("gauge", $"{type}Bar");
            if (fillSprite != null)
            {
                Image fillImage = fillObj.AddComponent<Image>();
                fillImage.sprite = fillSprite;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                
                if (type == "hp")
                    hpBar = fillImage;
                else if (type == "mp")
                    mpBar = fillImage;
            }
            
            // Text display
            GameObject textObj = new GameObject($"{type}Text");
            textObj.transform.SetParent(gaugeObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            Text text = textObj.AddComponent<Text>();
            text.text = "100/100";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 11;
            text.color = Color.white;
            
            // Use default font for now
            // TODO: Convert MapleStory font to Unity font
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            if (type == "hp")
                hpText = text;
            else if (type == "mp")
                mpText = text;
        }
        
        private void CreateExpBar()
        {
            GameObject expObj = new GameObject("ExpBar");
            expObj.transform.SetParent(statusBarContainer.transform, false);
            
            RectTransform rect = expObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(-20, 5);
            rect.anchoredPosition = new Vector2(0, 2);
            
            // Background
            Image bgImage = expObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Fill
            GameObject fillObj = new GameObject("ExpFill");
            fillObj.transform.SetParent(expObj.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            expBar = fillObj.AddComponent<Image>();
            expBar.color = new Color(1f, 0.8f, 0f, 1f); // Gold color
            expBar.type = Image.Type.Filled;
            expBar.fillMethod = Image.FillMethod.Horizontal;
            expBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            expBar.fillAmount = 0.3f; // Example: 30% exp
        }
        
        private void CreateLevelDisplay()
        {
            GameObject levelObj = new GameObject("LevelDisplay");
            levelObj.transform.SetParent(statusBarContainer.transform, false);
            
            RectTransform rect = levelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(40, 30);
            rect.anchoredPosition = new Vector2(5, 0);
            
            // Load level display background
            var lvSprite = assetLoader.LoadUIElement("level", "background");
            if (lvSprite != null)
            {
                Image bgImage = levelObj.AddComponent<Image>();
                bgImage.sprite = lvSprite;
            }
            
            // Level text
            levelText = levelObj.AddComponent<Text>();
            levelText.text = "10";
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.fontSize = 14;
            levelText.fontStyle = FontStyle.Bold;
            levelText.color = Color.yellow;
            levelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        private void LoadSkillBar()
        {
            // Create quick slots
            float slotSize = 40f;
            float spacing = 5f;
            float totalWidth = (slotSize + spacing) * QUICK_SLOT_COUNT - spacing;
            float startX = -totalWidth / 2f;
            
            for (int i = 0; i < QUICK_SLOT_COUNT; i++)
            {
                GameObject slotObj = new GameObject($"QuickSlot_{i}");
                slotObj.transform.SetParent(skillBarContainer.transform, false);
                
                RectTransform rect = slotObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                rect.sizeDelta = new Vector2(slotSize, slotSize);
                rect.anchoredPosition = new Vector2(startX + i * (slotSize + spacing), 0);
                
                // Load slot background
                var slotSprite = assetLoader.LoadUIElement("quickSlot", "background");
                Image slotImage = slotObj.AddComponent<Image>();
                if (slotSprite != null)
                {
                    slotImage.sprite = slotSprite;
                }
                else
                {
                    slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
                
                quickSlots.Add(slotImage);
                
                // Add key label
                GameObject keyLabel = new GameObject("KeyLabel");
                keyLabel.transform.SetParent(slotObj.transform, false);
                
                RectTransform keyRect = keyLabel.AddComponent<RectTransform>();
                keyRect.anchorMin = new Vector2(0, 1);
                keyRect.anchorMax = new Vector2(0, 1);
                keyRect.pivot = new Vector2(0, 1);
                keyRect.sizeDelta = new Vector2(15, 15);
                keyRect.anchoredPosition = new Vector2(2, -2);
                
                Text keyText = keyLabel.AddComponent<Text>();
                keyText.text = (i + 1).ToString();
                keyText.fontSize = 10;
                keyText.color = Color.white;
                keyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Add outline for visibility
                Outline outline = keyLabel.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);
            }
        }
        
        private void LoadSystemMenu()
        {
            // Create system menu buttons
            string[] menuItems = { "Character", "Inventory", "Skill", "Quest", "System" };
            float buttonHeight = 30f;
            float spacing = 5f;
            
            for (int i = 0; i < menuItems.Length; i++)
            {
                GameObject buttonObj = new GameObject($"MenuButton_{menuItems[i]}");
                buttonObj.transform.SetParent(systemMenuContainer.transform, false);
                
                RectTransform rect = buttonObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, buttonHeight);
                rect.anchoredPosition = new Vector2(0, -(i * (buttonHeight + spacing)));
                
                // Load button background
                var btnSprite = assetLoader.LoadUIElement("menu", menuItems[i].ToLower());
                Button button = buttonObj.AddComponent<Button>();
                Image btnImage = buttonObj.AddComponent<Image>();
                
                if (btnSprite != null)
                {
                    btnImage.sprite = btnSprite;
                }
                else
                {
                    btnImage.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
                }
                
                // Add text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                
                Text btnText = textObj.AddComponent<Text>();
                btnText.text = menuItems[i];
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.fontSize = 12;
                btnText.color = Color.white;
                btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Add button click handler
                int index = i;
                button.onClick.AddListener(() => OnMenuButtonClick(menuItems[index]));
            }
        }
        
        private void OnMenuButtonClick(string menuItem)
        {
            Debug.Log($"Menu button clicked: {menuItem}");
            
            switch (menuItem)
            {
                case "Inventory":
                    inventoryContainer.SetActive(!inventoryContainer.activeSelf);
                    break;
                // TODO: Handle other menu items
            }
        }
        
        public void UpdateHP(int current, int max)
        {
            if (hpBar != null)
            {
                hpBar.fillAmount = (float)current / max;
            }
            
            if (hpText != null)
            {
                hpText.text = $"{current}/{max}";
            }
        }
        
        public void UpdateMP(int current, int max)
        {
            if (mpBar != null)
            {
                mpBar.fillAmount = (float)current / max;
            }
            
            if (mpText != null)
            {
                mpText.text = $"{current}/{max}";
            }
        }
        
        public void UpdateExp(float percentage)
        {
            if (expBar != null)
            {
                expBar.fillAmount = percentage;
            }
        }
        
        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = level.ToString();
            }
        }
        
        public void SetQuickSlotItem(int slot, Sprite itemSprite)
        {
            if (slot >= 0 && slot < quickSlots.Count)
            {
                // Create item icon if it doesn't exist
                Transform itemTransform = quickSlots[slot].transform.Find("ItemIcon");
                Image itemImage;
                
                if (itemTransform == null)
                {
                    GameObject itemObj = new GameObject("ItemIcon");
                    itemObj.transform.SetParent(quickSlots[slot].transform, false);
                    
                    RectTransform rect = itemObj.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = new Vector2(-4, -4);
                    rect.anchoredPosition = Vector2.zero;
                    
                    itemImage = itemObj.AddComponent<Image>();
                }
                else
                {
                    itemImage = itemTransform.GetComponent<Image>();
                }
                
                itemImage.sprite = itemSprite;
                itemImage.enabled = itemSprite != null;
            }
        }
    }
}