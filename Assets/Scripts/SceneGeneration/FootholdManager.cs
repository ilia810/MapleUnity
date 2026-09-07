using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Manages foothold data and provides methods to query ground positions
    /// </summary>
    public class FootholdManager : MonoBehaviour
    {
        private List<Foothold> footholds = new List<Foothold>();
        private Dictionary<int, Foothold> footholdById = new Dictionary<int, Foothold>();
        
        private static FootholdManager instance;
        public static FootholdManager Instance => instance;
        
        private void Awake()
        {
            instance = this;
        }
        
        /// <summary>
        /// Initialize with foothold data
        /// </summary>
        public void Initialize(List<Foothold> footholdData)
        {
            footholds = footholdData;
            footholdById.Clear();
            
            foreach (var fh in footholds)
            {
                footholdById[fh.Id] = fh;
            }
        }
        
        /// <summary>
        /// Get the Y position of the ground below a given X,Y position
        /// Mimics the C++ client's Physics::get_y_below function
        /// </summary>
        public float GetYBelow(float x, float y)
        {
            return TryGetGroundBelow(x, y, out float groundY) ? groundY - 1f : y;
        }

        // Exact settled surface, separate from HeavenClient's one-pixel spawn query.
        public bool TryGetGroundBelow(float x, float y, out float groundY)
        {
            groundY = float.PositiveInfinity;
            bool found = false;
            foreach (var fh in footholds)
            {
                if (fh.X1 == fh.X2 || x < Mathf.Min(fh.X1, fh.X2) || x > Mathf.Max(fh.X1, fh.X2))
                    continue;
                float candidate = GetYAtX(fh, x);
                if (candidate >= y - 0.001f && candidate < groundY)
                {
                    groundY = candidate;
                    found = true;
                }
            }
            if (!found) groundY = y;
            return found;
        }

        /// <summary>
        /// Calculate the Y position on a foothold at a given X coordinate
        /// </summary>
        private float GetYAtX(Foothold fh, float x)
        {
            // Handle vertical footholds
            if (fh.X1 == fh.X2)
            {
                // For vertical footholds, return the lower Y (higher value in MapleStory coords)
                return Mathf.Max(fh.Y1, fh.Y2);
            }
            
            // Linear interpolation for sloped footholds
            float t = (x - fh.X1) / (fh.X2 - fh.X1);
            return Mathf.Lerp(fh.Y1, fh.Y2, t);
        }
        
        /// <summary>
        /// Get the foothold at a specific position
        /// </summary>
        public Foothold GetFootholdAt(float x, float y)
        {
            foreach (var fh in footholds)
            {
                float minX = Mathf.Min(fh.X1, fh.X2);
                float maxX = Mathf.Max(fh.X1, fh.X2);
                
                if (x >= minX && x <= maxX)
                {
                    float groundY = GetYAtX(fh, x);
                    float minY = Mathf.Min(fh.Y1, fh.Y2);
                    float maxY = Mathf.Max(fh.Y1, fh.Y2);
                    
                    // Check if position is on or very close to this foothold
                    if (Mathf.Abs(y - groundY) < 5) // 5 pixel tolerance
                    {
                        return fh;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get foothold by ID
        /// </summary>
        public Foothold GetFootholdById(int id)
        {
            return footholdById.TryGetValue(id, out var fh) ? fh : null;
        }
        
        /// <summary>
        /// Get all footholds in the map
        /// </summary>
        public List<Foothold> GetAllFootholds()
        {
            return footholds;
        }
    }
}