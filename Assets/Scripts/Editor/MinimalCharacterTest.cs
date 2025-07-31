using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class MinimalCharacterTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\minimal-character-test.log";
        
        try
        {
            File.WriteAllText(logPath, "Starting minimal character rendering test\n");
            
            // Load scene
            EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(logPath, "Scene loaded\n");
            
            // Find player
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                File.AppendAllText(logPath, "ERROR: Player not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, "Player found at: " + player.transform.position + "\n");
            File.AppendAllText(logPath, "Player scale: " + player.transform.localScale + "\n");
            
            // Get sprite renderers
            SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            File.AppendAllText(logPath, "\nFound " + renderers.Length + " sprite renderers:\n");
            
            foreach (SpriteRenderer sr in renderers)
            {
                File.AppendAllText(logPath, "\n[" + sr.name + "]\n");
                File.AppendAllText(logPath, "  Position: " + sr.transform.position + "\n");
                File.AppendAllText(logPath, "  Local Position: " + sr.transform.localPosition + "\n");
                File.AppendAllText(logPath, "  Active: " + sr.gameObject.activeSelf + "\n");
                File.AppendAllText(logPath, "  Has Sprite: " + (sr.sprite != null) + "\n");
                File.AppendAllText(logPath, "  FlipX: " + sr.flipX + "\n");
            }
            
            // Check head position
            SpriteRenderer head = null;
            SpriteRenderer body = null;
            SpriteRenderer arm = null;
            SpriteRenderer face = null;
            
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr.name == "Head") head = sr;
                if (sr.name == "Body") body = sr;
                if (sr.name == "Arm") arm = sr;
                if (sr.name == "Face") face = sr;
            }
            
            File.AppendAllText(logPath, "\n=== ALIGNMENT CHECKS ===\n");
            
            if (head != null && body != null)
            {
                float headOffset = head.transform.position.y - body.transform.position.y;
                File.AppendAllText(logPath, "Head Y offset from body: " + headOffset + "\n");
                File.AppendAllText(logPath, "Status: " + (headOffset > 0.4f ? "PASS - Head above body" : "FAIL - Head not properly above body") + "\n");
            }
            
            if (arm != null && body != null)
            {
                float armOffset = arm.transform.position.y - body.transform.position.y;
                File.AppendAllText(logPath, "Arm Y offset from body: " + armOffset + "\n");
                File.AppendAllText(logPath, "Status: " + (System.Math.Abs(armOffset) < 0.01f ? "PASS - Arm aligned with body" : "FAIL - Arm misaligned") + "\n");
            }
            
            if (face != null && head != null)
            {
                float faceXOffset = face.transform.position.x - head.transform.position.x;
                float faceYOffset = face.transform.position.y - head.transform.position.y;
                File.AppendAllText(logPath, "Face offset from head: (" + faceXOffset + ", " + faceYOffset + ")\n");
                bool aligned = System.Math.Abs(faceXOffset) < 0.01f && System.Math.Abs(faceYOffset) < 0.01f;
                File.AppendAllText(logPath, "Status: " + (aligned ? "PASS - Face aligned with head" : "FAIL - Face misaligned") + "\n");
            }
            
            File.AppendAllText(logPath, "\nTest completed successfully\n");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, "ERROR: " + e.Message + "\n" + e.StackTrace + "\n");
            EditorApplication.Exit(1);
        }
    }
}