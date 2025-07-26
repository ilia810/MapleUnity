using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates portal GameObjects from portal data
    /// </summary>
    public class PortalGenerator
    {
        // Portal types from MapleStory
        public enum PortalType
        {
            StartPoint = 0,      // Spawn point
            Invisible = 1,       // Invisible portal
            Visible = 2,         // Visible portal
            Collision = 3,       // Collision portal
            Changable = 4,       // Can be toggled
            ChangableInvisible = 5,
            TownPortalPoint = 6, // Town return point
            Script = 7,          // Script portal
            ScriptInvisible = 8,
            CollisionVerticalJump = 9,
            CollisionCustomImpact = 10,
            CollisionUnknownPcig = 11,
            ScriptHidden = 12
        }
        
        private const string PORTAL_PREFAB_PATH = "Prefabs/Portal";
        
        public GameObject GeneratePortals(List<Portal> portals, Transform parent)
        {
            GameObject portalContainer = new GameObject("Portals");
            portalContainer.transform.parent = parent;
            
            foreach (var portal in portals)
            {
                CreatePortal(portal, portalContainer.transform);
            }
            
            return portalContainer;
        }
        
        private void CreatePortal(Portal portal, Transform parent)
        {
            GameObject portalObj = new GameObject($"Portal_{portal.Id}_{portal.Name}");
            portalObj.transform.parent = parent;
            
            // Set position
            Vector3 position = CoordinateConverter.ToUnityPosition(portal.X, portal.Y, -1f); // Slightly in front
            portalObj.transform.position = position;
            
            // Add portal component
            PortalBehavior behavior = portalObj.AddComponent<PortalBehavior>();
            behavior.portalId = portal.Id;
            behavior.portalName = portal.Name;
            behavior.portalType = (PortalType)portal.Type;
            behavior.targetMapId = portal.TargetMap;
            behavior.targetPortalName = portal.TargetName;
            
            // Add appropriate components based on portal type
            switch ((PortalType)portal.Type)
            {
                case PortalType.StartPoint:
                case PortalType.TownPortalPoint:
                    // Spawn points - just markers, no collision
                    // Mark as spawn point without using tags
                    behavior.isSpawnPoint = true;
                    AddSpawnPointVisual(portalObj);
                    break;
                    
                case PortalType.Visible:
                case PortalType.Changable:
                    // Visible portals with collision
                    AddPortalCollider(portalObj);
                    AddPortalVisual(portalObj, true);
                    break;
                    
                case PortalType.Invisible:
                case PortalType.ChangableInvisible:
                case PortalType.ScriptInvisible:
                    // Invisible portals with collision
                    AddPortalCollider(portalObj);
                    AddPortalVisual(portalObj, false);
                    break;
                    
                case PortalType.Script:
                case PortalType.ScriptHidden:
                    // Script portals
                    AddPortalCollider(portalObj);
                    AddScriptPortalComponent(portalObj);
                    AddPortalVisual(portalObj, portal.Type == (int)PortalType.Script);
                    break;
                    
                case PortalType.Collision:
                case PortalType.CollisionVerticalJump:
                case PortalType.CollisionCustomImpact:
                case PortalType.CollisionUnknownPcig:
                    // Special collision portals
                    AddSpecialCollider(portalObj, (PortalType)portal.Type);
                    break;
            }
        }
        
        private void AddPortalCollider(GameObject portalObj)
        {
            BoxCollider2D collider = portalObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 1f); // Adjust size as needed
            collider.isTrigger = true;
        }
        
        private void AddSpecialCollider(GameObject portalObj, PortalType type)
        {
            BoxCollider2D collider = portalObj.AddComponent<BoxCollider2D>();
            
            switch (type)
            {
                case PortalType.CollisionVerticalJump:
                    collider.size = new Vector2(0.3f, 2f); // Tall and narrow
                    break;
                default:
                    collider.size = new Vector2(0.5f, 1f);
                    break;
            }
            
            collider.isTrigger = true;
        }
        
        private void AddPortalVisual(GameObject portalObj, bool visible)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.parent = portalObj.transform;
            visual.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Objects";
            renderer.sortingOrder = 10;
            
            if (visible)
            {
                // Load portal sprite from NX data
                // For now, use a placeholder
                renderer.color = new Color(0, 0.5f, 1f, 0.7f);
                
                // Create simple portal visual
                GameObject innerGlow = new GameObject("InnerGlow");
                innerGlow.transform.parent = visual.transform;
                innerGlow.transform.localPosition = Vector3.zero;
                innerGlow.transform.localScale = new Vector3(0.5f, 1f, 1f);
                
                // Destroy the MeshRenderer if it exists from CreatePrimitive
                MeshRenderer meshRenderer = innerGlow.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    Object.DestroyImmediate(meshRenderer);
                
                SpriteRenderer glowRenderer = innerGlow.AddComponent<SpriteRenderer>();
                glowRenderer.color = new Color(0.5f, 0.8f, 1f, 0.5f);
                glowRenderer.sortingLayerName = "Objects";
                glowRenderer.sortingOrder = 9;
            }
            else
            {
                // Invisible portal - only show in editor
                renderer.color = new Color(1f, 0, 0, 0.3f);
                visual.SetActive(false); // Hide in game
                
                #if UNITY_EDITOR
                visual.SetActive(true); // Show in editor for debugging
                #endif
            }
        }
        
        private void AddSpawnPointVisual(GameObject portalObj)
        {
            #if UNITY_EDITOR
            // Add gizmo component for editor visualization
            portalObj.AddComponent<SpawnPointGizmo>();
            #endif
        }
        
        private void AddScriptPortalComponent(GameObject portalObj)
        {
            // Add component to handle script execution
            portalObj.AddComponent<ScriptPortalHandler>();
        }
    }
    
    /// <summary>
    /// Portal behavior component
    /// </summary>
    public class PortalBehavior : MonoBehaviour
    {
        public int portalId;
        public string portalName;
        public PortalGenerator.PortalType portalType;
        public int targetMapId;
        public string targetPortalName;
        public bool isSpawnPoint = false;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                HandlePortalEnter(other.gameObject);
            }
        }
        
        private void HandlePortalEnter(GameObject player)
        {
            // Handle different portal types
            switch (portalType)
            {
                case PortalGenerator.PortalType.Visible:
                case PortalGenerator.PortalType.Invisible:
                case PortalGenerator.PortalType.Changable:
                case PortalGenerator.PortalType.ChangableInvisible:
                    // Teleport to target map
                    Debug.Log($"Portal {portalName} activated - Target: Map {targetMapId}, Portal {targetPortalName}");
                    // TODO: Implement map transition
                    break;
                    
                case PortalGenerator.PortalType.Script:
                case PortalGenerator.PortalType.ScriptInvisible:
                case PortalGenerator.PortalType.ScriptHidden:
                    // Execute portal script
                    var scriptHandler = GetComponent<ScriptPortalHandler>();
                    if (scriptHandler != null)
                    {
                        scriptHandler.ExecuteScript(player);
                    }
                    break;
            }
        }
    }
    
    /// <summary>
    /// Handles script portal execution
    /// </summary>
    public class ScriptPortalHandler : MonoBehaviour
    {
        public void ExecuteScript(GameObject player)
        {
            // TODO: Implement script execution
            Debug.Log($"Executing script for portal: {gameObject.name}");
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Gizmo for spawn point visualization in editor
    /// </summary>
    public class SpawnPointGizmo : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, transform.position + Vector3.down * 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.3f, transform.position + Vector3.right * 0.3f);
        }
    }
    #endif
}