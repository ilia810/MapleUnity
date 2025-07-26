using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class MapNXDebugger : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Map NX Structure")]
        public static void ShowWindow()
        {
            GetWindow<MapNXDebugger>("Map NX Debugger");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Map NX Debugger", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Debug Map.nx Structure"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugMapStructure();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Test Load Henesys (100000000) - Detailed"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                var node = nxManager.GetMapNode(100000000);
                if (node != null)
                {
                    Debug.Log($"Successfully loaded Henesys node: {node.Name}");
                    
                    // Check what backgrounds this map uses
                    var backNode = node["back"];
                    if (backNode != null)
                    {
                        Debug.Log("Henesys backgrounds (detailed):");
                        int bgIndex = 0;
                        foreach (var bg in backNode.Children)
                        {
                            Debug.Log($"  Background {bgIndex}:");
                            var bsNode = bg["bS"];
                            var noNode = bg["no"];
                            var typeNode = bg["type"];
                            var xNode = bg["x"];
                            var yNode = bg["y"];
                            var aniNode = bg["ani"];
                            
                            Debug.Log($"    - bS (sprite): {bsNode?.GetValue<string>() ?? "null"}");
                            Debug.Log($"    - no (layer): {noNode?.GetValue<int>() ?? -1}");
                            Debug.Log($"    - type: {typeNode?.GetValue<int>() ?? -1}");
                            Debug.Log($"    - x,y: {xNode?.GetValue<int>() ?? 0}, {yNode?.GetValue<int>() ?? 0}");
                            Debug.Log($"    - ani (animated): {aniNode?.GetValue<int>() ?? 0}");
                            
                            // Check for other potential sprite references
                            foreach (var child in bg.Children.Take(10))
                            {
                                if (child.Name != "bS" && child.Name != "no" && child.Name != "type" && 
                                    child.Name != "x" && child.Name != "y" && child.Name != "ani" &&
                                    child.Name != "rx" && child.Name != "ry" && child.Name != "a" && 
                                    child.Name != "front" && child.Name != "f")
                                {
                                    Debug.Log($"    - {child.Name}: {child.Value?.ToString() ?? "complex node"}");
                                }
                            }
                            bgIndex++;
                        }
                    }
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Debug Background Structure"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugBackgroundStructure();
            }
            
            if (GUILayout.Button("Debug Specific Background: grassySoil"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugBackgroundStructure("grassySoil");
            }
            
            if (GUILayout.Button("Search for 'grass' in backgrounds"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.SearchBackgroundsContaining("grass");
            }
            
            if (GUILayout.Button("Debug grassySoil Background Detail"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugBackgroundDetail("grassySoil");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Debug NPC Structure"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugNPCStructure();
            }
            
            if (GUILayout.Button("Debug Specific NPC: 9200000"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugNPCStructure("9200000");
            }
            
            if (GUILayout.Button("Debug Failed Sprite: 9000036.img/stand/0"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugSpritePath("9000036.img/stand/0");
            }
            
            if (GUILayout.Button("Debug Failed Object: guide.img/common/sign/0"))
            {
                var nxManager = NXDataManagerSingleton.Instance;
                nxManager.DebugSpritePath("Obj/guide.img/common/sign/0");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Cleanup NX Manager"))
            {
                NXDataManagerSingleton.Cleanup();
                Debug.Log("NX Manager cleaned up");
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Test Sprite Loading Improvements"))
            {
                TestSpriteLoadingImprovements();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Debug Tile Structure"))
            {
                DebugTileStructure();
            }
            
            if (GUILayout.Button("Debug Henesys Objects"))
            {
                DebugHenesysObjects();
            }
        }
        
        private void TestSpriteLoadingImprovements()
        {
            Debug.Log("=== Testing Sprite Loading Improvements ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            
            // Test 1: grassySoil background
            Debug.Log("\n--- Test 1: grassySoil background ---");
            var grassySoil = nxManager.GetBackgroundSprite("grassySoil");
            if (grassySoil != null)
            {
                Debug.Log($"✓ grassySoil loaded: {grassySoil.texture.width}x{grassySoil.texture.height}");
                // Check if it's the expected blue color
                var color = grassySoil.texture.GetPixel(128, 128);
                Debug.Log($"  Center pixel color: {color}");
            }
            else
            {
                Debug.LogError("✗ grassySoil failed to load");
            }
            
            // Test 2: Object sprite
            Debug.Log("\n--- Test 2: Object sprite (houseGS) ---");
            var objSprite = nxManager.GetObjectSprite("Obj/houseGS.img/house9/basic/1");
            if (objSprite != null)
            {
                Debug.Log($"✓ Object sprite loaded: {objSprite.texture.width}x{objSprite.texture.height}");
            }
            else
            {
                Debug.LogError("✗ Object sprite failed to load");
            }
            
            // Test 3: NPC sprite
            Debug.Log("\n--- Test 3: NPC sprite (9000036) ---");
            var npcSprite = nxManager.GetNPCSprite("9000036");
            if (npcSprite != null)
            {
                Debug.Log($"✓ NPC sprite loaded: {npcSprite.texture.width}x{npcSprite.texture.height}");
                // Check if it's not a green rectangle
                var npcColor = npcSprite.texture.GetPixel(npcSprite.texture.width/2, npcSprite.texture.height/2);
                Debug.Log($"  Center pixel color: {npcColor}");
            }
            else
            {
                Debug.LogError("✗ NPC sprite failed to load");
            }
            
            // Test 4: Another object sprite
            Debug.Log("\n--- Test 4: Object sprite (guide sign) ---");
            var signSprite = nxManager.GetObjectSprite("Obj/guide.img/common/sign/0");
            if (signSprite != null)
            {
                Debug.Log($"✓ Sign sprite loaded: {signSprite.texture.width}x{signSprite.texture.height}");
            }
            else
            {
                Debug.LogError("✗ Sign sprite failed to load");
            }
            
            Debug.Log("\n=== Test Complete ===");
        }
        
        private void DebugTileStructure()
        {
            Debug.Log("=== Debugging Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check Tile folder
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode != null)
            {
                Debug.Log("Tile folder found! First 20 entries:");
                int count = 0;
                foreach (var child in tileNode.Children)
                {
                    Debug.Log($"  - {child.Name}");
                    if (count++ >= 20) 
                    {
                        Debug.Log("  ... (truncated)");
                        break;
                    }
                }
                
                // Check specific tiles that might be used in Henesys
                string[] commonTiles = { "grassySoil", "wood", "woodPlatform", "enH0", "enV0" };
                foreach (var tileName in commonTiles)
                {
                    var tile = dataManager.GetNode("map", $"Tile/{tileName}.img");
                    if (tile != null)
                    {
                        Debug.Log($"\nFound tile: {tileName}");
                        foreach (var subNode in tile.Children.Take(5))
                        {
                            Debug.Log($"  - {subNode.Name} (has {subNode.Children.Count()} children)");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Tile folder not found in Map.nx!");
            }
        }
        
        private void DebugHenesysObjects()
        {
            Debug.Log("=== Debugging Henesys Objects ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(100000000); // Henesys
            
            if (mapNode == null)
            {
                Debug.LogError("Could not load Henesys map data!");
                return;
            }
            
            // Check object layers
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode != null)
                {
                    var objNode = layerNode["obj"];
                    if (objNode != null && objNode.Children.Any())
                    {
                        Debug.Log($"\nLayer {layer} objects (first 10):");
                        int count = 0;
                        foreach (var obj in objNode.Children)
                        {
                            var oS = obj["oS"]?.GetValue<string>(); // Object set name
                            var l0 = obj["l0"]?.GetValue<string>(); // Sub path
                            var l1 = obj["l1"]?.GetValue<string>();
                            var l2 = obj["l2"]?.GetValue<string>();
                            var x = obj["x"]?.GetValue<int>() ?? 0;
                            var y = obj["y"]?.GetValue<int>() ?? 0;
                            
                            Debug.Log($"  [{count}] {oS} - {l0}/{l1}/{l2} at ({x}, {y})");
                            
                            if (count++ >= 10) break;
                        }
                    }
                }
            }
            
            // Check tile layer specifically
            var tileLayerNode = mapNode["tile"];
            if (tileLayerNode != null)
            {
                Debug.Log($"\nTile layer found! Children: {tileLayerNode.Children.Count()}");
                foreach (var child in tileLayerNode.Children.Take(10))
                {
                    Debug.Log($"  - {child.Name}");
                    // Dive deeper into tile structure
                    var tS = child["tS"]?.GetValue<string>(); // Tile set
                    var x = child["x"]?.GetValue<int>() ?? 0;
                    var y = child["y"]?.GetValue<int>() ?? 0;
                    var u = child["u"]?.GetValue<string>(); // Tile variant
                    var no = child["no"]?.GetValue<int>() ?? 0; // Tile number
                    Debug.Log($"    tS={tS}, u={u}, no={no}, pos=({x},{y})");
                }
            }
            else
            {
                Debug.Log("\nNo tile layer found in map data");
            }
        }
    }
}