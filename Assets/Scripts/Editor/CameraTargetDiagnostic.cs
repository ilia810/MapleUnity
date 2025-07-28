using UnityEngine;
using UnityEditor;
using MapleClient.GameView;

namespace MapleClient.Editor
{
    public class CameraTargetDiagnostic : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Diagnose Camera Target")]
        static void Init()
        {
            GetWindow<CameraTargetDiagnostic>("Camera Target Diagnostic");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Camera Target Diagnostic", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Check Camera Target"))
            {
                CheckCameraTarget();
            }

            if (GUILayout.Button("Find '12345678' GameObjects"))
            {
                Find12345678GameObjects();
            }

            if (GUILayout.Button("List All Scene GameObjects"))
            {
                ListAllGameObjects();
            }
        }

        void CheckCameraTarget()
        {
            Debug.Log("=== Camera Target Diagnostic ===");
            
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found!");
                return;
            }

            Debug.Log($"Main Camera: {camera.name} at position {camera.transform.position}");
            
            // Check CameraController
            var cameraController = camera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                if (cameraController.target != null)
                {
                    Debug.Log($"CameraController target: {GetPath(cameraController.target.gameObject)}");
                    Debug.Log($"Target position: {cameraController.target.position}");
                    Debug.Log($"Target name: '{cameraController.target.name}'");
                    
                    // Check if target has any text components
                    CheckForTextComponents(cameraController.target.gameObject);
                }
                else
                {
                    Debug.LogWarning("CameraController has null target!");
                }
            }
            else
            {
                Debug.LogWarning("No CameraController found on main camera!");
            }

            // Check PlayerCameraController
            var playerCameraController = camera.GetComponent<PlayerCameraController>();
            if (playerCameraController != null)
            {
                Debug.Log("PlayerCameraController found");
            }
        }

        void Find12345678GameObjects()
        {
            Debug.Log("=== Finding '12345678' GameObjects ===");
            
            var allObjects = FindObjectsOfType<GameObject>();
            bool found = false;
            
            foreach (var obj in allObjects)
            {
                // Check name
                if (obj.name.Contains("12345678"))
                {
                    Debug.LogWarning($"Found GameObject with '12345678' in name: {GetPath(obj)}", obj);
                    found = true;
                }
                
                // Check for text components
                CheckForTextComponents(obj);
            }
            
            if (!found)
            {
                Debug.Log("No GameObjects found with '12345678' in name");
            }
        }

        void CheckForTextComponents(GameObject obj)
        {
            // Check UI Text
            var uiText = obj.GetComponent<UnityEngine.UI.Text>();
            if (uiText != null && uiText.text.Contains("12345678"))
            {
                Debug.LogWarning($"Found UI Text with '12345678': {GetPath(obj)} - Text: '{uiText.text}'", obj);
            }
            
            // Check TextMesh
            var textMesh = obj.GetComponent<TextMesh>();
            if (textMesh != null && textMesh.text.Contains("12345678"))
            {
                Debug.LogWarning($"Found TextMesh with '12345678': {GetPath(obj)} - Text: '{textMesh.text}'", obj);
            }
            
            // Check all children recursively
            foreach (Transform child in obj.transform)
            {
                CheckForTextComponents(child.gameObject);
            }
        }

        void ListAllGameObjects()
        {
            Debug.Log("=== All Scene GameObjects ===");
            
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                LogGameObjectHierarchy(rootObj, 0);
            }
        }

        void LogGameObjectHierarchy(GameObject obj, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}{obj.name} (active: {obj.activeInHierarchy}, pos: {obj.transform.position})");
            
            // Check if this object has interesting components
            var components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is SpriteRenderer || comp is UnityEngine.UI.Text || comp is TextMesh)
                {
                    Debug.Log($"{indent}  - {comp.GetType().Name}");
                }
            }
            
            foreach (Transform child in obj.transform)
            {
                LogGameObjectHierarchy(child.gameObject, depth + 1);
            }
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