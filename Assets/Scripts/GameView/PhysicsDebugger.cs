using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Debug overlay for monitoring physics system performance and frame timing.
    /// Verifies that physics is running at exactly 60 FPS.
    /// </summary>
    public class PhysicsDebugger : MonoBehaviour
    {
        [Header("Debug Display Settings")]
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color textColor = Color.green;
        [SerializeField] private Vector2 displayPosition = new Vector2(10, 10);
        
        private GameWorld gameWorld;
        private Player player;
        private PhysicsDebugStats lastStats;
        private float updateInterval = 0.5f; // Update display every 0.5 seconds
        private float lastUpdateTime;
        
        // Movement state tracking
        private PlayerState lastPlayerState;
        private MapleClient.GameLogic.Vector2 lastPlayerVelocity;
        private bool lastGroundedState;
        private List<IMovementModifier> lastModifiers = new List<IMovementModifier>();
        
        // Performance tracking
        private float[] frameTimes = new float[60];
        private int frameTimeIndex = 0;
        private float lastFrameTime;
        
        // GUI style
        private GUIStyle debugStyle;
        private Rect debugRect;
        
        private void Start()
        {
            // Find GameWorld through GameManager
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                // Use reflection to get GameWorld (since it's private)
                var fieldInfo = typeof(GameManager).GetField("gameWorld", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    gameWorld = fieldInfo.GetValue(gameManager) as GameWorld;
                    if (gameWorld != null)
                    {
                        player = gameWorld.Player;
                    }
                }
            }
            
            // Initialize GUI style
            debugStyle = new GUIStyle();
            debugStyle.fontSize = fontSize;
            debugStyle.normal.textColor = textColor;
            debugStyle.alignment = TextAnchor.UpperLeft;
            
            debugRect = new Rect(displayPosition.x, displayPosition.y, 400, 300);
            
            lastFrameTime = Time.realtimeSinceStartup;
        }
        
        private void Update()
        {
            // Toggle debug overlay
            if (Input.GetKeyDown(toggleKey))
            {
                showDebugOverlay = !showDebugOverlay;
            }
            
            // Track frame times
            float currentTime = Time.realtimeSinceStartup;
            float frameTime = currentTime - lastFrameTime;
            lastFrameTime = currentTime;
            
            frameTimes[frameTimeIndex] = frameTime;
            frameTimeIndex = (frameTimeIndex + 1) % frameTimes.Length;
            
            // Update stats periodically
            if (currentTime - lastUpdateTime >= updateInterval)
            {
                if (gameWorld != null)
                {
                    lastStats = gameWorld.GetPhysicsDebugStats();
                }
                lastUpdateTime = currentTime;
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugOverlay || gameWorld == null)
                return;
            
            // Calculate average frame time
            float avgFrameTime = 0f;
            float minFrameTime = float.MaxValue;
            float maxFrameTime = 0f;
            
            foreach (float time in frameTimes)
            {
                if (time > 0)
                {
                    avgFrameTime += time;
                    minFrameTime = Mathf.Min(minFrameTime, time);
                    maxFrameTime = Mathf.Max(maxFrameTime, time);
                }
            }
            avgFrameTime /= frameTimes.Length;
            
            // Build debug text
            string debugText = "=== MapleUnity Physics Debug ===\n\n";
            
            // Frame timing
            debugText += "Frame Timing:\n";
            debugText += $"  Current FPS: {(1f / avgFrameTime):F1}\n";
            debugText += $"  Frame Time: {avgFrameTime * 1000:F2}ms (avg) | {minFrameTime * 1000:F2}ms (min) | {maxFrameTime * 1000:F2}ms (max)\n";
            debugText += $"  Unity Fixed Timestep: {Time.fixedDeltaTime * 1000:F2}ms ({1f / Time.fixedDeltaTime:F1} FPS)\n";
            debugText += $"  Time Scale: {Time.timeScale:F2}\n\n";
            
            // Physics stats
            debugText += "Physics System:\n";
            debugText += $"  Target FPS: {PhysicsUpdateManager.TARGET_FPS}\n";
            debugText += $"  Fixed Timestep: {PhysicsUpdateManager.FIXED_TIMESTEP * 1000:F2}ms\n";
            debugText += $"  Total Frames: {lastStats.TotalFrames:N0}\n";
            debugText += $"  Total Physics Steps: {lastStats.TotalPhysicsSteps:N0}\n";
            debugText += $"  Steps Per Second: {lastStats.StepsPerSecond:F1}\n";
            debugText += $"  Accumulator: {lastStats.Accumulator * 1000:F2}ms\n\n";
            
            // Object counts
            debugText += "Physics Objects:\n";
            debugText += $"  Active Objects: {lastStats.ActiveObjectCount}\n";
            debugText += $"  Total Objects: {lastStats.TotalObjectCount}\n\n";
            
            // Performance warnings
            if (lastStats.StepsPerSecond > 0)
            {
                float physicsAccuracy = lastStats.StepsPerSecond / PhysicsUpdateManager.TARGET_FPS;
                if (physicsAccuracy < 0.95f || physicsAccuracy > 1.05f)
                {
                    debugText += "WARNING: Physics not running at target 60 FPS!\n";
                    debugText += $"  Accuracy: {physicsAccuracy * 100:F1}%\n";
                }
                else
                {
                    debugText += "Physics running at target 60 FPS ✓\n";
                }
            }
            
            // Interpolation factor
            float interpolation = gameWorld.GetPhysicsInterpolationFactor();
            debugText += $"\nInterpolation Factor: {interpolation:F3}";
            
            // Player movement state
            if (player != null)
            {
                debugText += "\n\n=== Player Movement ===\n";
                debugText += $"  State: {player.State}";
                if (player.State != lastPlayerState)
                {
                    debugText += " [CHANGED]";
                    lastPlayerState = player.State;
                }
                debugText += "\n";
                
                var velocity = player.Velocity;
                debugText += $"  Velocity: ({velocity.X:F2}, {velocity.Y:F2})";
                float speed = Mathf.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
                debugText += $" | Speed: {speed:F2}\n";
                
                debugText += $"  Grounded: {(player.IsGrounded ? "YES" : "NO")}";
                if (player.IsGrounded != lastGroundedState)
                {
                    debugText += " [CHANGED]";
                    lastGroundedState = player.IsGrounded;
                }
                debugText += "\n";
                
                debugText += $"  Position: ({player.Position.X:F2}, {player.Position.Y:F2})\n";
                
                // Movement modifiers
                var modifiers = player.GetActiveModifiers();
                if (modifiers.Count > 0)
                {
                    debugText += "\n  Active Modifiers:\n";
                    foreach (var modifier in modifiers)
                    {
                        debugText += $"    - {modifier.Id}";
                        if (modifier.SpeedMultiplier != 1f)
                            debugText += $" (Speed x{modifier.SpeedMultiplier:F1})";
                        if (modifier.JumpMultiplier != 1f)
                            debugText += $" (Jump x{modifier.JumpMultiplier:F1})";
                        if (modifier.FrictionMultiplier != 1f)
                            debugText += $" (Friction x{modifier.FrictionMultiplier:F1})";
                        if (modifier.Duration > 0)
                            debugText += $" [{modifier.Duration:F1}s]";
                        debugText += "\n";
                    }
                }
                
                // Movement capabilities
                debugText += "\n  Capabilities:\n";
                debugText += $"    Walk Speed: {player.GetModifiedWalkSpeed():F2}\n";
                debugText += $"    Jump Power: {player.GetModifiedJumpPower():F2}\n";
                
                // Special movement states
                if (player.State == PlayerState.Climbing)
                {
                    var ladder = player.GetCurrentLadder();
                    if (ladder != null)
                    {
                        debugText += $"\n  Climbing Ladder at X: {ladder.X}\n";
                    }
                }
                
                // Check for special abilities
                var hasDoubleJump = typeof(Player).GetField("hasDoubleJump", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(player) as bool? ?? false;
                var hasFlashJump = typeof(Player).GetField("hasFlashJump", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(player) as bool? ?? false;
                var flashJumpCooldown = typeof(Player).GetField("flashJumpCooldown", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(player) as float? ?? 0f;
                    
                if (hasDoubleJump || hasFlashJump)
                {
                    debugText += "\n  Special Abilities:\n";
                    if (hasDoubleJump)
                        debugText += "    - Double Jump [ENABLED]\n";
                    if (hasFlashJump)
                    {
                        debugText += "    - Flash Jump [ENABLED]";
                        if (flashJumpCooldown > 0)
                            debugText += $" (Cooldown: {flashJumpCooldown:F1}s)";
                        debugText += "\n";
                    }
                }
            }
            
            // Platform detection rays visualization
            if (showDebugOverlay && player != null)
            {
                DrawMovementDebugVisuals();
            }
            
            // Adjust debug rect size based on content
            debugRect.height = debugText.Split('\n').Length * 20 + 40;
            
            // Draw background
            GUI.Box(debugRect, "");
            
            // Draw text
            GUI.Label(debugRect, debugText, debugStyle);
        }
        
        // Public method to log physics performance
        public void LogPhysicsPerformance()
        {
            if (gameWorld == null) return;
            
            var stats = gameWorld.GetPhysicsDebugStats();
            
            Debug.Log("=== Physics Performance Report ===");
            Debug.Log($"Total Frames: {stats.TotalFrames}");
            Debug.Log($"Total Physics Steps: {stats.TotalPhysicsSteps}");
            Debug.Log($"Average Steps/Second: {stats.StepsPerSecond:F2}");
            Debug.Log($"Target Steps/Second: {PhysicsUpdateManager.TARGET_FPS}");
            Debug.Log($"Accuracy: {(stats.StepsPerSecond / PhysicsUpdateManager.TARGET_FPS) * 100:F1}%");
            Debug.Log($"Active Physics Objects: {stats.ActiveObjectCount}");
            Debug.Log($"Average Frame Time: {stats.AverageFrameTime * 1000:F2}ms");
        }
        
        private void DrawMovementDebugVisuals()
        {
            if (player == null) return;
            
            Vector3 playerPos = new Vector3(player.Position.X, player.Position.Y, 0);
            
            // Draw ground check ray
            Debug.DrawRay(playerPos - Vector3.up * 0.3f, Vector3.down * 0.5f, 
                player.IsGrounded ? Color.green : Color.red);
            
            // Draw movement direction
            if (Mathf.Abs(player.Velocity.X) > 0.01f)
            {
                Vector3 velocityDir = new Vector3(player.Velocity.X, 0, 0).normalized;
                Debug.DrawRay(playerPos, velocityDir * 0.5f, Color.blue);
            }
            
            // Draw jump trajectory prediction (if jumping)
            if (!player.IsGrounded && player.Velocity.Y > 0)
            {
                Vector3 startPos = playerPos;
                Vector3 velocity = new Vector3(player.Velocity.X, player.Velocity.Y, 0);
                float gravity = MaplePhysics.Gravity / 100f;
                float timeStep = 0.1f;
                
                for (int i = 0; i < 10; i++)
                {
                    Vector3 nextPos = startPos + velocity * timeStep;
                    velocity.y -= gravity * timeStep;
                    
                    Debug.DrawLine(startPos, nextPos, new Color(1f, 1f, 0f, 1f - i * 0.1f));
                    startPos = nextPos;
                    
                    if (velocity.y < 0) break;
                }
            }
            
            // Draw platform detection area
            float checkDistance = 1f;
            Vector3 leftCheck = playerPos + Vector3.left * 0.15f;
            Vector3 rightCheck = playerPos + Vector3.right * 0.15f;
            
            Debug.DrawLine(leftCheck, leftCheck + Vector3.down * checkDistance, 
                new Color(0.5f, 0.5f, 1f, 0.5f));
            Debug.DrawLine(rightCheck, rightCheck + Vector3.down * checkDistance, 
                new Color(0.5f, 0.5f, 1f, 0.5f));
            
            // Draw modifier effect areas
            var modifiers = player.GetActiveModifiers();
            foreach (var modifier in modifiers)
            {
                if (modifier.Id == "slippery_surface")
                {
                    // Draw ice effect area
                    DrawCircle(playerPos + Vector3.down * 0.3f, 0.3f, new Color(0.5f, 0.8f, 1f, 0.3f));
                }
                else if (modifier.SpeedMultiplier > 1f)
                {
                    // Draw speed boost effect
                    DrawCircle(playerPos, 0.4f, new Color(0.5f, 1f, 0.5f, 0.3f));
                }
            }
        }
        
        private void DrawCircle(Vector3 center, float radius, Color color)
        {
            int segments = 16;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
                Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;
                
                Debug.DrawLine(point1, point2, color);
            }
        }
    }
}