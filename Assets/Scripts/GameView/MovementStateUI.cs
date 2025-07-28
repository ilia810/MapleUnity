using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;

namespace MapleClient.GameView
{
    /// <summary>
    /// UI overlay that displays movement state information and skill cooldowns
    /// </summary>
    public class MovementStateUI : MonoBehaviour
    {
        [Header("UI References")]
        private Canvas canvas;
        private GameObject statePanel;
        private Text stateText;
        private Text velocityText;
        private Text groundedText;
        private GameObject modifiersPanel;
        private GameObject cooldownsPanel;
        
        // Modifier display
        private Dictionary<string, GameObject> modifierDisplays = new Dictionary<string, GameObject>();
        
        // Cooldown display
        private Dictionary<string, GameObject> cooldownDisplays = new Dictionary<string, GameObject>();
        
        // References
        private GameWorld gameWorld;
        private Player player;
        
        void Awake()
        {
            CreateUI();
        }
        
        void Start()
        {
            // Find GameWorld through GameManager
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var fieldInfo = typeof(GameManager).GetField("gameWorld", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    gameWorld = fieldInfo.GetValue(gameManager) as GameWorld;
                    if (gameWorld != null)
                    {
                        player = gameWorld.Player;
                    }
                }
            }
        }
        
        private void CreateUI()
        {
            // Create canvas if needed
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Create state panel
            CreateStatePanel();
            
            // Create modifiers panel
            CreateModifiersPanel();
            
            // Create cooldowns panel
            CreateCooldownsPanel();
        }
        
        private void CreateStatePanel()
        {
            statePanel = new GameObject("StatePanel");
            statePanel.transform.SetParent(canvas.transform, false);
            
            var rectTransform = statePanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -100);
            rectTransform.sizeDelta = new Vector2(300, 150);
            
            // Background
            var bg = statePanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            
            // Vertical layout
            var layout = statePanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            
            // State text
            var stateObj = CreateTextElement("StateText", "State: Standing", statePanel.transform);
            stateText = stateObj.GetComponent<Text>();
            
            // Velocity text
            var velocityObj = CreateTextElement("VelocityText", "Velocity: (0.0, 0.0)", statePanel.transform);
            velocityText = velocityObj.GetComponent<Text>();
            
