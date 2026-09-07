using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameLogic
{
    /// <summary>
    /// Concrete implementation of IFootholdService for querying foothold (ground) information.
    /// Works entirely in MapleStory coordinate system.
    /// </summary>
    public class FootholdService : IFootholdService
    {
        public int Revision { get; private set; }
        private List<Foothold> footholds = new List<Foothold>();
        private Dictionary<int, Foothold> footholdById = new Dictionary<int, Foothold>();
        
        /// <summary>
        /// Load or update foothold data
        /// </summary>
        public void LoadFootholds(List<Foothold> footholdData)
        {
            Revision++;
            footholds = new List<Foothold>(footholdData);
            footholdById.Clear();
            
            foreach (var fh in footholds)
            {
                footholdById[fh.Id] = fh;
            }
            
            // Log summary of loaded footholds
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] Loaded {footholds.Count} footholds:");
            foreach (var fh in footholds.Take(5)) // Log first 5
            {
                System.Console.WriteLine($"[FOOTHOLD_COLLISION]   Foothold {fh.Id}: X[{fh.X1},{fh.X2}] Y[{fh.Y1},{fh.Y2}]");
            }
            if (footholds.Count > 5)
            {
                System.Console.WriteLine($"[FOOTHOLD_COLLISION]   ... and {footholds.Count - 5} more");
            }
        }
        
        /// <summary>
        /// Update a single foothold
        /// </summary>
        public void UpdateFoothold(Foothold foothold)
        {
            Revision++;
            if (footholdById.ContainsKey(foothold.Id))
            {
                // Update existing
                var index = footholds.FindIndex(f => f.Id == foothold.Id);
                if (index >= 0)
                {
                    footholds[index] = foothold;
                }
            }
            else
            {
                // Add new
                footholds.Add(foothold);
            }
            
            footholdById[foothold.Id] = foothold;
        }
        
        /// <summary>
        /// Gets the ground Y position below a given point.
        /// Mimics the C++ client's Physics::get_y_below function
        /// </summary>
        public float GetGroundBelow(float x, float y)
        {
            float closestY = float.MaxValue;
            foreach (var foothold in footholds)
            {
                if (IsWall(foothold)) continue;
                float groundY = foothold.GetYAtX(x);
                // Preserve the legacy near-ground query and HeavenClient ground-1 offset.
                // Landing sweeps are independent of this lookup tolerance.
                if (!float.IsNaN(groundY) && groundY >= y - 10 && groundY < closestY) closestY = groundY;
            }
            return closestY == float.MaxValue ? float.MaxValue : closestY - 1;
        }

        /// <summary>
        /// Checks if a position is on solid ground within the specified tolerance.
        /// </summary>
        public bool IsOnGround(float x, float y, float tolerance = 1f)
        {
            float groundY = GetGroundBelow(x, y - tolerance);
            
            if (groundY == float.MaxValue)
                return false;
            
            // Check if we're within tolerance of the ground
            // Add 1 back since GetGroundBelow subtracts 1
            float actualGroundY = groundY + 1;
            return System.Math.Abs(y - actualGroundY) <= tolerance;
        }
        
        /// <summary>
        /// Gets the foothold at the specified position.
        /// </summary>
        public Foothold GetFootholdAt(float x, float y)
        {
            foreach (var fh in footholds)
            {
                if (!IsWall(fh) && fh.ContainsPoint(x, y, 5f)) // 5 pixel tolerance
                {
                    return fh;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the foothold below a given point (for landing calculations).
        /// </summary>
        public Foothold GetFootholdBelow(float x, float y)
        {
            Foothold closest = null;
            float closestY = float.MaxValue;
            
            foreach (var fh in footholds)
            {
                if (IsWall(fh)) continue;
                float minX = System.Math.Min(fh.X1, fh.X2);
                float maxX = System.Math.Max(fh.X1, fh.X2);
                
                if (x >= minX && x <= maxX)
                {
                    float groundY = fh.GetYAtX(x);
                    
                    if (!float.IsNaN(groundY) && groundY >= y && groundY < closestY)
                    {
                        closest = fh;
                        closestY = groundY;
                    }
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Gets all footholds in a specified area.
        /// </summary>
        public IEnumerable<Foothold> GetFootholdsInArea(float minX, float minY, float maxX, float maxY)
        {
            foreach (var fh in footholds)
            {
                // Check if foothold intersects with the area
                float fhMinX = System.Math.Min(fh.X1, fh.X2);
                float fhMaxX = System.Math.Max(fh.X1, fh.X2);
                float fhMinY = System.Math.Min(fh.Y1, fh.Y2);
                float fhMaxY = System.Math.Max(fh.Y1, fh.Y2);
                
                // Check for intersection
                if (fhMaxX >= minX && fhMinX <= maxX && fhMaxY >= minY && fhMinY <= maxY)
                {
                    yield return fh;
                }
            }
        }
        
        /// <summary>
        /// Gets the connected foothold in the specified direction.
        /// </summary>
        public Foothold GetConnectedFoothold(Foothold currentFoothold, bool movingRight)
        {
            if (currentFoothold == null)
                return null;
            
            int connectedId = movingRight ? currentFoothold.NextId : currentFoothold.PreviousId;
            
            if (connectedId == 0)
                return null;
            
            return footholdById.TryGetValue(connectedId, out var connected) ? connected : null;
        }
        
        /// <summary>
        /// Finds the nearest foothold to a given position.
        /// </summary>
        public Foothold FindNearestFoothold(float x, float y, float maxDistance = 1000f)
        {
            Foothold nearest = null;
            float nearestDistSq = maxDistance * maxDistance;
            
            foreach (var fh in footholds)
            {
                // Find closest point on foothold line segment
                float closestX, closestY;
                GetClosestPointOnFoothold(fh, x, y, out closestX, out closestY);
                
                float dx = x - closestX;
                float dy = y - closestY;
                float distSq = dx * dx + dy * dy;
                
                if (distSq < nearestDistSq)
                {
                    nearest = fh;
                    nearestDistSq = distSq;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Checks if a foothold is a wall (vertical platform).
        /// </summary>
        public bool IsWall(Foothold foothold)
        {
            if (foothold == null)
                return false;
            
            // Check both the IsWall flag and if it's nearly vertical
            return foothold.IsWall || System.Math.Abs(foothold.X2 - foothold.X1) < 0.1f;
        }
        
        /// <summary>
        /// Gets the slope at a specific X position on a foothold.
        /// </summary>
        public float GetSlopeAt(Foothold foothold, float x)
        {
            if (foothold == null)
                return 0f;
            
            // Check if X is within foothold bounds
            float minX = System.Math.Min(foothold.X1, foothold.X2);
            float maxX = System.Math.Max(foothold.X1, foothold.X2);
            
            if (x < minX || x > maxX)
                return 0f;
            
            return foothold.GetSlope();
        }
        
        /// <summary>
        /// Helper method to find the closest point on a foothold line segment
        /// </summary>
        private void GetClosestPointOnFoothold(Foothold fh, float px, float py, out float closestX, out float closestY)
        {
            float dx = fh.X2 - fh.X1;
            float dy = fh.Y2 - fh.Y1;
            
            if (System.Math.Abs(dx) < 0.0001f && System.Math.Abs(dy) < 0.0001f)
            {
                // Degenerate foothold (point)
                closestX = fh.X1;
                closestY = fh.Y1;
                return;
            }
            
            float t = ((px - fh.X1) * dx + (py - fh.Y1) * dy) / (dx * dx + dy * dy);
            t = System.Math.Max(0, System.Math.Min(1, t));
            
            closestX = fh.X1 + t * dx;
            closestY = fh.Y1 + t * dy;
        }
    }
}