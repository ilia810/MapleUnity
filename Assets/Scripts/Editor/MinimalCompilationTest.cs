using UnityEngine;
using UnityEditor;

public static class MinimalCompilationTest
{
    public static void RunTest()
    {
        Debug.Log("Minimal test running!");
        EditorApplication.Exit(0);
    }
}