using UnityEngine;
using MapleClient.GameLogic;

namespace MapleClient.GameView
{
    public class PortalView : MonoBehaviour
    {
        private Portal portal;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                
                // Create a simple portal visual
                var texture = new Texture2D(32, 48);
                var pixels = new Color[32 * 48];
                
                // Create oval shape
                for (int y = 0; y < 48; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float cx = 16f;
                        float cy = 24f;
                        float rx = 14f;
                        float ry = 20f;
                        
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        
                        if (dx * dx + dy * dy <= 1f)
                        {
                            // Portal color based on type
                            pixels[y * 32 + x] = GetPortalColor();
                        }
                        else
                        {
                            pixels[y * 32 + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                texture.filterMode = FilterMode.Point;
                
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 48), new UnityEngine.Vector2(0.5f, 0));
            }
        }

        public void SetPortal(Portal portal)
        {
            this.portal = portal;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (portal != null)
            {
                // Convert from game logic coordinates to Unity coordinates
                transform.position = new Vector3(portal.X / 100f, portal.Y / 100f, 0);
                
                // Hide spawn and hidden portals
                gameObject.SetActive(portal.Type != PortalType.Spawn && portal.Type != PortalType.Hidden);
                
                // Update color based on type
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = GetPortalColor();
                }
            }
        }

        private Color GetPortalColor()
        {
            if (portal == null) return new Color(0.5f, 0.5f, 1f, 0.7f); // Default blue
            
            switch (portal.Type)
            {
                case PortalType.Regular:
                case PortalType.Normal:
                    return new Color(0.5f, 0.5f, 1f, 0.7f); // Blue
                case PortalType.Script:
                    return new Color(1f, 0.5f, 0.5f, 0.7f); // Red
                default:
                    return new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray
            }
        }

        void Update()
        {
            // Simple animation - pulsing effect
            if (spriteRenderer != null && portal != null && 
                (portal.Type == PortalType.Regular || portal.Type == PortalType.Normal))
            {
                float pulse = Mathf.Sin(Time.time * 2f) * 0.1f + 0.9f;
                transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }
}