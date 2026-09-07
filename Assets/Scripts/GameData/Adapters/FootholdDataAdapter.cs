using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;

namespace MapleClient.GameData.Adapters
{
    /// <summary>
    /// Adapter to convert between different foothold data representations
    /// </summary>
    public static class FootholdDataAdapter
    {
        /// <summary>
        /// Converts Platform objects from NxMapLoader to Foothold objects for FootholdService
        /// </summary>
        public static List<Foothold> ConvertPlatformsToFootholds(List<Platform> platforms)
        {
            var footholds = new List<Foothold>();
            
            foreach (var platform in platforms)
            {
                var foothold = new Foothold
                {
                    Id = platform.Id,
                    X1 = platform.X1,
                    Y1 = platform.Y1,
                    X2 = platform.X2,
                    Y2 = platform.Y2,
                    
                    // Platform type mapping
                    IsWall = platform.X1 == platform.X2 || platform.Type == PlatformType.Ladder || platform.Type == PlatformType.Rope,
                    
                    // Environmental properties
                    IsSlippery = platform.IsSlippery,
                    IsConveyor = platform.IsConveyor,
                    ConveyorSpeed = platform.ConveyorSpeed,
                    
                    PreviousId = platform.PreviousId,
                    NextId = platform.NextId,
                    Layer = platform.Layer
                };
                
                footholds.Add(foothold);
            }
            
            // Source insertion order decides equal-height support ties.
            return footholds;
        }
        
        // Removed ConvertSceneFootholdsToGameLogic as GameData should not depend on SceneGeneration
        
        /// <summary>
        /// Converts MapData footholds to Platform objects (for backward compatibility)
        /// </summary>
        public static List<Platform> ConvertFootholdsToPlatforms(List<GameLogic.Foothold> footholds)
        {
            var platforms = new List<Platform>();
            
            foreach (var foothold in footholds)
            {
                var platform = new Platform
                {
                    Id = foothold.Id,
                    PreviousId = foothold.PreviousId,
                    NextId = foothold.NextId,
                    Layer = foothold.Layer,
                    HasSourceTopology = true,
                    X1 = foothold.X1,
                    Y1 = foothold.Y1,
                    X2 = foothold.X2,
                    Y2 = foothold.Y2,
                    Type = PlatformType.Normal,
                    IsSlippery = foothold.IsSlippery,
                    IsConveyor = foothold.IsConveyor,
                    ConveyorSpeed = foothold.ConveyorSpeed
                };
                
                platforms.Add(platform);
            }
            
            return platforms;
        }
        
        /// <summary>
        /// Assigns layer information to footholds based on vertical grouping
        /// </summary>
        private static void AssignLayers(List<GameLogic.Foothold> footholds)
        {
            if (footholds.Count == 0) return;
            
            // Group footholds by approximate Y position
            var layerGroups = new Dictionary<int, List<GameLogic.Foothold>>();
            int currentLayer = 0;
            
            // Sort by average Y position
            var sortedFootholds = footholds.OrderBy(f => (f.Y1 + f.Y2) / 2f).ToList();
            
            float lastY = float.MinValue;
            const float layerThreshold = 50f; // Pixels between layers
            
            foreach (var foothold in sortedFootholds)
            {
                float avgY = (foothold.Y1 + foothold.Y2) / 2f;
                
                // Check if this foothold is significantly below the last one
                if (avgY - lastY > layerThreshold)
                {
                    currentLayer++;
                }
                
                foothold.Layer = currentLayer;
                lastY = avgY;
            }
            
            // Assigned layers to footholds
        }
        
        /// <summary>
        /// Builds foothold connectivity information based on horizontal adjacency
        /// </summary>
        public static void BuildFootholdConnectivity(List<GameLogic.Foothold> footholds)
        {
            // Group by layer
            var layerGroups = footholds.GroupBy(f => f.Layer).ToDictionary(g => g.Key, g => g.ToList());
            
            foreach (var layerGroup in layerGroups)
            {
                var layerFootholds = layerGroup.Value.OrderBy(f => f.X1).ToList();
                
                for (int i = 0; i < layerFootholds.Count; i++)
                {
                    var current = layerFootholds[i];
                    
                    // Find previous foothold (to the left)
                    if (i > 0)
                    {
                        var prev = layerFootholds[i - 1];
                        // Check if they're adjacent (within reasonable distance)
                        if (System.Math.Abs(current.X1 - prev.X2) < 10f)
                        {
                            current.PreviousId = prev.Id;
                            prev.NextId = current.Id;
                        }
                    }
                    
                    // Next foothold connection is handled when processing the next foothold
                }
            }
            
            // Built connectivity for footholds
        }
    }
}