using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MapleClient.Editor
{
    public class Find12345678Source : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Find 12345678 Source")]
        static void Init()
        {
            GetWindow<Find12345678Source>("Find 12345678");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Find '12345678' Source", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Check All Sprites in Scene"))
            {
                CheckAllSprites();
            }

            if (GUILayout.Button("Check All Materials"))
            {
                CheckAllMaterials();
            }

            if (GUILayout.Button("Check All Textures in Resources"))
            {
                CheckAllTextures();
            }

            if (GUILayout.Button("Create Debug Sprite"))
            {
                CreateDebugSprite();
            }
        }

        void CheckAllSprites()
        {
            Debug.Log("=== Checking All Sprites in Scene ===");
            
            var spriteRenderers = FindObjectsOfType<SpriteRenderer>();
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null && sr.sprite.texture != null)
                {
                    var tex = sr.sprite.texture;
                    
                    // Check if texture name contains numbers
                    if (tex.name.Contains("1234") || tex.name.Contains("5678") || tex.name.Contains("12345678"))
                    {
                        Debug.LogWarning($"Found suspect texture name: {tex.name} on {GetPath(sr.gameObject)}", sr.gameObject);
                    }
                    
                    // Check if it's a procedural texture with text
                    if (tex.width == 128 && tex.height == 32) // Common size for text
                    {
                        Debug.LogWarning($"Found text-sized texture: {tex.name} ({tex.width}x{tex.height}) on {GetPath(sr.gameObject)}", sr.gameObject);
                    }
                }
            }
            
            Debug.Log("=== Sprite Check Complete ===");
        }

        void CheckAllMaterials()
        {
            Debug.Log("=== Checking All Materials ===");
            
            var renderers = FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null)
                {
                    var mat = r.sharedMaterial;
                    if (mat.name.Contains("1234") || mat.name.Contains("5678"))
                    {
                        Debug.LogWarning($"Found suspect material: {mat.name} on {GetPath(r.gameObject)}", r.gameObject);
                    }
                    
                    // Check main texture
                    if (mat.mainTexture != null && mat.mainTexture.name.Contains("1234"))
                    {
                        Debug.LogWarning($"Found suspect texture in material: {mat.mainTexture.name} on {GetPath(r.gameObject)}", r.gameObject);
                    }
                }
            }
            
            Debug.Log("=== Material Check Complete ===");
        }

        void CheckAllTextures()
        {
            Debug.Log("=== Checking All Loaded Textures ===");
            
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (var tex in textures)
            {
                if (tex.name.Contains("1234") || tex.name.Contains("5678") || tex.name.Contains("12345678"))
                {
                    Debug.LogWarning($"Found suspect texture in memory: {tex.name} ({tex.width}x{tex.height})");
                    
                    // Try to find where it's used
                    var sprites = Resources.FindObjectsOfTypeAll<Sprite>().Where(s => s.texture == tex);
                    foreach (var sprite in sprites)
                    {
                        Debug.Log($"  - Used by sprite: {sprite.name}");
                    }
                }
            }
            
            Debug.Log($"Checked {textures.Length} textures");
        }

        void CreateDebugSprite()
        {
            // Create a sprite with "TEST" text to see if it appears similar
            var tex = new Texture2D(128, 32);
            var colors = new Color[128 * 32];
            
            // Fill with white
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            
            // Draw simple "TEST" text in black
            for (int y = 8; y < 24; y++)
            {
                for (int x = 10; x < 118; x++)
                {
                    if ((x - 10) % 28 < 4) // Simple vertical lines for letters
                    {
                        colors[y * 128 + x] = Color.black;
                    }
                }
            }
            
            tex.SetPixels(colors);
            tex.Apply();
            
            var sprite = Sprite.Create(tex, new Rect(0, 0, 128, 32), new Vector2(0.5f, 0.5f), 100f);
            
            // Create GameObject with this sprite
            var go = new GameObject("DebugTextSprite");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100;
            
            Debug.Log("Created debug sprite at origin");
        }

        string GetPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}