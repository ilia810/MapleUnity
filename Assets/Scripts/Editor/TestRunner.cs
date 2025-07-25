using UnityEditor;
using UnityEngine;

public class TestRunner : EditorWindow
{
    [MenuItem("MapleStory/Run Player States Tests")]
    public static void RunPlayerStatesTests()
    {
        Debug.Log("Running Player States Tests...");
        
        // Create a test instance
        var tests = new MapleClient.Tests.GameLogic.PlayerStatesTests();
        tests.Setup();
        
        // Run a simple test
        try
        {
            tests.Player_InitialState_IsStanding();
            Debug.Log("✓ Player_InitialState_IsStanding - PASSED");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Player_InitialState_IsStanding - FAILED: {e.Message}");
        }
        
        try
        {
            tests.Crouch_WhenGrounded_ChangesStateToCrouching();
            Debug.Log("✓ Crouch_WhenGrounded_ChangesStateToCrouching - PASSED");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Crouch_WhenGrounded_ChangesStateToCrouching - FAILED: {e.Message}");
        }
        
        Debug.Log("Test run complete!");
    }
}