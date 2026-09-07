using MapleClient.GameView;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    [DefaultExecutionOrder(900)]
    public class CameraBounds : MonoBehaviour
    {
        [SerializeField] private Bounds bounds;
        private Camera mainCamera;
        private GameManager gameManager;

        private void OnEnable()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null) gameManager.PlayerViewRepositioned += Apply;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.PlayerViewRepositioned -= Apply;
        }
        
        private void Start()
        {
            mainCamera = Camera.main;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                var info = FindObjectOfType<MapInfo>();
                if (info != null) bounds = info.vrBounds;
            }
        }
        
        public void SetBounds(Bounds newBounds)
        {
            bounds = newBounds;
            Apply();
        }
        
        private void LateUpdate() => Apply();

        public void Apply()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || bounds.size.x <= 0 || bounds.size.y <= 0) return;
            
            // Constrain camera position to bounds
            Vector3 pos = mainCamera.transform.position;
            
            // Calculate camera extents
            float height = mainCamera.orthographicSize * 2;
            float width = height * mainCamera.aspect;
            
            // Clamp position
            pos.x = bounds.size.x <= width ? bounds.center.x : Mathf.Clamp(pos.x, bounds.min.x + width / 2, bounds.max.x - width / 2);
            pos.y = bounds.size.y <= height ? bounds.center.y : Mathf.Clamp(pos.y, bounds.min.y + height / 2, bounds.max.y - height / 2);
            
            mainCamera.transform.position = pos;
            if (Application.isPlaying)
                (mainCamera.GetComponent<ClassicInteriorMask>() ?? mainCamera.gameObject.AddComponent<ClassicInteriorMask>()).SetBounds(bounds);
        }
    }
}
