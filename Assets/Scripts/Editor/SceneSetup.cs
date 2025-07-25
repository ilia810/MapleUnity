using UnityEngine;
using UnityEditor;
using MapleClient.GameView;

public class SceneSetup : EditorWindow
{
    [MenuItem("MapleClient/Setup Scene")]
    public static void SetupScene()
    {
        // Find or create GameManager
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            Debug.Log("Created GameManager GameObject");
        }
        else if (gameManager.GetComponent<GameManager>() == null)
        {
            gameManager.AddComponent<GameManager>();
            Debug.Log("Added GameManager component");
        }
        else
        {
            Debug.Log("GameManager already exists");
        }

        // Setup Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                cameraObj.tag = "MainCamera";
            }
            else
            {
                mainCamera = cameraObj.GetComponent<Camera>();
            }
        }

        // Configure camera for 2D
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5;
        mainCamera.transform.position = new Vector3(0, 0, -10);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.5f, 0.8f, 1f); // Sky blue

        // Add CameraController if missing
        if (mainCamera.GetComponent<CameraController>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraController>();
            Debug.Log("Added CameraController to Main Camera");
        }

        // Create UI Canvas for inventory
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("Created UI Canvas");
        }

        // Create EventSystem if missing
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created EventSystem");
        }

        // Mark scene as dirty to save changes
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("Scene setup complete! You can now play the scene.");
        Debug.Log("Controls: Arrow Keys = Move, Space = Jump, Z = Attack");
    }

    [MenuItem("MapleClient/Create Test Monster")]
    public static void CreateTestMonster()
    {
        GameObject monster = new GameObject("Test Monster");
        monster.transform.position = new Vector3(2, 0, 0);
        
        // Add a sprite renderer with a colored square as placeholder
        SpriteRenderer sr = monster.AddComponent<SpriteRenderer>();
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.red;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
        
        Debug.Log("Created test monster at position (2, 0). It will spawn when you play the scene.");
    }
}