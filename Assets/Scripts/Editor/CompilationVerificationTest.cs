using UnityEngine;
using UnityEditor;

public static class CompilationVerificationTest
{
    public static void VerifyCompilation()
    {
        Debug.Log("=== Compilation Verification Test ===");
        Debug.Log("If you see this message, compilation was successful.");
        Debug.Log("Exit code will be 0 for success.");
        EditorApplication.Exit(0);
    }
}