            // Grounded text
            var groundedObj = CreateTextElement("GroundedText", "Grounded: Yes", statePanel.transform);
            groundedText = groundedObj.GetComponent<Text>();
        }
        
        private void CreateModifiersPanel()
        {
            modifiersPanel = new GameObject("ModifiersPanel");
            modifiersPanel.transform.SetParent(canvas.transform, false);
            
            var rectTransform = modifiersPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -260);
            rectTransform.sizeDelta = new Vector2(300, 100);
            
            // Background
            var bg = modifiersPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            
            // Title
            var titleObj = CreateTextElement("ModifiersTitle", "Active Modifiers:", modifiersPanel.transform);
            titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            
            // Vertical layout
            var layout = modifiersPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 3;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }
        
        private void CreateCooldownsPanel()
        {
            cooldownsPanel = new GameObject("CooldownsPanel");
            cooldownsPanel.transform.SetParent(canvas.transform, false);
            
            var rectTransform = cooldownsPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2(0, 100);
            rectTransform.sizeDelta = new Vector2(400, 60);
            
            // Horizontal layout for skill cooldowns
            var layout = cooldownsPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
        }
        
        private GameObject CreateTextElement(string name, string text, Transform parent)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            textComp.fontSize = 14;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.UpperLeft;
            
            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(280, 20);
            
            return textObj;
        }
        
        void Update()
        {
            if (player == null) return;
            
            // Update state display
            UpdateStateDisplay();
            
            // Update modifiers display
            UpdateModifiersDisplay();
            
            // Update cooldowns display
            UpdateCooldownsDisplay();
        }
        
        private void UpdateStateDisplay()
        {
            if (stateText != null)
            {
                stateText.text = $"State: {player.State}";
                
                // Color code by state
                switch (player.State)
                {
                    case PlayerState.Jumping:
                    case PlayerState.DoubleJumping:
                    case PlayerState.FlashJumping:
                        stateText.color = new Color(0.5f, 0.8f, 1f);
                        break;
                    case PlayerState.Climbing:
                        stateText.color = new Color(0.5f, 1f, 0.5f);
                        break;
                    case PlayerState.Falling:
                        stateText.color = new Color(1f, 0.8f, 0.5f);
                        break;
                    default:
                        stateText.color = Color.white;
                        break;
                }
            }
            
            if (velocityText != null)
            {
                var vel = player.Velocity;
                velocityText.text = $"Velocity: ({vel.X:F1}, {vel.Y:F1})";
                
                // Color based on speed
                float speed = Mathf.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
                if (speed > 5f)
                    velocityText.color = new Color(1f, 0.5f, 0.5f);
                else if (speed > 2f)
                    velocityText.color = new Color(1f, 1f, 0.5f);
                else
                    velocityText.color = Color.white;
            }
            
            if (groundedText != null)
            {
                groundedText.text = $"Grounded: {(player.IsGrounded ? "Yes" : "No")}";
                groundedText.color = player.IsGrounded ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            }
        }
        
        private void UpdateModifiersDisplay()
        {
            var modifiers = player.GetActiveModifiers();
            
            // Remove old modifier displays
            foreach (var kvp in modifierDisplays)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            modifierDisplays.Clear();
            
            // Create new modifier displays
            foreach (var modifier in modifiers)
            {
                var modifierObj = CreateTextElement($"Modifier_{modifier.Id}", "", modifiersPanel.transform);
                var text = modifierObj.GetComponent<Text>();
                
                string modifierInfo = GetModifierDisplayText(modifier);
                text.text = modifierInfo;
                text.color = GetModifierColor(modifier);
                text.fontSize = 12;
                
                modifierDisplays[modifier.Id] = modifierObj;
            }
        }
        
        private string GetModifierDisplayText(IMovementModifier modifier)
        {
            if (modifier.Id == "slippery_surface") return "• Slippery Surface (Ice)";
            if (modifier.Id == "swimming") return "• Swimming";
            if (modifier.Id.StartsWith("stun_")) return $"• Stunned ({modifier.Duration:F1}s)";
            if (modifier.SpeedMultiplier > 1f) return $"• Speed Boost x{modifier.SpeedMultiplier:F1}";
            if (modifier.SpeedMultiplier < 1f) return $"• Slowed x{modifier.SpeedMultiplier:F1}";
            return $"• {modifier.Id}";
        }
        
        private Color GetModifierColor(IMovementModifier modifier)
        {
            if (modifier.Id == "slippery_surface") return new Color(0.5f, 0.8f, 1f);
            if (modifier.Id == "swimming") return new Color(0.2f, 0.5f, 1f);
            if (modifier.Id.StartsWith("stun_")) return new Color(1f, 0.5f, 0.5f);
            if (modifier.SpeedMultiplier > 1f) return new Color(0.5f, 1f, 0.5f);
            if (modifier.SpeedMultiplier < 1f) return new Color(1f, 0.8f, 0.5f);
            return Color.white;
        }
        
        private void UpdateCooldownsDisplay()
        {
            // Check Flash Jump cooldown
            UpdateSkillCooldown("FlashJump", GetFlashJumpCooldown());
            
            // Future: Add other skill cooldowns here
        }
        
        private float GetFlashJumpCooldown()
        {
            // Access flash jump cooldown through reflection (since it's private)
            if (player == null) return 0f;
            
            var fieldInfo = typeof(Player).GetField("flashJumpCooldown", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                return (float)fieldInfo.GetValue(player);
            }
            
            return 0f;
        }
        
        private void UpdateSkillCooldown(string skillName, float cooldown)
        {
            if (cooldown <= 0)
            {
                // Remove cooldown display if exists
                if (cooldownDisplays.ContainsKey(skillName))
                {
                    Destroy(cooldownDisplays[skillName]);
                    cooldownDisplays.Remove(skillName);
                }
                return;
            }
            
            // Create or update cooldown display
            GameObject cooldownObj;
            if (!cooldownDisplays.ContainsKey(skillName))
            {
                cooldownObj = new GameObject($"Cooldown_{skillName}");
                cooldownObj.transform.SetParent(cooldownsPanel.transform, false);
                
                var rectTransform = cooldownObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(80, 50);
                
                // Background
                var bg = cooldownObj.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.8f);
                
                // Skill name
                var nameObj = new GameObject("SkillName");
                nameObj.transform.SetParent(cooldownObj.transform, false);
                var nameText = nameObj.AddComponent<Text>();
                nameText.text = skillName;
                nameText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
                nameText.fontSize = 12;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.MiddleCenter;
                
                var nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.5f);
                nameRect.anchorMax = new Vector2(1, 1);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;
                
                // Cooldown text
                var cdObj = new GameObject("CooldownText");
                cdObj.transform.SetParent(cooldownObj.transform, false);
                var cdText = cdObj.AddComponent<Text>();
                cdText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                cdText.fontSize = 16;
                cdText.fontStyle = FontStyle.Bold;
                cdText.alignment = TextAnchor.MiddleCenter;
                
                var cdRect = cdObj.GetComponent<RectTransform>();
                cdRect.anchorMin = new Vector2(0, 0);
                cdRect.anchorMax = new Vector2(1, 0.5f);
                cdRect.offsetMin = Vector2.zero;
                cdRect.offsetMax = Vector2.zero;
                
                cooldownDisplays[skillName] = cooldownObj;
            }
            else
            {
                cooldownObj = cooldownDisplays[skillName];
            }
            
            // Update cooldown text
            var cooldownText = cooldownObj.transform.Find("CooldownText")?.GetComponent<Text>();
            if (cooldownText != null)
            {
                cooldownText.text = $"{cooldown:F1}s";
                // Color based on cooldown amount
                float t = cooldown / 1f; // Assuming 1 second max cooldown for Flash Jump
                cooldownText.color = Color.Lerp(new Color(0.5f, 1f, 0.5f), new Color(1f, 0.5f, 0.5f), t);
            }
            
            // Update background color
            var bgImage = cooldownObj.GetComponent<Image>();
            if (bgImage != null)
            {
                float alpha = Mathf.Lerp(0.3f, 0.8f, cooldown / 1f);
                bgImage.color = new Color(0, 0, 0, alpha);
            }
        }
    }
}