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
            
            // Set position with Z ordering
            Vector3 position = CoordinateConverter.ToUnityPosition(objData.X, objData.Y, 0);
            
            // Use Z value for depth sorting within the layer
            float zOrder = -objData.Z * 0.01f; // Negative so higher Z values appear behind
            position.z = zOrder;
            
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
            renderer.sortingLayerName = GetSortingLayer(objData.Layer);
            renderer.sortingOrder = CalculateSortingOrder(objData);
            
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
                
                // Apply origin offset just like tiles do
                // C++ client: draws at pos - origin
                // With top-left pivot, we need to consider coordinate system differences:
                // - MapleStory: Y+ is down, origin from top-left, subtract origin = move up+left
                // - Unity: Y+ is up, pivot at top-left
                // Since Y is already inverted by CoordinateConverter, origin.y behavior is inverted too
                float offsetX = -origin.x / 100f;  // Move left by origin.x
                float offsetY = origin.y / 100f;   // Move up by origin.y (inverted due to coordinate flip)
                
                renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
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
            // Map layer numbers to sorting layers
            switch (layer)
            {
                case 0:
                case 1:
                case 2:
                    return "Background";
                case 3:
                case 4:
                case 5:
                    return "Objects";
                case 6:
                case 7:
                    return "Foreground";
                default:
                    return "Objects";
            }
        }
        
        private int CalculateSortingOrder(ObjectData objData)
        {
            // Calculate sorting order based on layer and Z value
            int baseOrder = objData.Layer * 1000;
            int zOrder = objData.Z + objData.ZM;
            return baseOrder + zOrder;
        }
    }
    
    /// <summary>
    /// Component for map objects
    /// </summary>
    public class MapObject : MonoBehaviour
    {
        public string objectSet;
        public string l0;
        public string l1;
        public string l2;
        public int layer;
        public int zOrder;
        public int zModifier;
    }
    
    /// <summary>
    /// Hidden portal activated by objects
    /// </summary>
    public class HiddenPortal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Activate hidden portal
                Debug.Log("Hidden portal activated!");
            }
        }
    }
    
    /// <summary>
    /// Climbable object (ladder/rope)
    /// </summary>
    public class ClimbableObject : MonoBehaviour
    {
        public bool isLadder = true;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Enable climbing
                Debug.Log($"Player can climb {(isLadder ? "ladder" : "rope")}");
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Disable climbing
            }
        }
    }
    
    /// <summary>
    /// Sittable object
    /// </summary>
    public class SeatObject : MonoBehaviour
    {
        private bool isOccupied = false;
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isOccupied && Input.GetKeyDown(KeyCode.DownArrow))
            {
                // TODO: Make player sit
                isOccupied = true;
                Debug.Log("Player sits down");
            }
        }
    }
    
    /// <summary>
    /// Reactor (interactive object)
    /// </summary>
    public class ReactorObject : MonoBehaviour
    {
        public int reactorId;
        public bool isActivated = false;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isActivated)
            {
                // TODO: Activate reactor
                isActivated = true;
                Debug.Log("Reactor activated!");
            }
        }
    }
}