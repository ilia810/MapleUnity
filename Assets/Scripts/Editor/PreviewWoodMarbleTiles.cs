using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class PreviewWoodMarbleTiles : EditorWindow
    {
        private string currentVariant = "bsc";
        private Vector2 scrollPos;
        
        [MenuItem("MapleUnity/Debug/Preview woodMarble Tiles")]
        public static void ShowWindow()
        {
            GetWindow<PreviewWoodMarbleTiles>("woodMarble Preview");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("woodMarble Tileset Preview", EditorStyles.boldLabel);
            
            currentVariant = EditorGUILayout.TextField("Variant:", currentVariant);
            
            if (GUILayout.Button("Load Tiles"))
            {
                LoadTiles();
            }
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            // Display loaded tiles
            var nxManager = NXDataManagerSingleton.Instance;
            if (nxManager != null)
            {
                int tilesPerRow = 5;
                int tileSize = 100;
                int currentTile = 0;
                
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 0; i < 20; i++) // Show first 20 tiles
                {
                    var sprite = nxManager.GetTileSprite("woodMarble", currentVariant, i);
                    if (sprite != null)
                    {
                        if (currentTile > 0 && currentTile % tilesPerRow == 0)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                        
                        EditorGUILayout.BeginVertical(GUILayout.Width(tileSize));
                        
                        // Draw sprite preview
                        Rect rect = GUILayoutUtility.GetRect(tileSize, tileSize);
                        EditorGUI.DrawTextureTransparent(rect, sprite.texture, ScaleMode.ScaleToFit);
                        
                        EditorGUILayout.LabelField($"{currentVariant}/{i}", EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.EndVertical();
                        
                        currentTile++;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void LoadTiles()
        {
            Debug.Log($"Loading woodMarble/{currentVariant} tiles...");
            Repaint();
        }
    }
}