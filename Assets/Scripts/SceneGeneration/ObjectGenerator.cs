using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameData;
using GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates map objects (decorations, interactive objects, etc.)
    /// </summary>
    public class ObjectGenerator
    {
        private NXDataManagerSingleton nxManager;
        
        public ObjectGenerator()
        {
            nxManager = NXDataManagerSingleton.Instance;
        }
        
        public GameObject GenerateObjects(List<ObjectData> objects, Transform parent)
        {
            GameObject objContainer = new GameObject("Objects");
            objContainer.transform.parent = parent;
            
            // Group objects by layer
            var layerGroups = new Dictionary<int, List<ObjectData>>();
            foreach (var obj in objects)
            {
                if (!layerGroups.ContainsKey(obj.Layer))
                    layerGroups[obj.Layer] = new List<ObjectData>();
                layerGroups[obj.Layer].Add(obj);
            }
            
            // Create objects for each layer
            foreach (var kvp in layerGroups)
            {
                GameObject layerContainer = new GameObject($"Layer_{kvp.Key}");
                layerContainer.transform.parent = objContainer.transform;
                
                foreach (var obj in kvp.Value)
                {
                    CreateObject(obj, layerContainer.transform);
                }
            }
            
            return objContainer;
        }
        
        private void CreateObject(ObjectData objData, Transform parent)
        {
            GameObject obj = new GameObject($"Object_{objData.ObjName}");
            obj.transform.parent = parent;
            
            // Check if this object should snap to footholds (ground-based objects)
            float adjustedY = objData.Y;
            bool shouldSnap = ShouldSnapToFoothold(objData);
            if (shouldSnap && FootholdManager.Instance != null)
            {
                float originalY = objData.Y;
                adjustedY = FootholdManager.Instance.GetYBelow(objData.X, objData.Y);
            }
            
            // Set position - keep all objects at Z=0 to prevent Z-fighting
            Vector3 position = CoordinateConverter.ToUnityPosition(objData.X, adjustedY, 0);
            obj.transform.position = position;
            
            // Add object component
            MapObject mapObj = obj.AddComponent<MapObject>();
            mapObj.objectSet = objData.ObjName;
            mapObj.l0 = objData.L0;
            mapObj.l1 = objData.L1;
            mapObj.l2 = objData.L2;
            mapObj.layer = objData.Layer;
            mapObj.zOrder = objData.Z;
            mapObj.zModifier = objData.ZM;
            
            // Flip if needed
            if (objData.F != 0)
            {
                obj.transform.localScale = new Vector3(-1, 1, 1);
            }
            
            // Create sprite object
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.parent = obj.transform;
            spriteObj.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            MapRenderOrder.SetOrder(renderer, MapRenderOrder.ObjectOrder(objData.Layer, objData.Z));
            
            // Load sprite
            LoadObjectSprite(objData, renderer);
            
            // Add special components based on object type
            AddObjectComponents(objData, obj);
        }
        
        private void LoadObjectSprite(ObjectData objData, SpriteRenderer renderer)
        {
            // Build sprite path
            string spritePath = $"Obj/{objData.ObjName}.img/{objData.L0}";
            if (!string.IsNullOrEmpty(objData.L1))
                spritePath += $"/{objData.L1}";
            if (!string.IsNullOrEmpty(objData.L2))
                spritePath += $"/{objData.L2}";
            
            // Load sprite from NX
            var (sprite, origin) = nxManager.GetObjectSpriteWithOrigin(spritePath);
            if (sprite != null)
            {
                renderer.sprite = sprite;
                
                // Use the EXACT same logic as tiles (which work correctly)
                float offsetX = -origin.x / 100f;  // Move left by origin.x
                float offsetY = origin.y / 100f;   // Move up by origin.y (inverted due to coordinate flip)
                
                renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
                
                // Debug logging
                float spriteHeight = sprite.texture.height;
                float spriteWidth = sprite.texture.width;
                
                // Calculate where the bottom of the sprite will be
                Vector3 parentWorldPos = renderer.transform.parent.parent.position; // sprite -> object -> position
                float spriteBottomY = parentWorldPos.y + offsetY - (spriteHeight / 100f);
                
            }
            else
            {
                Debug.LogWarning($"Object sprite not found: {spritePath}");
                // Create placeholder
                renderer.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            }
        }
        
        private void AddObjectComponents(ObjectData objData, GameObject obj)
        {
            // Check if object is interactive based on name patterns
            string objName = objData.ObjName.ToLower();
            
            if (objName.Contains("portal") || objName.Contains("potal"))
            {
                // Hidden portal object
                obj.AddComponent<HiddenPortal>();
            }
            else if (objName.Contains("ladder") || objName.Contains("rope"))
            {
                // Climbable object
                AddClimbableComponent(obj, objName.Contains("ladder"));
            }
            else if (objName.Contains("seat") || objName.Contains("chair") || objName.Contains("bench"))
            {
                // Sittable object
                obj.AddComponent<SeatObject>();
            }
            else if (objName.Contains("reactor"))
            {
                // Reactor (interactive object)
                obj.AddComponent<ReactorObject>();
            }
        }
        
        private bool ShouldSnapToFoothold(ObjectData objData)
        {
            // Based on C++ client analysis:
            // ONLY reactor objects snap to footholds
            // Regular decorative objects use their raw positions
            
            string objName = objData.ObjName.ToLower();
            
            // Only reactor-type objects should snap to footholds
            if (objName.Contains("reactor")) return true;
            
            // Some specific interactive objects that might be reactors
            // (these would need to be verified against actual map data)
            if (objName.Contains("chest") && objName.Contains("treasure")) return true;
            if (objName.Contains("lever") && objName.Contains("activate")) return true;
            
            // Everything else uses raw position (decorative objects like signs, flowers, etc.)
            return false;
        }
        
        private string GetOriginType(Vector2 origin, float width, float height)
        {
            float tolerance = 5f; // 5 pixel tolerance
            
            // Check common origin positions
            if (Mathf.Abs(origin.x) < tolerance && Mathf.Abs(origin.y) < tolerance)
                return "top-left";
            else if (Mathf.Abs(origin.x - width/2) < tolerance && Mathf.Abs(origin.y) < tolerance)
                return "top-center";
            else if (Mathf.Abs(origin.x - width) < tolerance && Mathf.Abs(origin.y) < tolerance)
                return "top-right";
            else if (Mathf.Abs(origin.x) < tolerance && Mathf.Abs(origin.y - height/2) < tolerance)
                return "middle-left";
            else if (Mathf.Abs(origin.x - width/2) < tolerance && Mathf.Abs(origin.y - height/2) < tolerance)
                return "center";
            else if (Mathf.Abs(origin.x - width) < tolerance && Mathf.Abs(origin.y - height/2) < tolerance)
                return "middle-right";
            else if (Mathf.Abs(origin.x) < tolerance && Mathf.Abs(origin.y - height) < tolerance)
                return "bottom-left";
            else if (Mathf.Abs(origin.x - width/2) < tolerance && Mathf.Abs(origin.y - height) < tolerance)
                return "bottom-center";
            else if (Mathf.Abs(origin.x - width) < tolerance && Mathf.Abs(origin.y - height) < tolerance)
                return "bottom-right";
            else
                return $"custom({origin.x},{origin.y})";
        }
        
        private void AddClimbableComponent(GameObject obj, bool isLadder)
        {
            ClimbableObject climbable = obj.AddComponent<ClimbableObject>();
            climbable.isLadder = isLadder;
            
            // Add trigger collider
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            
            // Get sprite bounds to set collider size
            SpriteRenderer renderer = obj.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                collider.size = renderer.sprite.bounds.size;
            }
            else
            {
                collider.size = new Vector2(0.5f, 3f); // Default size
            }
        }
        
        private string GetSortingLayer(int layer)
        {
            // Objects should use the Objects sorting layer
            return "Objects";
        }
        

    }
    
    /// <summary>
    /// Component for map objects
    /// </summary>

    
    /// <summary>
    /// Hidden portal activated by objects
    /// </summary>

    
    /// <summary>
    /// Climbable object (ladder/rope)
    /// </summary>

    
    /// <summary>
    /// Sittable object
    /// </summary>

    
    /// <summary>
    /// Reactor (interactive object)
    /// </summary>

}