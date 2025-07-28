using UnityEngine;
using UnityEditor;
using System.Linq;
using MapleClient.GameView;

namespace MapleClient.Editor
{
    /// <summary>
    /// Editor window to debug rendering issues and monitor sorting layers
    /// </summary>
    public class RenderingDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private float refreshRate = 0.5f;
        private float lastRefreshTime;
        
        [MenuItem("MapleUnity/Rendering Debug")]
        public static void ShowWindow()
        {
            GetWindow<RenderingDebugWindow>("Rendering Debug");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Rendering Debug Monitor", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Camera info
            EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                EditorGUILayout.LabelField("Orthographic Size:", mainCamera.orthographicSize.ToString("F2"));
                EditorGUILayout.LabelField("Background Color:", mainCamera.backgroundColor.ToString());
                EditorGUILayout.LabelField("Position:", mainCamera.transform.position.ToString());
                
                if (GUILayout.Button("Apply MapleStory Camera Settings"))
                {
                    RenderingConfiguration.ConfigureCamera(mainCamera);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No main camera found", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Sorting layers info
            EditorGUILayout.LabelField("Sorting Layers", EditorStyles.boldLabel);
            if (GUILayout.Button("Log Sorting Layer Info"))
            {
                RenderingConfiguration.LogSortingLayerInfo();
            }
            
            EditorGUILayout.Space();
            
            // Renderer statistics
            EditorGUILayout.LabelField("Active Renderers", EditorStyles.boldLabel);
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            if (autoRefresh)
            {
                refreshRate = EditorGUILayout.Slider("Refresh Rate", refreshRate, 0.1f, 2f);
            }
            
            if (GUILayout.Button("Refresh Now") || (autoRefresh && Time.realtimeSinceStartup - lastRefreshTime > refreshRate))
            {
                lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
            
            EditorGUILayout.Space();
            
            // Show renderer info
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            ShowRendererInfo();
            EditorGUILayout.EndScrollView();
        }
        
        private void ShowRendererInfo()
        {
            var renderers = FindObjectsOfType<SpriteRenderer>()
                .OrderBy(r => r.sortingLayerName)
                .ThenBy(r => r.sortingOrder)
                .ToList();
            
            EditorGUILayout.LabelField($"Total Sprite Renderers: {renderers.Count}");
            
            // Group by sorting layer
            var layerGroups = renderers.GroupBy(r => r.sortingLayerName);
            
            foreach (var group in layerGroups)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{group.Key} ({group.Count()} renderers)", EditorStyles.boldLabel);
                
                // Show some samples from this layer
                int shown = 0;
                foreach (var renderer in group.OrderBy(r => r.sortingOrder))
                {
                    if (shown >= 5)
                    {
                        EditorGUILayout.LabelField($"... and {group.Count() - shown} more");
                        break;
                    }
                    
                    string info = $"  {renderer.gameObject.name}: Order {renderer.sortingOrder}";
                    if (renderer.transform.position.z != 0)
                    {
                        info += $" (Z: {renderer.transform.position.z:F2})";
                    }
                    
                    EditorGUILayout.LabelField(info);
                    shown++;
                }
            }
            
            // Check for potential issues
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Potential Issues", EditorStyles.boldLabel);
            
            var defaultLayerRenderers = renderers.Where(r => r.sortingLayerName == "Default").ToList();
            if (defaultLayerRenderers.Count > 0)
            {
                EditorGUILayout.HelpBox($"{defaultLayerRenderers.Count} renderers using Default layer - may cause sorting issues", MessageType.Warning);
            }
            
            var nonZeroZ = renderers.Where(r => r.transform.position.z != 0).ToList();
            if (nonZeroZ.Count > 0)
            {
                EditorGUILayout.HelpBox($"{nonZeroZ.Count} renderers with non-zero Z position - may cause Z-fighting", MessageType.Warning);
            }
        }
        
        private void Update()
        {
            if (autoRefresh && EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
    }
}