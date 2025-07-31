using UnityEngine;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;
using System.Linq;
// Disambiguate Vector2 references
using UnityVector2 = UnityEngine.Vector2;
using MapleVector2 = MapleClient.GameLogic.Vector2;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView.Debugging
{
    /// <summary>
    /// Runtime debug component for visualizing foothold collision in real-time
    /// Add this to your scene to see collision detection visualization
    /// </summary>
    public class FootholdCollisionDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Transform playerTransform;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugView = true;
        [SerializeField] private bool showFootholds = true;
        [SerializeField] private bool showPlayerCollision = true;
        [SerializeField] private bool showGroundRays = true;
        [SerializeField] private bool showVelocityVector = true;
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showPerformanceStats = true;
        
        [Header("Visualization")]
        [SerializeField] private Color footholdColor = Color.green;
        [SerializeField] private Color playerBoxColor = Color.blue;
        [SerializeField] private Color groundRayColor = Color.yellow;
        [SerializeField] private Color velocityColor = Color.red;
        [SerializeField] private Color collisionPointColor = Color.magenta;
        
        private IFootholdService footholdService;
        private Player gameLogicPlayer;
        private List<Foothold> cachedFootholds = new List<Foothold>();
        
        // Performance tracking
        private float frameTime = 0f;
        private int frameCount = 0;
        private float avgFrameTime = 0f;
        private float updateTimer = 0f;
        
        // Debug info
        private string debugText = "";
        private GUIStyle debugStyle;
        
        void Start()
        {
            // Try to find GameManager if not assigned
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
            
            // Setup debug style
            debugStyle = new GUIStyle();
            debugStyle.normal.textColor = Color.white;
            debugStyle.fontSize = 12;
            debugStyle.fontStyle = FontStyle.Bold;
            
            // Get foothold service
            if (gameManager != null)
            {
                footholdService = gameManager.FootholdService;
                RefreshFootholds();
            }
        }
        
        void Update()
        {
            if (!enableDebugView) return;
            
            // Track performance
            frameTime += Time.deltaTime;
            frameCount++;
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= 0.5f) // Update every 0.5 seconds
            {
                avgFrameTime = (frameTime / frameCount) * 1000f; // Convert to ms
                frameTime = 0f;
                frameCount = 0;
                updateTimer = 0f;
            }
            
            // Update references
            UpdateReferences();
            
            // Update debug text
            UpdateDebugText();
        }
        
        void OnDrawGizmos()
        {
            if (!enableDebugView || !Application.isPlaying) return;
            
            DrawFootholds();
            DrawPlayerCollision();
            DrawGroundDetection();
            DrawVelocity();
        }
        
        void OnGUI()
        {
            if (!enableDebugView || !showDebugInfo) return;
            
            // Background box
            GUI.Box(new Rect(10, 10, 400, 300), "");
            
            // Title
            GUI.Label(new Rect(15, 15, 390, 20), "Foothold Collision Debug", debugStyle);
            
            // Debug text
            GUI.Label(new Rect(15, 40, 390, 250), debugText, debugStyle);
            
            // Performance stats
            if (showPerformanceStats)
            {
                GUI.Label(new Rect(15, 280, 200, 20), $"Frame Time: {avgFrameTime:F2}ms", debugStyle);
            }
        }
        
        private void UpdateReferences()
        {
            // Try to get player reference
            if (gameLogicPlayer == null && gameManager != null)
            {
                gameLogicPlayer = gameManager.Player;
            }
            
            // Try to find player transform
            if (playerTransform == null)
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }
        }
        
        private void UpdateDebugText()
        {
            debugText = "";
            
            if (gameLogicPlayer != null)
            {
                // Player state
                debugText += $"=== Player State ===\n";
                debugText += $"Position: {gameLogicPlayer.Position}\n";
                debugText += $"Velocity: {gameLogicPlayer.Velocity}\n";
                debugText += $"State: {gameLogicPlayer.State}\n";
                debugText += $"Grounded: {gameLogicPlayer.IsGrounded}\n";
                debugText += $"Jumping: {gameLogicPlayer.IsJumping}\n\n";
                
                // Collision info
                debugText += $"=== Collision Info ===\n";
                
                // Get ground below
                MapleVector2 playerBottom = new MapleVector2(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y - 0.3f);
                MapleVector2 maplePos = MaplePhysicsConverter.UnityToMaple(playerBottom);
                
                if (footholdService != null)
                {
                    float groundY = footholdService.GetGroundBelow(maplePos.X, maplePos.Y);
                    
                    if (groundY != float.MaxValue)
                    {
                        float unityGroundY = MaplePhysicsConverter.MapleToUnityY(groundY + 1);
                        debugText += $"Ground Below: Y={unityGroundY:F3}\n";
                        debugText += $"Distance to Ground: {playerBottom.Y - unityGroundY:F3}\n";
                        
                        // Current foothold
                        var foothold = footholdService.GetFootholdAt(maplePos.X, maplePos.Y);
                        if (foothold != null)
                        {
                            debugText += $"Current Foothold: ID={foothold.Id}\n";
                            debugText += $"Slope: {foothold.GetSlope():F3}\n";
                        }
                    }
                    else
                    {
                        debugText += "No ground below\n";
                    }
                }
                
                // Movement info
                debugText += $"\n=== Movement ===\n";
                debugText += $"Walk Speed: {gameLogicPlayer.GetWalkSpeed():F3}\n";
                debugText += $"Jump Count: {gameLogicPlayer.GetJumpCount()}\n";
                
                // Active modifiers
                var modifiers = gameLogicPlayer.GetActiveModifiers();
                if (modifiers.Count > 0)
                {
                    debugText += $"\n=== Active Modifiers ===\n";
                    foreach (var mod in modifiers)
                    {
                        debugText += $"- {mod.Id}: Speed x{mod.SpeedMultiplier:F2}\n";
                    }
                }
            }
            else
            {
                debugText = "Waiting for player initialization...";
            }
        }
        
        private void DrawFootholds()
        {
            if (!showFootholds || cachedFootholds == null) return;
            
            Gizmos.color = footholdColor;
            
            foreach (var foothold in cachedFootholds)
            {
                // Convert to Unity coordinates
                Vector3 start = new Vector3(foothold.X1 / 100f, foothold.Y1 / 100f, 0);
                Vector3 end = new Vector3(foothold.X2 / 100f, foothold.Y2 / 100f, 0);
                
                // Draw foothold line
                Gizmos.DrawLine(start, end);
                
                // Draw thicker line for visibility
                DrawThickLine(start, end, 0.05f);
                
                // Draw foothold ID at center
                Vector3 center = (start + end) / 2f;
                DrawString(center, foothold.Id.ToString(), footholdColor);
            }
        }
        
        private void DrawPlayerCollision()
        {
            if (!showPlayerCollision || gameLogicPlayer == null) return;
            
            Vector3 playerPos = new Vector3(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y, 0);
            
            // Draw player collision box
            Gizmos.color = playerBoxColor;
            Vector3 boxSize = new Vector3(0.3f, 0.6f, 0.1f);
            Gizmos.DrawWireCube(playerPos, boxSize);
            
            // Draw player center
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(playerPos, 0.05f);
            
            // Draw player bottom (collision point)
            Vector3 bottomPos = playerPos - new Vector3(0, 0.3f, 0);
            Gizmos.color = collisionPointColor;
            Gizmos.DrawWireSphere(bottomPos, 0.05f);
            
            // State indicator
            Color stateColor = GetStateColor(gameLogicPlayer.State);
            Gizmos.color = stateColor;
            Gizmos.DrawWireSphere(playerPos + Vector3.up * 0.4f, 0.1f);
        }
        
        private void DrawGroundDetection()
        {
            if (!showGroundRays || gameLogicPlayer == null || footholdService == null) return;
            
            Vector3 playerPos = new Vector3(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y, 0);
            Vector3 bottomPos = playerPos - new Vector3(0, 0.3f, 0);
            
            // Convert to Maple coordinates
            MapleVector2 maplePos = MaplePhysicsConverter.UnityToMaple(new MapleVector2(bottomPos.x, bottomPos.y));
            
            // Get ground below
            float groundY = footholdService.GetGroundBelow(maplePos.X, maplePos.Y);
            
            if (groundY != float.MaxValue)
            {
                float unityGroundY = MaplePhysicsConverter.MapleToUnityY(groundY + 1);
                Vector3 groundPos = new Vector3(playerPos.x, unityGroundY, 0);
                
                // Draw ray from player to ground
                Gizmos.color = groundRayColor;
                Gizmos.DrawLine(bottomPos, groundPos);
                
                // Draw ground point
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundPos, 0.08f);
                
                // Draw distance text
                float distance = bottomPos.y - groundPos.y;
                Vector3 midPoint = (bottomPos + groundPos) / 2f;
                DrawString(midPoint, $"{distance:F2}", groundRayColor);
            }
            else
            {
                // No ground - draw long ray
                Gizmos.color = new Color(groundRayColor.r, groundRayColor.g, groundRayColor.b, 0.5f);
                Gizmos.DrawLine(bottomPos, bottomPos - Vector3.up * 5f);
            }
        }
        
        private void DrawVelocity()
        {
            if (!showVelocityVector || gameLogicPlayer == null) return;
            
            Vector3 playerPos = new Vector3(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y, 0);
            Vector3 velocity = new Vector3(gameLogicPlayer.Velocity.X, gameLogicPlayer.Velocity.Y, 0);
            
            if (velocity.magnitude > 0.01f)
            {
                // Scale velocity for visualization
                Vector3 velocityEnd = playerPos + velocity * 0.2f;
                
                Gizmos.color = velocityColor;
                Gizmos.DrawLine(playerPos, velocityEnd);
                
                // Draw arrowhead
                Vector3 dir = (velocityEnd - playerPos).normalized;
                Vector3 right = Quaternion.Euler(0, 0, -30) * dir * 0.1f;
                Vector3 left = Quaternion.Euler(0, 0, 30) * dir * 0.1f;
                Gizmos.DrawLine(velocityEnd, velocityEnd - right);
                Gizmos.DrawLine(velocityEnd, velocityEnd - left);
                
                // Draw velocity magnitude
                DrawString(velocityEnd + Vector3.up * 0.1f, $"{velocity.magnitude:F2}", velocityColor);
            }
        }
        
        private void DrawThickLine(Vector3 start, Vector3 end, float thickness)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0) * thickness * 0.5f;
            
            // Draw multiple lines to create thickness
            for (int i = -2; i <= 2; i++)
            {
                float t = i / 2f;
                Gizmos.DrawLine(start + perp * t, end + perp * t);
            }
        }
        
        private void DrawString(Vector3 worldPos, string text, Color color)
        {
            // Note: This is a placeholder - in actual implementation you'd use
            // Handles.Label in OnDrawGizmos or create TextMesh objects
            // For now, we'll just draw a small colored sphere where text would be
            Gizmos.color = color;
            Gizmos.DrawWireSphere(worldPos, 0.02f);
        }
        
        private Color GetStateColor(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Standing: return Color.green;
                case PlayerState.Walking: return Color.yellow;
                case PlayerState.Jumping: return Color.cyan;
                case PlayerState.DoubleJumping: return Color.blue;
                case PlayerState.FlashJumping: return Color.magenta;
                case PlayerState.Falling: return Color.red;
                case PlayerState.Climbing: return new Color(0, 1, 0.5f);
                case PlayerState.Crouching: return Color.gray;
                case PlayerState.Swimming: return new Color(0, 0.5f, 1);
                default: return Color.white;
            }
        }
        
        public void RefreshFootholds()
        {
            if (footholdService == null) return;
            
            // Get all footholds in view
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
                Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
                
                // Convert to Maple coordinates and expand area
                float minX = (bottomLeft.x - 5) * 100;
                float maxX = (topRight.x + 5) * 100;
                float minY = (bottomLeft.y - 5) * 100;
                float maxY = (topRight.y + 5) * 100;
                
                cachedFootholds = footholdService.GetFootholdsInArea(minX, minY, maxX, maxY).ToList();
            }
            else
            {
                // Get all footholds if no camera
                cachedFootholds = footholdService.GetFootholdsInArea(
                    float.MinValue, float.MinValue, 
                    float.MaxValue, float.MaxValue
                ).Take(100).ToList(); // Limit to 100 for performance
            }
        }
        
        // Public methods for runtime control
        public void ToggleDebugView()
        {
            enableDebugView = !enableDebugView;
        }
        
        public void SetShowFootholds(bool show)
        {
            showFootholds = show;
        }
        
        public void SetShowPlayerCollision(bool show)
        {
            showPlayerCollision = show;
        }
        
        public void SetShowGroundRays(bool show)
        {
            showGroundRays = show;
        }
        
        public void ForcePlayerGroundCheck()
        {
            if (gameLogicPlayer != null)
            {
                // Force a physics update to recheck grounding
                gameLogicPlayer.UpdatePhysics(Time.deltaTime, null);
                UnityEngine.Debug.Log($"Forced ground check - Grounded: {gameLogicPlayer.IsGrounded}");
            }
        }
    }
}