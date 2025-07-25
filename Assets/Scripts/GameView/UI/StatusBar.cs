using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView.UI
{
    public class StatusBar : MonoBehaviour
    {
        private Player player;
        private GameObject hpBar;
        private GameObject mpBar;
        private Image hpFill;
        private Image mpFill;
        private Text hpText;
        private Text mpText;
        private Text levelText;

        void Start()
        {
            CreateUI();
            StartCoroutine(WaitForPlayer());
        }

        private System.Collections.IEnumerator WaitForPlayer()
        {
            GameManager gameManager = null;
            
            // Wait for GameManager
            while (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                yield return null;
            }
            
            // Wait for Player
            while (gameManager.Player == null)
            {
                yield return null;
            }
            
            SetPlayer(gameManager.Player);
        }

        private void CreateUI()
        {
            // Create status bar container
            GameObject statusContainer = new GameObject("StatusContainer");
            statusContainer.transform.SetParent(transform, false);
            
            RectTransform containerRect = statusContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(10, -10);
            containerRect.sizeDelta = new Vector2(250, 80);

            // Create HP Bar
            hpBar = CreateBar(statusContainer, "HP Bar", new Vector2(0, -5), Color.red);
            hpFill = hpBar.transform.Find("Fill").GetComponent<Image>();
            
            // Create HP Text
            GameObject hpTextObj = new GameObject("HP Text");
            hpTextObj.transform.SetParent(hpBar.transform, false);
            RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.sizeDelta = Vector2.zero;
            hpTextRect.anchoredPosition = Vector2.zero;
            
            hpText = hpTextObj.AddComponent<Text>();
            hpText.text = "HP: 100/100";
            hpText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 12);
            hpText.fontSize = 12;
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleCenter;

            // Create MP Bar
            mpBar = CreateBar(statusContainer, "MP Bar", new Vector2(0, -35), Color.blue);
            mpFill = mpBar.transform.Find("Fill").GetComponent<Image>();
            
            // Create MP Text
            GameObject mpTextObj = new GameObject("MP Text");
            mpTextObj.transform.SetParent(mpBar.transform, false);
            RectTransform mpTextRect = mpTextObj.AddComponent<RectTransform>();
            mpTextRect.anchorMin = Vector2.zero;
            mpTextRect.anchorMax = Vector2.one;
            mpTextRect.sizeDelta = Vector2.zero;
            mpTextRect.anchoredPosition = Vector2.zero;
            
            mpText = mpTextObj.AddComponent<Text>();
            mpText.text = "MP: 50/50";
            mpText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 12);
            mpText.fontSize = 12;
            mpText.color = Color.white;
            mpText.alignment = TextAnchor.MiddleCenter;

            // Create Level Text
            GameObject levelTextObj = new GameObject("Level Text");
            levelTextObj.transform.SetParent(statusContainer.transform, false);
            RectTransform levelTextRect = levelTextObj.AddComponent<RectTransform>();
            levelTextRect.anchorMin = new Vector2(0, 0);
            levelTextRect.anchorMax = new Vector2(1, 0);
            levelTextRect.pivot = new Vector2(0.5f, 0);
            levelTextRect.anchoredPosition = new Vector2(0, 5);
            levelTextRect.sizeDelta = new Vector2(0, 20);
            
            levelText = levelTextObj.AddComponent<Text>();
            levelText.text = "Level 1";
            levelText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 14);
            levelText.fontSize = 14;
            levelText.color = Color.yellow;
            levelText.alignment = TextAnchor.MiddleCenter;
        }

        private GameObject CreateBar(GameObject parent, string name, Vector2 position, Color color)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent.transform, false);
            
            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0.5f, 1);
            barRect.anchoredPosition = position;
            barRect.sizeDelta = new Vector2(-10, 25);

            // Background
            Image bgImage = barObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = new Vector2(-4, -4);
            fillRect.anchoredPosition = new Vector2(0, 0);
            
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = color;

            return barObj;
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

            // Update HP
            float hpPercent = (float)player.HP / player.MaxHP;
            hpFill.fillAmount = hpPercent;
            hpText.text = $"HP: {player.HP}/{player.MaxHP}";

            // Update MP
            float mpPercent = (float)player.MP / player.MaxMP;
            mpFill.fillAmount = mpPercent;
            mpText.text = $"MP: {player.MP}/{player.MaxMP}";

            // Update Level
            levelText.text = $"Level {player.Level}";
        }
    }
}