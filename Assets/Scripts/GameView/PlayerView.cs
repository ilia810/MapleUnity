using UnityEngine;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    public class PlayerView : MonoBehaviour
    {
        private Player player;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                // Create a simple colored square as placeholder
                var texture = new Texture2D(32, 64);
                var pixels = new Color[32 * 64];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.blue;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0));
            }
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        private void Update()
        {
            if (player != null)
            {
                // Update position - convert from game logic coordinates to Unity coordinates
                transform.position = new Vector3(player.Position.X / 100f, player.Position.Y / 100f, 0);
                
                // Flip sprite based on movement direction
                if (player.Velocity.X < 0)
                    spriteRenderer.flipX = true;
                else if (player.Velocity.X > 0)
                    spriteRenderer.flipX = false;
                
                // Update visual based on player state
                UpdateVisualState();
            }
        }
        
        private void UpdateVisualState()
        {
            // Simple visual feedback based on state
            switch (player.State)
            {
                case PlayerState.Walking:
                    spriteRenderer.color = Color.green;
                    transform.localScale = Vector3.one;
                    break;
                case PlayerState.Jumping:
                    spriteRenderer.color = Color.yellow;
                    transform.localScale = Vector3.one;
                    break;
                case PlayerState.Crouching:
                    spriteRenderer.color = Color.magenta;
                    transform.localScale = new Vector3(1f, 0.5f, 1f); // Make shorter when crouching
                    break;
                case PlayerState.Climbing:
                    spriteRenderer.color = Color.cyan;
                    transform.localScale = Vector3.one;
                    spriteRenderer.flipX = false; // Always face forward on ladder
                    break;
                case PlayerState.Standing:
                default:
                    spriteRenderer.color = Color.blue;
                    transform.localScale = Vector3.one;
                    break;
            }
        }
    }
}