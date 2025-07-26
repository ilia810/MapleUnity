using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameView
{
    public class PlayerView : MonoBehaviour
    {
        private Player player;
        private MapleCharacterRenderer characterRenderer;
        private ICharacterDataProvider characterData;

        private void Awake()
        {
            // The character renderer will handle all sprite rendering
            characterRenderer = GetComponent<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                characterRenderer = gameObject.AddComponent<MapleCharacterRenderer>();
            }
            
            // Add a collider for visualization (Unity physics isn't used, this is just visual)
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, 0.6f); // Match player dimensions
            collider.offset = new Vector2(0, 0);
            collider.isTrigger = true; // Don't interfere with game logic
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
            
            // Initialize character renderer with player data
            if (characterRenderer != null && characterData != null)
            {
                characterRenderer.Initialize(player, characterData);
                
                // Set default appearance (can be customized later)
                characterRenderer.SetCharacterAppearance(0, 20000, 30000);
            }
        }
        
        public void SetCharacterDataProvider(ICharacterDataProvider provider)
        {
            this.characterData = provider;
            
            // If player is already set, initialize the renderer
            if (player != null && characterRenderer != null)
            {
                characterRenderer.Initialize(player, characterData);
            }
        }

        private void Update()
        {
            // The MapleCharacterRenderer handles all visual updates
            // We just need to handle any additional effects or UI elements here
            
            // Sync position with game logic
            if (player != null)
            {
                var newPos = new UnityEngine.Vector3(
                    player.Position.X / 100f,
                    player.Position.Y / 100f,
                    0f
                );
                
                // Debug movement issues
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                {
                    Debug.Log($"Player State: {player.State}, Velocity: {player.Velocity}, Grounded: {player.IsGrounded}, Speed stat: {player.Speed}");
                }
                
                transform.position = newPos;
            }
        }
    }
}