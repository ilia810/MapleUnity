using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.GameData;
using System.Reflection;

public static class CreateAndAnalyzeCharacter
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "create-analyze-character.log");
        
        try
        {
            File.WriteAllText(logPath, "=== CREATE AND ANALYZE CHARACTER TEST ===\n");
            File.AppendAllText(logPath, $"Time: {System.DateTime.Now}\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n\n");
            
            // Create a new scene
            File.AppendAllText(logPath, "Creating new scene...\n");
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create character GameObject
            File.AppendAllText(logPath, "Creating character GameObject...\n");
            GameObject characterGO = new GameObject("TestCharacter");
            characterGO.transform.position = Vector3.zero;
            
            // Add MapleCharacterRenderer
            File.AppendAllText(logPath, "Adding MapleCharacterRenderer component...\n");
            var renderer = characterGO.AddComponent<MapleCharacterRenderer>();
            
            // Create mock player
            var player = new Player();
            // Position is set through the property, not a method
            // player.Position = new MapleClient.GameLogic.Vector2(0, 0);
            
            // Try to initialize with NXAssetLoader
            File.AppendAllText(logPath, "\nTrying to initialize with NXAssetLoader...\n");
            try
            {
                var nxLoader = NXAssetLoader.Instance;
                if (nxLoader != null)
                {
                    File.AppendAllText(logPath, "NXAssetLoader.Instance found!\n");
                    
                    // Initialize renderer with CharacterDataProvider
                    var characterDataProvider = new CharacterDataProvider();
                    renderer.Initialize(player, characterDataProvider);
                    File.AppendAllText(logPath, "Renderer initialized with NXAssetLoader\n");
                    
                    // Set default appearance
                    renderer.SetCharacterAppearance(0, 20000, 30000);
                    renderer.UpdateAppearance();
                    File.AppendAllText(logPath, "Set default appearance (skin:0, face:20000, hair:30000)\n");
                }
                else
                {
                    File.AppendAllText(logPath, "ERROR: NXAssetLoader.Instance is null\n");
                }
            }
            catch (System.Exception e)
            {
                File.AppendAllText(logPath, $"ERROR initializing with NXAssetLoader: {e.Message}\n");
            }
            
            // Force Unity to update
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            
            // Analyze the character after a delay
            EditorApplication.delayCall += () => {
                AnalyzeCharacterStructure(renderer, logPath);
                TestDirectionalFacing(renderer, logPath);
                TestNXDataLoading(logPath);
                
                File.AppendAllText(logPath, "\n=== TEST COMPLETED ===\n");
                Debug.Log($"Test completed. Results written to: {logPath}");
                EditorApplication.Exit(0);
            };
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nFATAL ERROR: {e.Message}\n");
            File.AppendAllText(logPath, $"Stack trace: {e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacterStructure(MapleCharacterRenderer renderer, string logPath)
    {
        File.AppendAllText(logPath, "\n=== CHARACTER STRUCTURE ANALYSIS ===\n");
        
        // Get all sprite renderers
        var sprites = renderer.GetComponentsInChildren<SpriteRenderer>(true);
        File.AppendAllText(logPath, $"Total sprite renderers: {sprites.Length}\n");
        
        // Analyze each sprite
        foreach (var sr in sprites)
        {
            File.AppendAllText(logPath, $"\n[{sr.gameObject.name}]\n");
            File.AppendAllText(logPath, $"  Active: {sr.gameObject.activeSelf}\n");
            File.AppendAllText(logPath, $"  Local Position: {sr.transform.localPosition}\n");
            File.AppendAllText(logPath, $"  World Position: {sr.transform.position}\n");
            File.AppendAllText(logPath, $"  Local Scale: {sr.transform.localScale}\n");
            File.AppendAllText(logPath, $"  Sorting Order: {sr.sortingOrder}\n");
            File.AppendAllText(logPath, $"  FlipX: {sr.flipX}\n");
            
            if (sr.sprite != null)
            {
                File.AppendAllText(logPath, $"  Sprite: {sr.sprite.name}\n");
                File.AppendAllText(logPath, $"  Size: {sr.sprite.rect.width}x{sr.sprite.rect.height}\n");
                File.AppendAllText(logPath, $"  Pivot: {sr.sprite.pivot}\n");
                File.AppendAllText(logPath, $"  PPU: {sr.sprite.pixelsPerUnit}\n");
            }
            else
            {
                File.AppendAllText(logPath, "  Sprite: NULL\n");
            }
        }
        
        // Check relative positions
        File.AppendAllText(logPath, "\n=== RELATIVE POSITIONS ===\n");
        
        var body = sprites.FirstOrDefault(s => s.gameObject.name.ToLower().Contains("body"));
        var arm = sprites.FirstOrDefault(s => s.gameObject.name.ToLower().Contains("arm"));
        var head = sprites.FirstOrDefault(s => s.gameObject.name.ToLower().Contains("head"));
        var face = sprites.FirstOrDefault(s => s.gameObject.name.ToLower().Contains("face"));
        
        if (body != null)
        {
            File.AppendAllText(logPath, $"Body found at Y: {body.transform.localPosition.y}\n");
            
            if (arm != null)
            {
                float armOffset = arm.transform.localPosition.y - body.transform.localPosition.y;
                File.AppendAllText(logPath, $"Arm offset from body: {armOffset:F3}\n");
                if (armOffset < -0.1f)
                {
                    File.AppendAllText(logPath, "WARNING: Arm appears BELOW body!\n");
                }
            }
            
            if (head != null)
            {
                float headOffset = head.transform.localPosition.y - body.transform.localPosition.y;
                File.AppendAllText(logPath, $"Head offset from body: {headOffset:F3}\n");
            }
        }
        
        if (head != null && face != null)
        {
            float faceOffset = face.transform.position.y - head.transform.position.y;
            File.AppendAllText(logPath, $"Face offset from head (world): {faceOffset:F3}\n");
        }
    }
    
    private static void TestDirectionalFacing(MapleCharacterRenderer renderer, string logPath)
    {
        File.AppendAllText(logPath, "\n=== DIRECTIONAL FACING TEST ===\n");
        
        var type = renderer.GetType();
        var setFlipXMethod = type.GetMethod("SetFlipX", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (setFlipXMethod != null)
        {
            // Test facing right (default)
            setFlipXMethod.Invoke(renderer, new object[] { false });
            File.AppendAllText(logPath, "\n--- Facing RIGHT (flipX = false) ---\n");
            LogSpriteStates(renderer, logPath);
            
            // Test facing left
            setFlipXMethod.Invoke(renderer, new object[] { true });
            File.AppendAllText(logPath, "\n--- Facing LEFT (flipX = true) ---\n");
            LogSpriteStates(renderer, logPath);
        }
        else
        {
            File.AppendAllText(logPath, "ERROR: SetFlipX method not found!\n");
        }
    }
    
    private static void LogSpriteStates(MapleCharacterRenderer renderer, string logPath)
    {
        var sprites = renderer.GetComponentsInChildren<SpriteRenderer>();
        string[] keyParts = { "body", "arm", "head", "face", "hair" };
        
        foreach (string partName in keyParts)
        {
            var sr = sprites.FirstOrDefault(s => s.gameObject.name.ToLower().Contains(partName));
            if (sr != null)
            {
                File.AppendAllText(logPath, $"{partName}: flipX={sr.flipX}, scale.x={sr.transform.localScale.x:F2}, pos={sr.transform.localPosition}\n");
            }
        }
    }
    
    private static void TestNXDataLoading(string logPath)
    {
        File.AppendAllText(logPath, "\n=== NX DATA LOADING TEST ===\n");
        
        try
        {
            var nxLoader = NXAssetLoader.Instance;
            if (nxLoader == null)
            {
                File.AppendAllText(logPath, "ERROR: NXAssetLoader.Instance is null\n");
                return;
            }
            
            // Test loading body parts with attachment data
            System.Collections.Generic.Dictionary<string, Vector2> attachmentPoints;
            var bodyParts = nxLoader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
            
            if (bodyParts != null)
            {
                File.AppendAllText(logPath, $"\nLoaded {bodyParts.Count} body parts\n");
                File.AppendAllText(logPath, $"Attachment points: {attachmentPoints.Count}\n");
                
                foreach (var attachment in attachmentPoints)
                {
                    File.AppendAllText(logPath, $"  {attachment.Key}: {attachment.Value}\n");
                }
                
                // Check specific attachment points
                if (attachmentPoints.ContainsKey("neck"))
                {
                    var neck = attachmentPoints["neck"];
                    File.AppendAllText(logPath, $"\nNeck attachment (for head): ({neck.x}, {neck.y})\n");
                    File.AppendAllText(logPath, $"In Unity units: ({neck.x / 100f:F3}, {neck.y / 100f:F3})\n");
                }
                
                if (attachmentPoints.ContainsKey("hand"))
                {
                    var hand = attachmentPoints["hand"];
                    File.AppendAllText(logPath, $"\nHand attachment: ({hand.x}, {hand.y})\n");
                    File.AppendAllText(logPath, $"In Unity units: ({hand.x / 100f:F3}, {hand.y / 100f:F3})\n");
                }
            }
            
            // Test loading face with data
            File.AppendAllText(logPath, "\n--- Face Data ---\n");
            var faceSprite = nxLoader.LoadFace(20000, "default");
            if (faceSprite != null)
            {
                File.AppendAllText(logPath, $"Face sprite loaded: {faceSprite.name}\n");
                File.AppendAllText(logPath, $"Face sprite size: {faceSprite.rect.width}x{faceSprite.rect.height}\n");
                File.AppendAllText(logPath, $"Face pivot: {faceSprite.pivot}\n");
            }
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"ERROR in NX data loading: {e.Message}\n");
        }
    }
}