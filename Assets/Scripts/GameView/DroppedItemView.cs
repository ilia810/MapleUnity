using UnityEngine;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    public class DroppedItemView : MonoBehaviour
    {
        private DroppedItem droppedItem;
        private SpriteRenderer spriteRenderer;
        private float bobAmount = 0.1f;
        private float bobSpeed = 2f;
        private float initialY;
        private float glowTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                // Create a simple colored square as placeholder for item
                var texture = new Texture2D(24, 24);
                var pixels = new Color[24 * 24];
                
                // Different colors for different item types
                Color itemColor = Color.yellow; // Default color
                
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = itemColor;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f));
            }
        }

        public void SetDroppedItem(DroppedItem item)
        {
            this.droppedItem = item;
            
            // Set initial position
            transform.position = new Vector3(item.Position.X / 100f, item.Position.Y / 100f, 0);
            initialY = transform.position.y;
            
            // Set color based on item type
            UpdateItemAppearance();
        }

        private void UpdateItemAppearance()
        {
            if (droppedItem == null || spriteRenderer == null) return;
            
            // Different colors for different item types
            Color itemColor = Color.yellow;
            
            if (droppedItem.ItemId >= 2000000 && droppedItem.ItemId < 2100000)
            {
                // Consumables (potions)
                itemColor = droppedItem.ItemId switch
                {
                    2000000 => new Color(1f, 0.2f, 0.2f), // Red Potion
                    2000001 => new Color(1f, 0.6f, 0f),   // Orange Potion
                    2000002 => Color.white,                // White Potion
                    2000003 => new Color(0.2f, 0.4f, 1f), // Blue Potion
                    _ => new Color(0.8f, 0.2f, 0.8f)      // Other potions
                };
            }
            else if (droppedItem.ItemId >= 2040000 && droppedItem.ItemId < 2050000)
            {
                // Scrolls
                itemColor = new Color(0.8f, 0.8f, 0.2f);
            }
            
            // Create gradient effect
            var texture = new Texture2D(24, 24);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(12, 12));
                    float fade = 1f - (distance / 12f);
                    fade = Mathf.Clamp01(fade);
                    
                    Color pixelColor = itemColor * fade;
                    pixelColor.a = 1f;
                    texture.SetPixel(x, y, pixelColor);
                }
            }
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            if (droppedItem == null) return;
            
            // Bobbing effect
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = new Vector3(transform.position.x, initialY + yOffset, transform.position.z);
            
            // Glow effect
            glowTimer += Time.deltaTime;
            float glowIntensity = (Mathf.Sin(glowTimer * 3f) + 1f) * 0.25f + 0.75f;
            spriteRenderer.color = new Color(glowIntensity, glowIntensity, glowIntensity, 1f);
            
            // Check if expired
            if (droppedItem.IsExpired)
            {
                // Fade out before destroying
                var color = spriteRenderer.color;
                color.a = Mathf.Max(0, color.a - Time.deltaTime * 2f);
                spriteRenderer.color = color;
                
                if (color.a <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            droppedItem = null;
        }
    }
}