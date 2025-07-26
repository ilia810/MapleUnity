using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MapleClient.Editor
{
    public class SetupSortingLayers
    {
        [MenuItem("MapleUnity/Setup/Configure Sorting Layers")]
        public static void ConfigureSortingLayers()
        {
            Debug.Log("=== Configuring Sorting Layers ===");
            
            // Define the sorting layers in order (back to front)
            string[] desiredLayers = new string[]
            {
                "Default",      // Keep default
                "Background",   // Far backgrounds (sky, etc.)
                "Tiles",        // Ground tiles
                "Objects",      // Buildings, decorations
                "NPCs",         // NPCs and monsters
                "Effects",      // Particle effects
                "Foreground",   // Foreground elements
                "UI"           // UI elements
            };
            
            // Access the TagManager asset
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            
            if (sortingLayers != null)
            {
                // Clear existing layers (except Default which is at index 0)
                while (sortingLayers.arraySize > 1)
                {
                    sortingLayers.DeleteArrayElementAtIndex(sortingLayers.arraySize - 1);
                }
                
                // Add our layers
                for (int i = 1; i < desiredLayers.Length; i++)
                {
                    sortingLayers.InsertArrayElementAtIndex(i);
                    SerializedProperty newLayer = sortingLayers.GetArrayElementAtIndex(i);
                    
                    SerializedProperty name = newLayer.FindPropertyRelative("name");
                    SerializedProperty uniqueID = newLayer.FindPropertyRelative("uniqueID");
                    
                    if (name != null && uniqueID != null)
                    {
                        name.stringValue = desiredLayers[i];
                        uniqueID.intValue = i * 100; // Give each layer a unique ID
                    }
                }
                
                tagManager.ApplyModifiedProperties();
                
                Debug.Log("Sorting layers configured successfully!");
                
                // List the layers
                Debug.Log("Sorting layers (back to front):");
                for (int i = 0; i < desiredLayers.Length; i++)
                {
                    Debug.Log($"  {i}: {desiredLayers[i]}");
                }
            }
            else
            {
                Debug.LogError("Could not find m_SortingLayers property!");
            }
        }
        
        [MenuItem("MapleUnity/Setup/List Current Sorting Layers")]
        public static void ListSortingLayers()
        {
            Debug.Log("=== Current Sorting Layers ===");
            
            foreach (var layer in SortingLayer.layers)
            {
                Debug.Log($"  ID: {layer.id}, Name: {layer.name}, Value: {layer.value}");
            }
        }
    }
}