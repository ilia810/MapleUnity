using UnityEngine;
using System.Collections.Generic;

namespace MapleClient.GameView
{
    public class CharacterRenderingDebugger : MonoBehaviour
    {
        private MapleCharacterRenderer characterRenderer;
        private bool hasLogged = false;
        
        void Start()
        {
            characterRenderer = GetComponent<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                Debug.LogError("[CharacterRenderingDebugger] MapleCharacterRenderer not found!");
                enabled = false;
            }
        }
        
        void Update()
        {
            // Log once after first frame when everything is initialized
            if (!hasLogged && Time.frameCount > 5)
            {
                LogCharacterStructure();
                hasLogged = true;
            }
        }
        
        private void LogCharacterStructure()
        {
            Debug.Log("=== CHARACTER RENDERING DEBUG ===");
            Debug.Log($"Character Position: {transform.position}");
            Debug.Log($"Character Local Scale: {transform.localScale}");
            
            // Log all child transforms
            Debug.Log("\n--- TRANSFORM HIERARCHY ---");
            LogTransformRecursive(transform, 0);
            
            // Find specific parts
            Transform face = transform.Find("Face");
            Transform head = transform.Find("Head");
            Transform body = transform.Find("Body");
            
            if (face != null)
            {
                Debug.Log($"\n--- FACE ANALYSIS ---");
                Debug.Log($"Face Local Position: {face.localPosition}");
                Debug.Log($"Face World Position: {face.position}");
                
                // Convert to pixel units for comparison with C++
                Vector2 facePixelOffset = new Vector2(face.localPosition.x * 100f, face.localPosition.y * 100f);
                Debug.Log($"Face Pixel Offset from Character: ({facePixelOffset.x:F0}, {facePixelOffset.y:F0})");
                
                // Check against expected C++ value
                Vector2 expectedOffset = new Vector2(-8f, -52f);
                float distance = Vector2.Distance(facePixelOffset, expectedOffset);
                
                if (distance < 1f)
                {
                    Debug.Log($"✓ Face offset matches C++ runtime! Distance: {distance:F2} pixels");
                }
                else
                {
                    Debug.LogWarning($"✗ Face offset mismatch! Expected: {expectedOffset}, Got: {facePixelOffset}, Distance: {distance:F2} pixels");
                }
            }
            
            // Also check if face is under head
            if (head != null)
            {
                Transform faceUnderHead = head.Find("Face");
                if (faceUnderHead != null)
                {
                    Debug.Log($"\n--- FACE UNDER HEAD ---");
                    Debug.Log($"Face is child of Head");
                    Debug.Log($"Face relative to Head: {faceUnderHead.localPosition}");
                    
                    // Calculate total offset from character origin
                    Vector3 totalOffset = head.localPosition + faceUnderHead.localPosition;
                    Vector2 totalPixelOffset = new Vector2(totalOffset.x * 100f, totalOffset.y * 100f);
                    Debug.Log($"Total Face Pixel Offset: ({totalPixelOffset.x:F0}, {totalPixelOffset.y:F0})");
                }
            }
            
            Debug.Log("\n=== END DEBUG ===");
        }
        
        private void LogTransformRecursive(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            string active = t.gameObject.activeSelf ? "" : " [INACTIVE]";
            Debug.Log($"{indent}{t.name}: LocalPos={t.localPosition}, Scale={t.localScale}{active}");
            
            foreach (Transform child in t)
            {
                LogTransformRecursive(child, depth + 1);
            }
        }
    }
}