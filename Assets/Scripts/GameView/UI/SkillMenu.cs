using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;

namespace MapleClient.GameView.UI
{
    public class SkillMenu : MonoBehaviour
    {
        private Player player;
        private GameObject skillPanel;
        private bool isVisible = false;

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
            // Create skill panel
            skillPanel = new GameObject("SkillPanel");
            skillPanel.transform.SetParent(transform, false);
            
            RectTransform panelRect = skillPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 300);

            // Background
            Image bg = skillPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(skillPanel.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 30);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Skills";
            titleText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 20);
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(skillPanel.transform, false);
            
            RectTransform closeRect = closeBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            closeRect.sizeDelta = new Vector2(25, 25);

            Button closeButton = closeBtn.AddComponent<Button>();
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = Color.red;

            Text closeText = new GameObject("Text").AddComponent<Text>();
            closeText.transform.SetParent(closeBtn.transform, false);
            closeText.text = "X";
            closeText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 16);
            closeText.fontSize = 16;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;

            closeButton.onClick.AddListener(() => ToggleVisibility());

            // Skill list container
            GameObject skillContainer = new GameObject("SkillContainer");
            skillContainer.transform.SetParent(skillPanel.transform, false);
            
            RectTransform containerRect = skillContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, -20);
            containerRect.sizeDelta = new Vector2(-20, -60);

            // Add sample skills
            CreateSkillEntry(skillContainer, "Basic Attack", "K", 0);
            CreateSkillEntry(skillContainer, "Jump", "Space", 1);
            CreateSkillEntry(skillContainer, "Crouch", "Down", 2);
            CreateSkillEntry(skillContainer, "Climb", "Up (at ladder)", 3);

            // Hide by default
            skillPanel.SetActive(false);
        }

        private void CreateSkillEntry(GameObject parent, string skillName, string keybind, int index)
        {
            GameObject entry = new GameObject($"Skill_{skillName}");
            entry.transform.SetParent(parent.transform, false);
            
            RectTransform entryRect = entry.AddComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(0.5f, 1);
            entryRect.anchoredPosition = new Vector2(0, -20 - (index * 50));
            entryRect.sizeDelta = new Vector2(-10, 45);

            // Background
            Image bg = entry.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Skill name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(entry.transform, false);
            
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.7f, 1);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.anchoredPosition = new Vector2(10, 0);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = skillName;
            nameText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 14);
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;

            // Keybind
            GameObject keyObj = new GameObject("Keybind");
            keyObj.transform.SetParent(entry.transform, false);
            
            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0.7f, 0);
            keyRect.anchorMax = new Vector2(1, 1);
            keyRect.sizeDelta = new Vector2(-10, 0);
            keyRect.anchoredPosition = Vector2.zero;

            Text keyText = keyObj.AddComponent<Text>();
            keyText.text = keybind;
            keyText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 12);
            keyText.fontSize = 12;
            keyText.color = Color.yellow;
            keyText.alignment = TextAnchor.MiddleRight;
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        void Update()
        {
            // Toggle with K key
            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleVisibility();
            }
        }

        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            skillPanel.SetActive(isVisible);
        }
    }
}