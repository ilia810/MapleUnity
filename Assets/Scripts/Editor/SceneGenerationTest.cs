using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class SceneGenerationTest : EditorWindow
    {
        [MenuItem("MapleUnity/Test Scene Generation")]
        public static void ShowWindow()
        {
            GetWindow<SceneGenerationTest>("Scene Generation Test");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Scene Generation Test", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Henesys Scene"))
            {
                GenerateHenesysScene();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Debug Henesys Data Extraction"))
            {
                DebugHenesysDataExtraction();
            }
            
            if (GUILayout.Button("Test Background Loading"))
            {
                TestBackgroundLoading();
            }
            
            if (GUILayout.Button("Test Tile Extraction"))
            {
                TestTileExtraction();
            }
        }
        
        private void GenerateHenesysScene()
        {
            Debug.Log("=== Generating Henesys Scene ===");
            
            // Create generator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            // Generate map
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot != null)
            {
                Debug.Log("Scene generated successfully!");
                
                // Report what was generated
                var backgrounds = mapRoot.GetComponentsInChildren<SpriteRenderer>().Length;
                var tiles = mapRoot.transform.Find("Tiles")?.GetComponentsInChildren<SpriteRenderer>().Length ?? 0;
                var objects = mapRoot.transform.Find("Objects")?.GetComponentsInChildren<SpriteRenderer>().Length ?? 0;
                var npcs = mapRoot.transform.Find("NPCs")?.GetComponentsInChildren<SpriteRenderer>().Length ?? 0;
                
                Debug.Log($"Generated: {backgrounds} sprite renderers total");
                Debug.Log($"  - Tiles: {tiles}");
                Debug.Log($"  - Objects: {objects}");
                Debug.Log($"  - NPCs: {npcs}");
            }
            
            // Clean up generator
            DestroyImmediate(generatorObj);
        }
        
        private void DebugHenesysDataExtraction()
        {
            Debug.Log("=== Debugging Henesys Data Extraction ===");
            
            MapDataExtractor extractor = new MapDataExtractor();
            var mapData = extractor.ExtractMapData(100000000);
            
            if (mapData != null)
            {
                Debug.Log($"Extracted data summary:");
                Debug.Log($"  - Backgrounds: {mapData.Backgrounds?.Count ?? 0}");
                Debug.Log($"  - Tiles: {mapData.Tiles?.Count ?? 0}");
                Debug.Log($"  - Objects: {mapData.Objects?.Count ?? 0}");
                Debug.Log($"  - NPCs: {mapData.NPCs?.Count ?? 0}");
                Debug.Log($"  - Monsters: {mapData.Monsters?.Count ?? 0}");
                Debug.Log($"  - Footholds: {mapData.Footholds?.Count ?? 0}");
                Debug.Log($"  - Portals: {mapData.Portals?.Count ?? 0}");
                
                // Detail backgrounds
                if (mapData.Backgrounds != null && mapData.Backgrounds.Count > 0)
                {
                    Debug.Log("\nBackground details (checking for inheritance bug):");
                    var nxManager = NXDataManagerSingleton.Instance;
                    int bgIndex = 0;
                    foreach (var bg in mapData.Backgrounds)
                    {
                        Debug.Log($"  Background[{bgIndex}] No={bg.No}, BgName='{bg.BgName}', Type={bg.Type}, Pos=({bg.X}, {bg.Y})");
                        
                        // Check the actual node to see if bS exists
                        var backNode = nxManager.GetMapNode(100000000)?["back"];
                        if (backNode != null)
                        {
                            var bgNode = backNode[bgIndex.ToString()];
                            if (bgNode != null)
                            {
                                var actualBs = bgNode["bS"]?.GetValue<string>();
                                Debug.Log($"    Actual bS from node: '{actualBs ?? "null"}'");
                                if (actualBs != bg.BgName)
                                {
                                    Debug.LogError($"    ERROR: BgName mismatch! Extracted='{bg.BgName}', Actual='{actualBs ?? "null"}'");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"    Could not find background node at index {bgIndex}");
                            }
                        }
                        bgIndex++;
                    }
                }
                
                // Detail tiles
                if (mapData.Tiles != null && mapData.Tiles.Count > 0)
                {
                    Debug.Log($"\nFirst 10 tiles:");
                    int count = 0;
                    foreach (var tile in mapData.Tiles)
                    {
                        Debug.Log($"  Tile: {tile.TileSet}/{tile.Variant}/{tile.No} at ({tile.X}, {tile.Y})");
                        if (++count >= 10) break;
                    }
                }
                else
                {
                    Debug.Log("\nNo tiles extracted!");
                }
                
                // Detail objects
                if (mapData.Objects != null && mapData.Objects.Count > 0)
                {
                    Debug.Log($"\nObjects by layer:");
                    
                    // Show first few objects per layer
                    for (int layer = 0; layer <= 7; layer++)
                    {
                        var layerObjects = mapData.Objects.Where(o => o.Layer == layer).Take(5).ToList();
                        if (layerObjects.Any())
                        {
                            Debug.Log($"\nLayer {layer} objects (showing {layerObjects.Count} of {mapData.Objects.Count(o => o.Layer == layer)}):");
                            foreach (var obj in layerObjects)
                            {
                                string objPath = obj.ObjName;
                                if (!string.IsNullOrEmpty(obj.L0)) objPath += "/" + obj.L0;
                                if (!string.IsNullOrEmpty(obj.L1)) objPath += "/" + obj.L1;
                                if (!string.IsNullOrEmpty(obj.L2)) objPath += "/" + obj.L2;
                                
                                Debug.Log($"  {objPath} at ({obj.X}, {obj.Y}), Z={obj.Z}");
                                
                                // Check if this might be a ground/tile object
                                if (obj.ObjName.ToLower().Contains("tile") || obj.ObjName.ToLower().Contains("floor") || 
                                    obj.ObjName.ToLower().Contains("ground") || obj.ObjName.ToLower().Contains("platform") ||
                                    obj.ObjName.ToLower().Contains("enh") || obj.ObjName.ToLower().Contains("env"))
                                {
                                    Debug.Log($"    ^ This might be terrain!");
                                }
                            }
                        }
                    }
                    
                    // Group objects by layer to see distribution
                    Debug.Log("\nObject distribution by layer:");
                    var layerCounts = mapData.Objects.GroupBy(o => o.Layer)
                                                   .OrderBy(g => g.Key)
                                                   .Select(g => $"Layer {g.Key}: {g.Count()} objects");
                    foreach (var lc in layerCounts)
                    {
                        Debug.Log($"  {lc}");
                    }
                }
            }
        }
        
        private void TestBackgroundLoading()
        {
            Debug.Log("=== Testing Background Loading ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            
            // Test different background types
            string[] bgTests = {
                "grassySoil",           // Type 3 - should be blue
                "henesysHill",         // Type 0 - static image
                "henesysTree",         // Another potential background
                "henesysCloud"         // Type 4 - cloud/effect
            };
            
            foreach (var bgName in bgTests)
            {
                var sprite = nxManager.GetBackgroundSprite(bgName);
                if (sprite != null)
                {
                    Debug.Log($"✓ {bgName}: {sprite.texture.width}x{sprite.texture.height}");
                }
                else
                {
                    Debug.Log($"✗ {bgName}: Not found");
                }
            }
            
            // Search for Henesys-related backgrounds
            nxManager.SearchBackgroundsContaining("henes");
            
            // Check background nodes by index
            Debug.Log("\nChecking background nodes by index:");
            var mapNode = nxManager.GetMapNode(100000000);
            if (mapNode != null)
            {
                var backNode = mapNode["back"];
                if (backNode != null)
                {
                    foreach (var bg in backNode.Children.Take(8))
                    {
                        Debug.Log($"\nBackground node {bg.Name}:");
                        
                        // Check if the node itself is an image
                        if (bg.Value != null)
                        {
                            Debug.Log($"  Node has direct value: {bg.Value.GetType().Name}");
                        }
                        
                        // Check all children
                        foreach (var child in bg.Children)
                        {
                            Debug.Log($"  Child: {child.Name} = {child.Value?.ToString() ?? "null"}");
                            if (child.Value != null && child.Value is byte[])
                            {
                                Debug.Log($"    ^ This is image data!");
                            }
                        }
                    }
                }
            }
        }
        
        private void TestTileExtraction()
        {
            Debug.Log("=== Testing Tile Extraction ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(100000000);
            
            if (mapNode == null)
            {
                Debug.LogError("Could not load Henesys map!");
                return;
            }
            
            // Check for tile node
            var tileNode = mapNode["tile"];
            if (tileNode != null)
            {
                Debug.Log($"Tile node found! Children: {tileNode.Children.Count()}");
                int count = 0;
                foreach (var tile in tileNode.Children)
                {
                    var tS = tile["tS"]?.GetValue<string>();
                    var u = tile["u"]?.GetValue<string>();
                    var no = tile["no"]?.GetValue<int>() ?? 0;
                    var x = tile["x"]?.GetValue<int>() ?? 0;
                    var y = tile["y"]?.GetValue<int>() ?? 0;
                    
                    Debug.Log($"  Tile {count}: tS={tS}, u={u}, no={no}, pos=({x},{y})");
                    
                    // Test loading this tile
                    if (!string.IsNullOrEmpty(tS))
                    {
                        var tilePath = $"Tile/{tS}.img/{u}/{no}";
                        var tileSprite = nxManager.GetBackgroundSprite(tilePath);
                        if (tileSprite != null)
                        {
                            Debug.Log($"    ✓ Sprite loaded: {tileSprite.texture.width}x{tileSprite.texture.height}");
                        }
                        else
                        {
                            Debug.Log($"    ✗ Sprite not found at: {tilePath}");
                        }
                    }
                    
                    if (++count >= 5) break;
                }
            }
            else
            {
                Debug.LogError("No tile node found in Henesys map data!");
                
                // Check what nodes exist at root level
                Debug.Log("Root level nodes:");
                foreach (var child in mapNode.Children.Take(20))
                {
                    Debug.Log($"  - {child.Name}");
                }
            }
        }
    }
}