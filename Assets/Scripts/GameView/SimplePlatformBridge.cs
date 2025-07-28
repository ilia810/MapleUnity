using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    /// <summary>
    /// Simple bridge to provide platform data to GameLogic without cross-assembly dependencies
    /// </summary>
    public class SimplePlatformBridge : MonoBehaviour
    {
        private List<Platform> platforms = new List<Platform>();
        
        /// <summary>
        /// Extract platforms from the Unity scene after map generation
        /// </summary>
        public void ExtractPlatformsFromScene(MapData mapData)
        {
            // Check if MapData already has platforms loaded
            if (mapData != null && mapData.Platforms != null && mapData.Platforms.Count > 0)
            {
                Debug.Log($"MapData already has {mapData.Platforms.Count} platforms loaded");
                return;
            }
            
            platforms.Clear();
            
            // Find the map root
            GameObject mapRoot = null;
            var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                if (root.name.StartsWith("Map_"))
                {
                    mapRoot = root;
                    break;
                }
            }
            
            if (mapRoot == null)
            {
                Debug.LogWarning("No map root found");
                // Don't create test platforms - let the game use actual footholds only
                // CreateTestPlatforms(mapData);
                return;
            }
            
            // Extract foothold data from map
            var footholdManager = mapRoot.GetComponent("FootholdManager");
            if (footholdManager != null)
            {
                // Use reflection to get footholds
                var footholds = footholdManager.GetType().GetMethod("GetAllFootholds")?.Invoke(footholdManager, null) as System.Collections.IList;
                if (footholds != null && footholds.Count > 0)
                {
                    foreach (var foothold in footholds)
                    {
                        var type = foothold.GetType();
                        var id = (int)type.GetProperty("Id").GetValue(foothold);
                        var x1 = (int)type.GetProperty("X1").GetValue(foothold);
                        var y1 = (int)type.GetProperty("Y1").GetValue(foothold);
                        var x2 = (int)type.GetProperty("X2").GetValue(foothold);
                        var y2 = (int)type.GetProperty("Y2").GetValue(foothold);
                        
                        platforms.Add(new Platform
                        {
                            Id = id,
                            X1 = x1,
                            Y1 = y1,
                            X2 = x2,
                            Y2 = y2,
                            Type = PlatformType.Normal
                        });
                    }
                    
                    Debug.Log($"Extracted {platforms.Count} platforms from FootholdManager");
                }
            }
            
            // If no platforms found, try extracting from LineRenderers
            if (platforms.Count == 0)
            {
                ExtractPlatformsFromLineRenderers(mapRoot);
            }
            
            // Update map data
            if (mapData != null && platforms.Count > 0)
            {
                mapData.Platforms = platforms;
                Debug.Log($"Set {platforms.Count} platforms in MapData");
            }
            else if (mapData != null)
            {
                Debug.LogWarning("No platforms found in map data");
                // Don't create test platforms - let the game use actual footholds only
                // CreateTestPlatforms(mapData);
            }
        }
        
        private void ExtractPlatformsFromLineRenderers(GameObject mapRoot)
        {
            // Find all LineRenderers that represent footholds
            var lineRenderers = mapRoot.GetComponentsInChildren<LineRenderer>();
            int platformId = 1;
            
            foreach (var line in lineRenderers)
            {
                if (line.gameObject.name.Contains("Foothold") && line.positionCount >= 2)
                {
                    Vector3 start = line.GetPosition(0);
                    Vector3 end = line.GetPosition(1);
                    
                    platforms.Add(new Platform
                    {
                        Id = platformId++,
                        X1 = (int)(start.x * 100),
                        Y1 = (int)(start.y * 100), // Keep Unity coordinates as-is
                        X2 = (int)(end.x * 100),
                        Y2 = (int)(end.y * 100), // Keep Unity coordinates as-is
                        Type = PlatformType.Normal
                    });
                }
            }
            
            if (platforms.Count > 0)
            {
                Debug.Log($"Extracted {platforms.Count} platforms from LineRenderers");
            }
        }
        
        /// <summary>
        /// Create test platforms for basic functionality
        /// </summary>
        public void CreateTestPlatforms(MapData mapData)
        {
            platforms.Clear();
            
            // Create a main ground platform
            platforms.Add(new Platform
            {
                Id = 1,
                X1 = -500,
                Y1 = 0,
                X2 = 500,
                Y2 = 0,
                Type = PlatformType.Normal
            });
            
            // Create some elevated platforms
            platforms.Add(new Platform
            {
                Id = 2,
                X1 = -300,
                Y1 = 200,
                X2 = -100,
                Y2 = 200,
                Type = PlatformType.Normal
            });
            
            platforms.Add(new Platform
            {
                Id = 3,
                X1 = 100,
                Y1 = 200,
                X2 = 300,
                Y2 = 200,
                Type = PlatformType.Normal
            });
            
            // Create a higher platform
            platforms.Add(new Platform
            {
                Id = 4,
                X1 = -100,
                Y1 = 400,
                X2 = 100,
                Y2 = 400,
                Type = PlatformType.Normal
            });
            
            if (mapData != null)
            {
                mapData.Platforms = platforms;
                Debug.Log($"Created {platforms.Count} test platforms");
            }
        }
    }
}