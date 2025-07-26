using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class CheckDefaultTileset : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check Default .img Tileset")]
        public static void ShowWindow()
        {
            GetWindow<CheckDefaultTileset>("Check .img Tileset");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check for .img tileset"))
            {
                CheckImgTileset();
            }
        }
        
        private void CheckImgTileset()
        {
            Debug.Log("=== Checking for .img tileset ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            
            // Use the new method
            bool exists = nxManager.CheckImgTilesetExists();
            
            if (!exists)
            {
                var dataManager = nxManager.DataManager;
                
                // List first few tilesets to see what we have
                var tileNode = dataManager.GetNode("map", "Tile");
                if (tileNode != null)
                {
                    Debug.Log("\nFirst 20 tilesets in Tile folder:");
                    foreach (var tileset in tileNode.Children.Take(20))
                    {
                        Debug.Log($"  - {tileset.Name}");
                    }
                }
            }
        }
    }
}