using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

// Minimal test to analyze character positions without dependencies on broken scripts
public static class DirectCharacterPositionTest
{
    public static void RunAnalysis()
    {
        var output = new StringBuilder();
        output.AppendLine("=== Direct Character Position Test ===");
        output.AppendLine($"Started at: {System.DateTime.Now}");
        
        try
        {
            // Create a new scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            output.AppendLine("Created new scene");
            
            // Create character GameObject
            GameObject characterGO = new GameObject("TestCharacter");
            characterGO.transform.position = Vector3.zero;
            
            // Try to add MapleCharacterRenderer using reflection to avoid compilation issues
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, GameView");
            if (rendererType == null)
            {
                output.AppendLine("ERROR: Could not find MapleCharacterRenderer type");
                output.AppendLine("Attempting alternate type search...");
                
                // Try to find the type by searching all assemblies
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == "MapleCharacterRenderer")
                        {
                            rendererType = type;
                            output.AppendLine($"Found type in assembly: {assembly.GetName().Name}");
                            break;
                        }
                    }
                    if (rendererType != null) break;
                }
            }
            
            if (rendererType != null)
            {
                var renderer = characterGO.AddComponent(rendererType);
                output.AppendLine("Added MapleCharacterRenderer component");
                
                // Try to call Start
                var startMethod = rendererType.GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (startMethod != null)
                {
                    startMethod.Invoke(renderer, null);
                    output.AppendLine("Called Start() method");
                }
                
                // Analyze sprite positions after a delay
                EditorApplication.delayCall += () => {
                    AnalyzePositions(characterGO, output);
                };
            }
            else
            {
                output.AppendLine("ERROR: MapleCharacterRenderer type not found in any assembly");
                
                // Create test sprites manually
                output.AppendLine("\nCreating manual test sprites...");
                CreateManualTestSprites(characterGO, output);
                
                SaveAndExit(output);
            }
        }
        catch (System.Exception e)
        {
            output.AppendLine($"\nERROR: {e.Message}");
            output.AppendLine($"Stack trace:\n{e.StackTrace}");
            SaveAndExit(output);
        }
    }
    
    static void AnalyzePositions(GameObject characterGO, StringBuilder output)
    {
        try
        {
            output.AppendLine("\n=== Analyzing Character Sprite Positions ===");
            
            // Find all sprite renderers
            var sprites = characterGO.GetComponentsInChildren<SpriteRenderer>();
            output.AppendLine($"Found {sprites.Length} sprite renderers");
            
            foreach (var sr in sprites)
            {
                output.AppendLine($"\n[{sr.gameObject.name}]");
                output.AppendLine($"  Local Position: {sr.transform.localPosition}");
                output.AppendLine($"  World Position: {sr.transform.position}");
                output.AppendLine($"  Sorting Order: {sr.sortingOrder}");
                output.AppendLine($"  Has Sprite: {sr.sprite != null}");
                if (sr.sprite != null)
                {
                    output.AppendLine($"  Sprite Size: {sr.sprite.rect.width}x{sr.sprite.rect.height}");
                    output.AppendLine($"  Pivot: {sr.sprite.pivot}");
                }
            }
            
            // Check specific parts
            output.AppendLine("\n=== Checking Specific Parts ===");
            CheckPart(characterGO, "body", output);
            CheckPart(characterGO, "arm", output);
            CheckPart(characterGO, "head", output);
            CheckPart(characterGO, "face", output);
            
            // Analyze relative positions
            output.AppendLine("\n=== Position Analysis ===");
            var body = characterGO.transform.Find("body");
            var arm = characterGO.transform.Find("arm");
            var head = characterGO.transform.Find("head");
            
            if (body != null && arm != null)
            {
                float diff = arm.localPosition.y - body.localPosition.y;
                output.AppendLine($"Arm vs Body Y difference: {diff:F3}");
                if (diff < -0.1f)
                {
                    output.AppendLine("WARNING: ARM IS BELOW BODY!");
                }
                else if (System.Math.Abs(diff) < 0.01f)
                {
                    output.AppendLine("WARNING: Arm and body are at same Y position (expected arm at Y=0.20)");
                }
            }
            
            if (body != null && head != null)
            {
                float diff = head.localPosition.y - body.localPosition.y;
                output.AppendLine($"Head vs Body Y difference: {diff:F3}");
                if (diff < 0)
                {
                    output.AppendLine("WARNING: HEAD IS BELOW BODY!");
                }
            }
            
            output.AppendLine("\n=== Expected vs Actual Positions ===");
            output.AppendLine("Expected: Body=0.00, Arm=0.20, Head=0.28+");
            output.AppendLine($"Actual: Body={body?.localPosition.y ?? -999:F3}, Arm={arm?.localPosition.y ?? -999:F3}, Head={head?.localPosition.y ?? -999:F3}");
        }
        catch (System.Exception e)
        {
            output.AppendLine($"\nError during analysis: {e.Message}");
        }
        
        SaveAndExit(output);
    }
    
    static void CheckPart(GameObject parent, string partName, StringBuilder output)
    {
        var part = parent.transform.Find(partName);
        if (part != null)
        {
            output.AppendLine($"{partName}: Found at Y={part.localPosition.y:F3}");
        }
        else
        {
            output.AppendLine($"{partName}: NOT FOUND");
            
            // Check if it exists as child of another part
            if (partName == "face")
            {
                var head = parent.transform.Find("head");
                if (head != null)
                {
                    var face = head.Find("face");
                    if (face != null)
                    {
                        output.AppendLine($"  Found as child of head at Y={face.position.y:F3} (world)");
                    }
                }
            }
        }
    }
    
    static void CreateManualTestSprites(GameObject parent, StringBuilder output)
    {
        // Create test sprites at expected positions
        CreateSpritePart(parent, "body", new Vector3(0, 0, 0), 0, output);
        CreateSpritePart(parent, "arm", new Vector3(0, 0.2f, 0), 1, output);
        CreateSpritePart(parent, "head", new Vector3(0, 0.28f, 0), 2, output);
        
        output.AppendLine("\nCreated manual test sprites at expected positions");
    }
    
    static void CreateSpritePart(GameObject parent, string name, Vector3 position, int sortingOrder, StringBuilder output)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = position;
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        
        output.AppendLine($"Created {name} at {position}");
    }
    
    static void SaveAndExit(StringBuilder output)
    {
        string filePath = "character-position-analysis.txt";
        File.WriteAllText(filePath, output.ToString());
        Debug.Log($"Analysis saved to {filePath}");
        Debug.Log(output.ToString());
        
        EditorApplication.Exit(0);
    }
}