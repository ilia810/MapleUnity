using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class CheckHenesysTileDetails : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check Henesys Tile Details")]
        public static void ShowWindow()
        {
            GetWindow<CheckHenesysTileDetails>("Henesys Tile Details");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check Henesys Tiles"))
            {
                CheckTiles();
            }
        }
        
        private void CheckTiles()
        {
            Debug.Log("=== Checking Henesys Tile Details ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(100000000);
            
            if (mapNode == null)
            {
                Debug.LogError("Failed to load Henesys map!");
                return;
            }
            
            // Check map-level tS
            var infoNode = mapNode["info"];
            if (infoNode != null)
            {
                var mapTS = infoNode["tS"];
                if (mapTS != null)
                {
                    Debug.Log($"Map-level tS value: '{mapTS.GetValue<string>()}'");
                }
                else
                {
                    Debug.Log("No map-level tS found");
                }
            }
            
            // Check tiles
            var tileNode = mapNode["tile"];
            if (tileNode == null)
            {
                Debug.Log("No tile node found");
                return;
            }
            
            int totalTiles = 0;
            int tilesWithTS = 0;
            var tilesetCounts = new System.Collections.Generic.Dictionary<string, int>();
            
            foreach (var tile in tileNode.Children)
            {
                totalTiles++;
                
                var tileTS = tile["tS"];
                if (tileTS != null)
                {
                    tilesWithTS++;
                    string tsValue = tileTS.GetValue<string>();
                    
                    if (!tilesetCounts.ContainsKey(tsValue))
                        tilesetCounts[tsValue] = 0;
                    tilesetCounts[tsValue]++;
                    
                    if (tilesWithTS <= 10)
                    {
                        Debug.Log($"Tile {tile.Name} has tS: '{tsValue}' (u={tile["u"]?.GetValue<string>()}, no={tile["no"]?.GetValue<int>()})");
                    }
                }
            }
            
            Debug.Log($"\nTotal tiles: {totalTiles}");
            Debug.Log($"Tiles with individual tS: {tilesWithTS}");
            Debug.Log($"Tiles without tS: {totalTiles - tilesWithTS}");
            
            if (tilesetCounts.Count > 0)
            {
                Debug.Log("\nTileset usage:");
                foreach (var kvp in tilesetCounts.OrderByDescending(x => x.Value))
                {
                    Debug.Log($"  '{kvp.Key}': {kvp.Value} tiles");
                }
            }
        }
    }
}