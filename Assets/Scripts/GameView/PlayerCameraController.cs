using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>
    /// Simple camera controller that follows the player
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        private Transform playerTransform;
        private Camera mainCamera;
        private Vector3 offset = new Vector3(0, 0, -10);
        
        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
            }
        }
        
        public void SetPlayer(GameObject player)
        {
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        void LateUpdate()
        {
            if (playerTransform != null && mainCamera != null)
            {
                // Follow player position
                Vector3 targetPos = playerTransform.position + offset;
                mainCamera.transform.position = targetPos;
            }
        }
    }
}