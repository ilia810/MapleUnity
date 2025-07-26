using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates platform colliders from foothold data
    /// </summary>
    public class FootholdGenerator
    {
        private const string FOOTHOLD_LAYER = "Platform";
        private const float PLATFORM_THICKNESS = 0.1f;
        
        public GameObject GenerateFootholds(List<Foothold> footholds, Transform parent)
        {
            GameObject footholdContainer = new GameObject("Footholds");
            footholdContainer.transform.parent = parent;
            
            // Group connected footholds
            var groups = GroupConnectedFootholds(footholds);
            
            int groupIndex = 0;
            foreach (var group in groups)
            {
                GenerateFootholdGroup(group, footholdContainer.transform, groupIndex++);
            }
            
            return footholdContainer;
        }
        
        private List<List<Foothold>> GroupConnectedFootholds(List<Foothold> footholds)
        {
            var groups = new List<List<Foothold>>();
            var processed = new HashSet<int>();
            
            foreach (var foothold in footholds)
            {
                if (processed.Contains(foothold.Id))
                    continue;
                    
                var group = new List<Foothold>();
                CollectConnectedFootholds(foothold, footholds, group, processed);
                
                if (group.Count > 0)
                    groups.Add(group);
            }
            
            return groups;
        }
        
        private void CollectConnectedFootholds(Foothold start, List<Foothold> allFootholds, 
            List<Foothold> group, HashSet<int> processed)
        {
            if (start == null || processed.Contains(start.Id))
                return;
                
            processed.Add(start.Id);
            group.Add(start);
            
            // Find connected footholds
            foreach (var fh in allFootholds)
            {
                if (fh.Id == start.Next || fh.Id == start.Prev)
                {
                    CollectConnectedFootholds(fh, allFootholds, group, processed);
                }
            }
        }
        
        private void GenerateFootholdGroup(List<Foothold> group, Transform parent, int groupIndex)
        {
            GameObject groupObj = new GameObject($"FootholdGroup_{groupIndex}");
            groupObj.transform.parent = parent;
            int platformLayer = LayerMask.NameToLayer(FOOTHOLD_LAYER);
            if (platformLayer == -1) platformLayer = 0; // Use default layer if Platform doesn't exist
            groupObj.layer = platformLayer;
            
            // Create individual foothold segments
            foreach (var foothold in group)
            {
                CreateFootholdSegment(foothold, groupObj.transform);
            }
            
            // Optional: Create edge colliders for connected platforms
            // This provides smoother collision for connected platforms
            CreateEdgeColliderForGroup(group, groupObj);
        }
        
        private void CreateFootholdSegment(Foothold foothold, Transform parent)
        {
            GameObject segment = new GameObject($"Foothold_{foothold.Id}");
            segment.transform.parent = parent;
            int platformLayer = LayerMask.NameToLayer(FOOTHOLD_LAYER);
            if (platformLayer == -1) platformLayer = 0; // Use default layer if Platform doesn't exist
            segment.layer = platformLayer;
            
            // Convert positions
            Vector3 start = CoordinateConverter.ToUnityPosition(foothold.X1, foothold.Y1);
            Vector3 end = CoordinateConverter.ToUnityPosition(foothold.X2, foothold.Y2);
            
            // Position at midpoint
            Vector3 center = (start + end) * 0.5f;
            segment.transform.position = center;
            
            // Create box collider
            BoxCollider2D collider = segment.AddComponent<BoxCollider2D>();
            
            // Calculate size and rotation
            Vector3 diff = end - start;
            float length = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            
            collider.size = new Vector2(length, PLATFORM_THICKNESS);
            segment.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Add platform effector for one-way platforms (optional)
            if (ShouldBeOneWayPlatform(foothold))
            {
                PlatformEffector2D effector = segment.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
                collider.usedByEffector = true;
            }
            
            // Add foothold data component
            FootholdData data = segment.AddComponent<FootholdData>();
            data.footholdId = foothold.Id;
            data.nextId = foothold.Next;
            data.prevId = foothold.Prev;
        }
        
        private void CreateEdgeColliderForGroup(List<Foothold> group, GameObject groupObj)
        {
            if (group.Count < 2) return;
            
            // Sort footholds to form a continuous line
            var sorted = SortFootholdsInOrder(group);
            if (sorted.Count < 2) return;
            
            GameObject edgeObj = new GameObject("EdgeCollider");
            edgeObj.transform.parent = groupObj.transform;
            edgeObj.layer = groupObj.layer;
            
            EdgeCollider2D edge = edgeObj.AddComponent<EdgeCollider2D>();
            
            // Create points for edge collider
            List<Vector2> points = new List<Vector2>();
            
            // Add first point
            var first = sorted[0];
            points.Add(CoordinateConverter.ToUnityPosition(first.X1, first.Y1));
            
            // Add end points of each foothold
            foreach (var fh in sorted)
            {
                points.Add(CoordinateConverter.ToUnityPosition(fh.X2, fh.Y2));
            }
            
            edge.points = points.ToArray();
        }
        
        private List<Foothold> SortFootholdsInOrder(List<Foothold> group)
        {
            var sorted = new List<Foothold>();
            var remaining = new List<Foothold>(group);
            
            if (remaining.Count == 0) return sorted;
            
            // Start with first foothold
            var current = remaining[0];
            remaining.RemoveAt(0);
            sorted.Add(current);
            
            // Follow the chain
            while (remaining.Count > 0)
            {
                bool found = false;
                
                // Find next connected foothold
                for (int i = 0; i < remaining.Count; i++)
                {
                    var fh = remaining[i];
                    
                    // Check if this foothold connects to the end of our chain
                    if (fh.Id == current.Next || 
                        (current.X2 == fh.X1 && current.Y2 == fh.Y1))
                    {
                        current = fh;
                        remaining.RemoveAt(i);
                        sorted.Add(current);
                        found = true;
                        break;
                    }
                }
                
                if (!found) break;
            }
            
            return sorted;
        }
        
        private bool ShouldBeOneWayPlatform(Foothold foothold)
        {
            // Platforms that are mostly horizontal and not walls
            float angle = Mathf.Atan2(foothold.Y2 - foothold.Y1, foothold.X2 - foothold.X1) * Mathf.Rad2Deg;
            return Mathf.Abs(angle) < 45f;
        }
    }
    
    /// <summary>
    /// Component to store foothold data on platform GameObjects
    /// </summary>
    public class FootholdData : MonoBehaviour
    {
        public int footholdId;
        public int nextId;
        public int prevId;
    }
}