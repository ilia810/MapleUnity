using UnityEngine;
using UnityEditor;
using System.Collections;

public class RunUnityTest : MonoBehaviour
{
    [MenuItem("Test/Run Collision Test")]
    static void RunCollisionTest()
    {
        UnityEditor.EditorApplication.OpenScene("Assets/henesys.unity");
        EditorApplication.isPlaying = true;
    }
    
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (System.Environment.GetCommandLineArgs().Length > 0)
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-runTest")
                {
                    RunCollisionTest();
                }
            }
        }
    }
}