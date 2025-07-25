using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView.UI
{
    public class ExperienceBar : MonoBehaviour
    {
        private Player player;
        private GameObject expBar;
        private Image expFill;
        private Text expText;

        void Start()
        {
            CreateUI();
            StartCoroutine(WaitForPlayer());
        }

        private System.Collections.IEnumerator WaitForPlayer()
        {
            GameManager gameManager = null;
            
            while (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                yield return null;
            }
            
            while (gameManager.Player == null)
            {
                yield return null;
            }
            
            SetPlayer(gameManager.Player);
        }

        private void CreateUI()
        {
            // Create experience bar at bottom of screen
            expBar = new GameObject("ExperienceBar");
            expBar.transform.SetParent(transform, false);
            
            RectTransform barRect = expBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 5);
            barRect.sizeDelta = new Vector2(-20, 15);

            // Background
            Image bgImage = expBar.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(expBar.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = new Vector2(-2, -2);
            fillRect.anchoredPosition = Vector2.zero;
            
            expFill = fillObj.AddComponent<Image>();
            expFill.color = new Color(1f, 0.8f, 0f, 0.9f); // Gold color
            expFill.type = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(expBar.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            expText = textObj.AddComponent<Text>();
            expText.text = "0 / 100 EXP (0.00%)";
            expText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 11);
            expText.fontSize = 11;
            expText.color = Color.white;
            expText.alignment = TextAnchor.MiddleCenter;
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
            UpdateDisplay();
        }

        void Update()
        {
            if (player != null)
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (player == null) return;

            // For now, using placeholder values since Player doesn't have EXP yet
            int currentExp = 0;
            int expToNextLevel = 100;
            float expPercent = 0f;

            expFill.fillAmount = expPercent;
            expText.text = $"{currentExp} / {expToNextLevel} EXP ({expPercent * 100:F2}%)";
        }
    }
}