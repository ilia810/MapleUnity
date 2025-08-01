using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    public class PlayerView : MonoBehaviour
    {
        private Player player;
        private MapleCharacterRenderer characterRenderer;
        private ICharacterDataProvider characterData;
        
        // Interpolation for smooth rendering
        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private bool useInterpolation = true;
        private float manualInterpolationFactor = -1f; // -1 means use automatic
        private GameWorld gameWorld;

        private void Awake()
        {
            // The character renderer will handle all sprite rendering
            characterRenderer = GetComponent<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                characterRenderer = gameObject.AddComponent<MapleCharacterRenderer>();
                Debug.Log("Created MapleCharacterRenderer component");
            }
            
            // Add a collider for visualization (Unity physics isn't used, this is just visual)
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, 0.6f); // Match player dimensions
            collider.offset = new Vector2(0, 0);
            collider.isTrigger = true; // Don't interfere with game logic
            
            Debug.Log($"PlayerView Awake - GameObject: {gameObject.name}, Position: {transform.position}");
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
            Debug.Log($"SetPlayer called with player: {player?.Name ?? "null"}");
            
            // Initialize positions for interpolation
            if (player != null)
            {
                var pos = new Vector3(player.Position.X, player.Position.Y, 0);
                transform.position = pos;
                previousPosition = pos;
                currentPosition = pos;
            }
            
            // Initialize character renderer with player data
            if (characterRenderer != null && characterData != null)
            {
                Debug.Log("Initializing character renderer...");
                characterRenderer.Initialize(player, characterData);
                
                // Set default appearance (can be customized later)
                characterRenderer.SetCharacterAppearance(0, 20000, 30000);
            }
            else
            {
                Debug.LogWarning($"Cannot initialize character renderer - renderer: {characterRenderer != null}, data: {characterData != null}");
            }
        }
        
        public void SetCharacterDataProvider(ICharacterDataProvider provider)
        {
            this.characterData = provider;
            Debug.Log($"SetCharacterDataProvider called with provider: {provider != null}");
            
            // If player is already set, initialize the renderer
            if (player != null && characterRenderer != null)
            {
                Debug.Log("Player already set, initializing character renderer now...");
                characterRenderer.Initialize(player, characterData);
                
                // Set default appearance
                characterRenderer.SetCharacterAppearance(0, 20000, 30000);
            }
        }
        
        private void Start()
        {
            // Find GameWorld for interpolation
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var fieldInfo = typeof(GameManager).GetField("gameWorld", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    gameWorld = fieldInfo.GetValue(gameManager) as GameWorld;
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (player != null)
            {
                // Store positions for interpolation
                previousPosition = currentPosition;
                currentPosition = new Vector3(player.Position.X, player.Position.Y, 0);
            }
        }

        private void Update()
        {
            // The MapleCharacterRenderer handles all visual updates
            // We just need to handle any additional effects or UI elements here
            
            // Sync position with game logic
            if (player != null)
            {
                // Interpolate position for smooth visual movement
                if (useInterpolation)
                {
                    float interpolationFactor = manualInterpolationFactor >= 0f ? 
                        manualInterpolationFactor : 
                        (gameWorld != null ? gameWorld.GetPhysicsInterpolationFactor() : 0f);
                    transform.position = Vector3.Lerp(previousPosition, currentPosition, interpolationFactor);
                }
                else
                {
                    // Fallback to direct position sync
                    transform.position = new Vector3(player.Position.X, player.Position.Y, 0);
                }
                
                // Debug movement issues
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                {
                    Debug.Log($"Player State: {player.State}, Position: {player.Position}, Velocity: {player.Velocity}, Grounded: {player.IsGrounded}, Speed stat: {player.Speed}");
                    Debug.Log($"Unity Position: {transform.position}, Interpolation: {manualInterpolationFactor}");
                }
            }
        }
        
        public void SetInterpolationFactor(float factor)
        {
            manualInterpolationFactor = Mathf.Clamp01(factor);
        }
    }
}