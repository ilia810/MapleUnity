using UnityEngine;
using UnityEditor;
using System.Linq;

public static class QuickCompilationTest
{
    public static void Run()
    {
        Debug.Log("=== Quick Compilation Test ===");
        
        // Check if ValidateCharacterRenderingFixes can be found
        var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp-Editor");
            
        if (assembly != null)
        {
            var validateType = assembly.GetType("ValidateCharacterRenderingFixes");
            if (validateType != null)
            {
                Debug.Log("SUCCESS: ValidateCharacterRenderingFixes class found!");
                
                // Try to find the RunValidation method
                var runMethod = validateType.GetMethod("RunValidation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (runMethod != null)
                {
                    Debug.Log("SUCCESS: RunValidation method found!");
                    Debug.Log("The file compiles correctly!");
                }
                else
                {
                    Debug.LogError("ERROR: RunValidation method not found!");
                }
            }
            else
            {
                Debug.LogError("ERROR: ValidateCharacterRenderingFixes class not found!");
            }
        }
        else
        {
            Debug.LogError("ERROR: Assembly-CSharp-Editor not found!");
        }
        
        EditorApplication.Exit(0);
    }
}