using UnityEngine;
using MapleClient.GameLogic;
using System.Collections.Generic;
using System.Linq;
// Disambiguate Vector2 references
using UnityVector2 = UnityEngine.Vector2;
using MapleVector2 = MapleClient.GameLogic.Vector2;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView.Debugging
{
    /// <summary>
    /// Visual debugger for footholds in the scene
    /// </summary>
    public class FootholdDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        public bool showFootholds = true;
        public bool showConnections = true;
        public bool showIds = true;
        public bool showLayers = true;
        public float lineWidth = 0.1f;
        
        [Header("Colors")]
        public Color normalFootholdColor = Color.green;
        public Color wallFootholdColor = Color.red;
        public Color slipperyFootholdColor = Color.cyan;
        public Color conveyorFootholdColor = Color.yellow;
        public Color connectionColor = Color.blue;
        
        private IFootholdService footholdService;
        private List<Foothold> cachedFootholds = new List<Foothold>();
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
            RefreshFootholds();
        }
        
        public void SetFootholdService(IFootholdService service)
        {
            footholdService = service;
            RefreshFootholds();
        }
        
        public void RefreshFootholds()
        {
            if (footholdService == null)
            {
                // Try to find FootholdService from GameManager
                var gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    // Note: We'd need to expose the FootholdService from GameWorld
                    UnityEngine.Debug.LogWarning("FootholdDebugVisualizer: Cannot access FootholdService from GameManager");
                }
                return;
            }
            
            // Get all footholds
            cachedFootholds = footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToList();
            UnityEngine.Debug.Log($"FootholdDebugVisualizer: Cached {cachedFootholds.Count} footholds");
        }
        
        private void OnDrawGizmos()
        {
            if (!showFootholds || cachedFootholds == null || cachedFootholds.Count == 0)
                return;
            
            // Draw each foothold
            foreach (var foothold in cachedFootholds)
            {
                // Choose color based on foothold properties
                Color color = GetFootholdColor(foothold);
                Gizmos.color = color;
                
                // Convert MapleStory coordinates to Unity using proper conversion
                Vector3 start = CoordinateConverter.MSToUnityPosition(foothold.X1, foothold.Y1);
                Vector3 end = CoordinateConverter.MSToUnityPosition(foothold.X2, foothold.Y2);
                
                // Draw the foothold line
                Gizmos.DrawLine(start, end);
                
                // Draw thicker line for better visibility
                DrawThickLine(start, end, lineWidth, color);
                
                // Show connections
                if (showConnections)
                {
                    DrawConnections(foothold);
                }
            }
            
            // Draw labels (using OnGUI since Gizmos can't draw text)
            if (showIds || showLayers)
            {
                // Labels are drawn in OnGUI
            }
        }
        
        private void OnGUI()
        {
            if (!showFootholds || cachedFootholds == null || cachedFootholds.Count == 0)
                return;
            
            if (!showIds && !showLayers)
                return;
            
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            if (mainCamera == null)
                return;
            
            GUI.color = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            
            // Draw labels for each foothold
            foreach (var foothold in cachedFootholds)
            {
                // Calculate center position in MapleStory coords then convert
                float centerX = (foothold.X1 + foothold.X2) / 2f;
                float centerY = (foothold.Y1 + foothold.Y2) / 2f;
                Vector3 worldPos = CoordinateConverter.MSToUnityPosition(centerX, centerY);
                
                // Convert to screen position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                
                // Skip if behind camera
                if (screenPos.z < 0)
                    continue;
                
                // Flip Y coordinate for GUI
                screenPos.y = Screen.height - screenPos.y;
                
                // Build label text
                string label = "";
                if (showIds)
                    label += $"ID: {foothold.Id}";
                if (showLayers)
                {
                    if (label.Length > 0) label += "\n";
                    label += $"L: {foothold.Layer}";
                }
                
                // Draw label with background
                UnityVector2 size = style.CalcSize(new GUIContent(label));
                Rect rect = new Rect(screenPos.x - size.x / 2, screenPos.y - size.y / 2, size.x, size.y);
                
                // Draw background
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                
                // Draw text
                GUI.color = GetFootholdColor(foothold);
                GUI.Label(rect, label, style);
            }
            
            GUI.color = Color.white;
        }
        
        private Color GetFootholdColor(Foothold foothold)
        {
            if (foothold.IsWall)
                return wallFootholdColor;
            if (foothold.IsConveyor)
                return conveyorFootholdColor;
            if (foothold.IsSlippery)
                return slipperyFootholdColor;
            return normalFootholdColor;
        }
        
        private void DrawConnections(Foothold foothold)
        {
            if (footholdService == null)
                return;
            
            Gizmos.color = connectionColor;
            
            // Draw connection to next foothold
            if (foothold.NextId != 0)
            {
                var next = footholdService.GetConnectedFoothold(foothold, true);
                if (next != null)
                {
                    Vector3 fromPos = CoordinateConverter.MSToUnityPosition(foothold.X2, foothold.Y2);
                    Vector3 toPos = CoordinateConverter.MSToUnityPosition(next.X1, next.Y1);
                    Gizmos.DrawLine(fromPos, toPos);
                    
                    // Draw arrow
                    Vector3 dir = (toPos - fromPos).normalized;
                    Vector3 right = Quaternion.Euler(0, 0, -30) * dir * 0.2f;
                    Vector3 left = Quaternion.Euler(0, 0, 30) * dir * 0.2f;
                    Gizmos.DrawLine(toPos, toPos - right);
                    Gizmos.DrawLine(toPos, toPos - left);
                }
            }
        }
        
        private void DrawThickLine(Vector3 start, Vector3 end, float width, Color color)
        {
            // Calculate perpendicular direction
            Vector3 dir = (end - start).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0) * width * 0.5f;
            
            // Draw multiple parallel lines to create thickness
            int segments = 3;
            for (int i = 0; i < segments; i++)
            {
                float t = (i - (segments - 1) * 0.5f) / (segments - 1);
                Vector3 offset = perp * t;
                Gizmos.color = new Color(color.r, color.g, color.b, color.a * (1f - Mathf.Abs(t)));
                Gizmos.DrawLine(start + offset, end + offset);
            }
        }
        
        [ContextMenu("Refresh Footholds")]
        public void ForceRefresh()
        {
            RefreshFootholds();
        }
    }
}