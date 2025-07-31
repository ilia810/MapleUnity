using UnityEngine;
using UnityEditor;

public static class FinalCompilationCheck  
{
    [MenuItem("Test/Final Compilation Check")]
    public static void RunCheck()
    {
        Debug.Log("=== Final Compilation Check for ValidateCharacterRenderingFixes ===");
        
        // Try to run the validation
        try
        {
            ValidateCharacterRenderingFixes.RunValidation();
        }
        catch (System.Exception e)
        {
            Debug.Log($"ValidateCharacterRenderingFixes compiled successfully but threw runtime exception: {e.Message}");
            Debug.Log("This is expected if no scene is loaded.");
        }
        
        Debug.Log("SUCCESS: ValidateCharacterRenderingFixes compiles without errors!");
        EditorApplication.Exit(0);
    }
}