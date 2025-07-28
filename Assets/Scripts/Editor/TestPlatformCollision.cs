using UnityEngine;
using UnityEditor;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;
using Vector2 = MapleClient.GameLogic.Vector2; // Use GameLogic Vector2 for Player

public class TestPlatformCollision : EditorWindow
{
    private Player player;
    private MapData mapData;
    private float simulationTime = 0f;
    private const float DELTA_TIME = 0.016f; // 60 FPS
    
    [MenuItem("MapleUnity/Test Platform Collision")]
    public static void ShowWindow()
    {
        GetWindow<TestPlatformCollision>("Platform Collision Test");
    }
    
    void OnEnable()
    {
        // Initialize test player
        player = new Player();
        player.Position = new Vector2(5f, 5f); // 500px, 500px
        
        // Create test map with platforms
        mapData = new MapData
        {
            MapId = 100000000,
            Name = "Test Map",
            Platforms = new List<Platform>()
        };
        
        // Add a simple platform at Y=2 (200px)
        mapData.Platforms.Add(new Platform
        {
            Id = 1,
            X1 = 0f,
            Y1 = 200f,
            X2 = 1000f,
            Y2 = 200f,
            Type = PlatformType.Normal
        });
        
        // Add a sloped platform
        mapData.Platforms.Add(new Platform
        {
            Id = 2,
            X1 = 1000f,
            Y1 = 200f,
            X2 = 1500f,
            Y2 = 300f,
            Type = PlatformType.Normal
        });
        
        // Add a one-way platform
        mapData.Platforms.Add(new Platform
        {
            Id = 3,
            X1 = 200f,
            Y1 = 400f,
            X2 = 400f,
            Y2 = 400f,
            Type = PlatformType.OneWay
        });
        
        Debug.Log($"TestPlatformCollision initialized with {mapData.Platforms.Count} platforms");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Platform Collision Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Display player state
        EditorGUILayout.LabelField($"Position: ({player.Position.X:F2}, {player.Position.Y:F2})");
        EditorGUILayout.LabelField($"Velocity: ({player.Velocity.X:F2}, {player.Velocity.Y:F2})");
        EditorGUILayout.LabelField($"IsGrounded: {player.IsGrounded}");
        EditorGUILayout.LabelField($"State: {player.State}");
        EditorGUILayout.LabelField($"Simulation Time: {simulationTime:F2}s");
        
        EditorGUILayout.Space();
        
        // Control buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset"))
        {
            player.Position = new Vector2(5f, 5f);
            player.Velocity = Vector2.Zero;
            player.IsGrounded = false;
            simulationTime = 0f;
            Debug.Log("Player reset to starting position");
        }
        
        if (GUILayout.Button("Step Physics"))
        {
            StepPhysics();
        }
        
        if (GUILayout.Button("Run 1 Second"))
        {
            for (int i = 0; i < 60; i++) // 60 frames = 1 second
            {
                StepPhysics();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Movement controls
        EditorGUILayout.LabelField("Movement Controls:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("← Move Left"))
        {
            player.MoveLeft(true);
            StepPhysics();
            player.MoveLeft(false);
        }
        if (GUILayout.Button("→ Move Right"))
        {
            player.MoveRight(true);
            StepPhysics();
            player.MoveRight(false);
        }
        if (GUILayout.Button("↑ Jump"))
        {
            player.Jump();
            Debug.Log($"Jump initiated. Velocity: {player.Velocity.Y}");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Platform info
        EditorGUILayout.LabelField("Platforms in Map:", EditorStyles.boldLabel);
        foreach (var platform in mapData.Platforms)
        {
            float platformYAtPlayer = platform.GetYAtX(player.Position.X * 100f);
            EditorGUILayout.LabelField($"Platform {platform.Id}: ({platform.X1},{platform.Y1}) to ({platform.X2},{platform.Y2}) - Type: {platform.Type}");
            if (!float.IsNaN(platformYAtPlayer))
            {
                EditorGUILayout.LabelField($"  Y at player X: {platformYAtPlayer / 100f:F2} units");
            }
        }
        
        // Test GetPlatformBelow
        if (GUILayout.Button("Test GetPlatformBelow"))
        {
            TestGetPlatformBelow();
        }
    }
    
    private void StepPhysics()
    {
        player.UpdatePhysics(DELTA_TIME, mapData);
        simulationTime += DELTA_TIME;
        
        // Log significant events
        if (player.IsGrounded && player.Velocity.Y < -0.1f)
        {
            Debug.Log($"Player landed at position ({player.Position.X:F2}, {player.Position.Y:F2})");
        }
    }
    
    private void TestGetPlatformBelow()
    {
        // We'll use reflection to test the private method
        var methodInfo = player.GetType().GetMethod("GetPlatformBelow", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (methodInfo != null)
        {
            var result = methodInfo.Invoke(player, new object[] { player.Position, mapData }) as Platform;
            if (result != null)
            {
                Debug.Log($"GetPlatformBelow returned Platform {result.Id} at Y range {result.Y1}-{result.Y2}");
            }
            else
            {
                Debug.Log("GetPlatformBelow returned null");
            }
        }
        else
        {
            Debug.LogError("Could not find GetPlatformBelow method via reflection");
        }
    }
}