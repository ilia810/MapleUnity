using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;

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
        private PhysicsDebugStats lastStats;
        private float updateInterval = 0.5f; // Update display every 0.5 seconds
        private float lastUpdateTime;
        
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
    }
}