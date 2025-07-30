using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    /// <summary>
    /// Manages contextual input prompts to help players understand available actions
    /// </summary>
    public class InputPromptManager : MonoBehaviour
    {
        [Header("UI Settings")]
        private Canvas canvas;
        private GameObject promptContainer;
        private float promptFadeSpeed = 2f;
        
        // Active prompts
        private Dictionary<string, GameObject> activePrompts = new Dictionary<string, GameObject>();
        private Dictionary<string, float> promptTimers = new Dictionary<string, float>();
        
        // References
        private GameWorld gameWorld;
        private Player player;
        
        // Prompt conditions
        private bool wasNearLadder = false;
        private bool hadDoubleJump = false;
        private bool hadFlashJump = false;
        private PlayerState lastPlayerState = PlayerState.Standing;
        
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
            // Create or find canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 900; // Below other UI
                
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Create prompt container
            promptContainer = new GameObject("PromptContainer");
            promptContainer.transform.SetParent(canvas.transform, false);
            
            var rectTransform = promptContainer.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-20, 20);
            rectTransform.sizeDelta = new Vector2(300, 400);
            
            // Vertical layout
            var layout = promptContainer.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.LowerRight;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }
        
        void Update()
        {
            if (player == null) return;
            
            CheckForPrompts();
            UpdatePromptTimers();
        }
        
        private void CheckForPrompts()
        {
            // Check ladder proximity
            bool nearLadder = IsNearLadder();
            if (nearLadder && !wasNearLadder && player.State != PlayerState.Climbing)
            {
                ShowPrompt("Ladder", "[↑/↓] Climb ladder", 5f);
            }
            else if (!nearLadder && wasNearLadder)
            {
                HidePrompt("Ladder");
            }
            wasNearLadder = nearLadder;
            
            // Check for new abilities
            bool hasDoubleJump = HasAbility("hasDoubleJump");
            bool hasFlashJump = HasAbility("hasFlashJump");
            
            if (hasDoubleJump && !hadDoubleJump)
            {
                ShowPrompt("DoubleJump", "[Alt] Double Jump (while airborne)", 10f);
            }
            hadDoubleJump = hasDoubleJump;
            
            if (hasFlashJump && !hadFlashJump)
            {
                ShowPrompt("FlashJump", "[Alt + →/←] Flash Jump (while airborne)", 10f);
            }
            hadFlashJump = hasFlashJump;
            
            // State-specific prompts
            if (player.State != lastPlayerState)
            {
                HandleStateChangePrompts(lastPlayerState, player.State);
                lastPlayerState = player.State;
            }
            
            // Movement modifier prompts
            var modifiers = player.GetActiveModifiers();
            foreach (var modifier in modifiers)
            {
                if (modifier.Id == "slippery_surface" && !activePrompts.ContainsKey("IceWarning"))
                {
                    ShowPrompt("IceWarning", "⚠ Slippery surface! Movement is harder to control", 3f);
                }
                else if (modifier.Id.StartsWith("stun_") && !activePrompts.ContainsKey("StunWarning"))
                {
                    ShowPrompt("StunWarning", "⚠ You are stunned! Cannot move or jump", 2f);
                }
            }
            
            // Basic controls reminder (show once at start)
            if (!activePrompts.ContainsKey("BasicControls") && Time.time < 5f)
            {
                ShowPrompt("BasicControls", "[←/→] Move | [Alt] Jump | [Ctrl] Attack", 8f);
            }
        }
        
        private void HandleStateChangePrompts(PlayerState oldState, PlayerState newState)
        {
            // Climbing prompts
            if (newState == PlayerState.Climbing)
            {
                ShowPrompt("ClimbingControls", "[↑/↓] Climb | [←/→] + [Alt] Jump off", 5f);
            }
            else if (oldState == PlayerState.Climbing)
            {
                HidePrompt("ClimbingControls");
            }
            
            // Jumping prompts
            if (newState == PlayerState.Jumping && (hadDoubleJump || hadFlashJump))
            {
                if (hadDoubleJump)
                    ShowPrompt("AirborneDouble", "[Alt] Double Jump available!", 2f);
                if (hadFlashJump)
                    ShowPrompt("AirborneFlash", "[Alt + →/←] Flash Jump available!", 2f);
            }
            
            // Crouching prompt
            if (newState == PlayerState.Crouching)
            {
                ShowPrompt("CrouchingInfo", "[↓] Crouching (reduces hitbox)", 3f);
            }
        }
        
        private bool IsNearLadder()
        {
            if (gameWorld?.CurrentMap?.Ladders == null) return false;
            
            var playerPos = player.Position;
            foreach (var ladder in gameWorld.CurrentMap.Ladders)
            {
                float ladderX = ladder.X / 100f;
                float distance = System.Math.Abs(playerPos.X - ladderX);
                
                if (distance < 0.3f && 
                    playerPos.Y >= ladder.Y2 / 100f && 
                    playerPos.Y <= ladder.Y1 / 100f)
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool HasAbility(string fieldName)
        {
            if (player == null) return false;
            
            var fieldInfo = typeof(Player).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                return (bool)fieldInfo.GetValue(player);
            }
            return false;
        }
        
        private void ShowPrompt(string id, string text, float duration)
        {
            if (activePrompts.ContainsKey(id))
            {
                // Reset timer if prompt already exists
                promptTimers[id] = duration;
                return;
            }
            
            // Create new prompt
            GameObject promptObj = new GameObject($"Prompt_{id}");
            promptObj.transform.SetParent(promptContainer.transform, false);
            
            var rectTransform = promptObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(280, 40);
            
            // Background
            var bg = promptObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            // Add rounded corners effect
            var outline = promptObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            outline.effectDistance = new Vector2(1, 1);
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(promptObj.transform, false);
            
            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            textComp.fontSize = 14;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            
            // Add icon based on prompt type
            AddPromptIcon(promptObj, id);
            
            // Fade in animation
            var canvasGroup = promptObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            StartCoroutine(FadePrompt(canvasGroup, 1f, 0.3f));
            
            activePrompts[id] = promptObj;
            promptTimers[id] = duration;
        }
        
        private void AddPromptIcon(GameObject promptObj, string promptId)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(promptObj.transform, false);
            
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(20, 20);
            
            var iconImage = iconObj.AddComponent<Image>();
            
            // Create simple icon based on prompt type
            var texture = new Texture2D(20, 20);
            var pixels = new Color[400];
            
            Color iconColor = Color.white;
            if (promptId.Contains("Jump")) iconColor = new Color(0.5f, 0.8f, 1f);
            else if (promptId.Contains("Ladder") || promptId.Contains("Climb")) iconColor = new Color(0.5f, 1f, 0.5f);
            else if (promptId.Contains("Warning")) iconColor = new Color(1f, 0.8f, 0.3f);
            else if (promptId.Contains("Controls")) iconColor = new Color(0.8f, 0.8f, 0.8f);
            
            // Simple icon patterns
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = iconColor;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            iconImage.sprite = Sprite.Create(texture, new Rect(0, 0, 20, 20), new Vector2(0.5f, 0.5f));
        }
        
        private void HidePrompt(string id)
        {
            if (activePrompts.ContainsKey(id))
            {
                var promptObj = activePrompts[id];
                var canvasGroup = promptObj.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    StartCoroutine(FadeAndDestroy(canvasGroup, promptObj, id));
                }
                else
                {
                    Destroy(promptObj);
                    activePrompts.Remove(id);
                    promptTimers.Remove(id);
                }
            }
        }
        
        private void UpdatePromptTimers()
        {
            List<string> toRemove = new List<string>();
            
            // Create a copy of the keys to avoid modification during iteration
            var keys = new List<string>(promptTimers.Keys);
            
            foreach (var key in keys)
            {
                if (promptTimers.ContainsKey(key))
                {
                    promptTimers[key] -= Time.deltaTime;
                    if (promptTimers[key] <= 0)
                    {
                        toRemove.Add(key);
                    }
                }
            }
            
            foreach (var id in toRemove)
            {
                HidePrompt(id);
            }
        }
        
        private System.Collections.IEnumerator FadePrompt(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < duration && canvasGroup != null)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
        }
        
        private System.Collections.IEnumerator FadeAndDestroy(CanvasGroup canvasGroup, GameObject obj, string id)
        {
            if (canvasGroup != null && obj != null)
            {
                yield return StartCoroutine(FadePrompt(canvasGroup, 0f, 0.3f));
            }
            
            if (obj != null)
            {
                Destroy(obj);
            }
            
            if (activePrompts.ContainsKey(id))
            {
                activePrompts.Remove(id);
            }
            
            if (promptTimers.ContainsKey(id))
            {
                promptTimers.Remove(id);
            }
        }
        
        public void ShowCustomPrompt(string id, string text, float duration = 3f)
        {
            ShowPrompt(id, text, duration);
        }
        
        public void ClearAllPrompts()
        {
            foreach (var kvp in activePrompts)
            {
                Destroy(kvp.Value);
            }
            activePrompts.Clear();
            promptTimers.Clear();
        }
    }
}