using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameData.Adapters;
using System.Linq;

namespace MapleClient.Editor
{
    public class TestFootholdIntegration : EditorWindow
    {
        private int testMapId = 100000000; // Henesys
        private FootholdService footholdService;
        private NxMapLoader mapLoader;
        
        [MenuItem("MapleUnity/Test/Foothold Integration")]
        public static void ShowWindow()
        {
            GetWindow<TestFootholdIntegration>("Test Foothold Integration");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Foothold Integration Test", EditorStyles.boldLabel);
            
            testMapId = EditorGUILayout.IntField("Map ID:", testMapId);
            
            if (GUILayout.Button("Test Load Map with Footholds"))
            {
                TestLoadMapWithFootholds();
            }
            
            if (footholdService != null && GUILayout.Button("Test Foothold Queries"))
            {
                TestFootholdQueries();
            }
            
            if (GUILayout.Button("Test Platform to Foothold Conversion"))
            {
                TestPlatformConversion();
            }
            
            if (GUILayout.Button("Test Scene Foothold Conversion"))
            {
                TestSceneFootholdConversion();
            }
        }
        
        private void TestLoadMapWithFootholds()
        {
            Debug.Log($"=== Testing Map Load with Foothold Integration (Map ID: {testMapId}) ===");
            
            // Create services
            footholdService = new FootholdService();
            mapLoader = new NxMapLoader("", footholdService);
            
            // Load map
            var mapData = mapLoader.GetMap(testMapId);
            
            if (mapData != null)
            {
                Debug.Log($"Map loaded: {mapData.Name} (ID: {mapData.MapId})");
                Debug.Log($"Platforms loaded: {mapData.Platforms.Count}");
                
                // Check foothold service
                var footholds = footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToList();
                Debug.Log($"Footholds in service: {footholds.Count}");
                
                if (footholds.Count > 0)
                {
                    Debug.Log($"First foothold: ID={footholds[0].Id}, X1={footholds[0].X1}, Y1={footholds[0].Y1}, X2={footholds[0].X2}, Y2={footholds[0].Y2}");
                    Debug.Log($"Last foothold: ID={footholds[^1].Id}, X1={footholds[^1].X1}, Y1={footholds[^1].Y1}, X2={footholds[^1].X2}, Y2={footholds[^1].Y2}");
                }
                
                // Test connectivity
                int connectedCount = footholds.Count(f => f.NextId != 0 || f.PreviousId != 0);
                Debug.Log($"Footholds with connections: {connectedCount}");
            }
            else
            {
                Debug.LogError("Failed to load map data");
            }
        }
        
        private void TestFootholdQueries()
        {
            Debug.Log("=== Testing Foothold Queries ===");
            
            // Test GetGroundBelow at various positions
            float[] testX = { 0, 500, 1000, -500 };
            float[] testY = { 0, 500, 1000, -500 };
            
            foreach (var x in testX)
            {
                foreach (var y in testY)
                {
                    float ground = footholdService.GetGroundBelow(x, y);
                    if (ground != float.MaxValue)
                    {
                        Debug.Log($"Ground below ({x}, {y}): {ground}");
                    }
                }
            }
            
            // Test IsOnGround
            var foothold = footholdService.GetFootholdAt(500, 500);
            if (foothold != null)
            {
                float groundY = foothold.GetYAtX(500);
                bool onGround = footholdService.IsOnGround(500, groundY, 5f);
                Debug.Log($"IsOnGround at (500, {groundY}): {onGround}");
            }
            
            // Test FindNearestFoothold
            var nearest = footholdService.FindNearestFoothold(0, 0);
            if (nearest != null)
            {
                Debug.Log($"Nearest foothold to (0, 0): ID={nearest.Id} at ({nearest.X1},{nearest.Y1})-({nearest.X2},{nearest.Y2})");
            }
        }
        
        private void TestPlatformConversion()
        {
            Debug.Log("=== Testing Platform to Foothold Conversion ===");
            
            // Create test platforms
            var platforms = new System.Collections.Generic.List<Platform>
            {
                new Platform { Id = 1, X1 = 0, Y1 = 100, X2 = 200, Y2 = 100, Type = PlatformType.Normal },
                new Platform { Id = 2, X1 = 200, Y1 = 100, X2 = 400, Y2 = 150, Type = PlatformType.Normal },
                new Platform { Id = 3, X1 = 100, Y1 = 200, X2 = 100, Y2 = 400, Type = PlatformType.Ladder },
                new Platform { Id = 4, X1 = 0, Y1 = 500, X2 = 300, Y2 = 500, Type = PlatformType.OneWay, IsSlippery = true }
            };
            
            // Convert
            var footholds = FootholdDataAdapter.ConvertPlatformsToFootholds(platforms);
            FootholdDataAdapter.BuildFootholdConnectivity(footholds);
            
            Debug.Log($"Converted {platforms.Count} platforms to {footholds.Count} footholds");
            
            foreach (var fh in footholds)
            {
                Debug.Log($"Foothold {fh.Id}: ({fh.X1},{fh.Y1})-({fh.X2},{fh.Y2}), " +
                         $"IsWall={fh.IsWall}, IsSlippery={fh.IsSlippery}, " +
                         $"Prev={fh.PreviousId}, Next={fh.NextId}");
            }
        }
        
        private void TestSceneFootholdConversion()
        {
            Debug.Log("=== Testing Scene Foothold Conversion ===");
            
            // Note: ConvertSceneFootholdsToGameLogic was removed to maintain layer separation
            // SceneGeneration and GameLogic use separate foothold implementations
            Debug.Log("Scene foothold conversion test skipped - conversion method removed for layer separation");
        }
    }
}