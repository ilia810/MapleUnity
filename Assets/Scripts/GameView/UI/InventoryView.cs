using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView.UI
{
    public class InventoryView : MonoBehaviour
    {
        private Player player;
        private Text inventoryText;
        private GameObject inventoryPanel;

        void Start()
        {
            CreateUI();
            
            // Find player through GameManager - use coroutine to wait for initialization
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
            Debug.Log("InventoryView connected to player");
        }

        private void CreateUI()
        {
            // Create inventory panel
            inventoryPanel = new GameObject("InventoryPanel");
            inventoryPanel.transform.SetParent(transform, false);
            
            RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            panelRect.sizeDelta = new Vector2(200, 300);

            // Add background
            Image bg = inventoryPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            // Add title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(inventoryPanel.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 30);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Inventory";
            titleText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 18);
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Add inventory content text
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(inventoryPanel.transform, false);
            
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0, -20);
            contentRect.sizeDelta = new Vector2(-20, -60);

            inventoryText = contentObj.AddComponent<Text>();
            inventoryText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Verdana" }, 14);
            inventoryText.fontSize = 14;
            inventoryText.color = Color.white;
            inventoryText.alignment = TextAnchor.UpperLeft;
            inventoryText.text = "Empty\n\nPress I to toggle";
        }

        public void SetPlayer(Player player)
        {
            if (this.player != null && this.player.Inventory != null)
            {
                this.player.Inventory.ItemAdded -= OnItemAdded;
                this.player.Inventory.ItemRemoved -= OnItemRemoved;
            }

            this.player = player;

            if (this.player != null && this.player.Inventory != null)
            {
                this.player.Inventory.ItemAdded += OnItemAdded;
                this.player.Inventory.ItemRemoved += OnItemRemoved;
                UpdateInventoryDisplay();
            }
        }

        private void OnItemAdded(int itemId, int quantity)
        {
            UpdateInventoryDisplay();
            Debug.Log($"Picked up {quantity}x {GetItemName(itemId)}");
        }

        private void OnItemRemoved(int itemId, int quantity)
        {
            UpdateInventoryDisplay();
        }

        private void UpdateInventoryDisplay()
        {
            if (player == null || inventoryText == null) return;

            var items = player.Inventory.GetAllItems();
            if (items.Count == 0)
            {
                inventoryText.text = "Empty";
                return;
            }

            string display = "";
            foreach (var item in items)
            {
                display += $"{GetItemName(item.Key)} x{item.Value}\n";
            }

            inventoryText.text = display;
        }

        private string GetItemName(int itemId)
        {
            switch (itemId)
            {
                case 2000000: return "Red Potion";
                case 2000001: return "Orange Potion";
                case 2000002: return "White Potion";
                case 2000003: return "Blue Potion";
                case 2000004: return "Mana Elixir";
                case 2000005: return "Dexterity Potion";
                case 2040002: return "10% Helmet DEF";
                default: return $"Item {itemId}";
            }
        }

        void Update()
        {
            // Toggle inventory with I key
            if (Input.GetKeyDown(KeyCode.I))
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }

        void OnDestroy()
        {
            if (player != null && player.Inventory != null)
            {
                player.Inventory.ItemAdded -= OnItemAdded;
                player.Inventory.ItemRemoved -= OnItemRemoved;
            }
        }
    }
}