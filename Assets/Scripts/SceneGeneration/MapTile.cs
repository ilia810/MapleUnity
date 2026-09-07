using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class MapTile : MonoBehaviour
    {
        public string tileSet;
        public string variant;
        public int tileNumber;
        public int layer;
        public int z;           // Base depth from tile image
        public int zM;          // Map-specific depth offset
        public int sortingOrder; // Final calculated sorting order
    }
}
