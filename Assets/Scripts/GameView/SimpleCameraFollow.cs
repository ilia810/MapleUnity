using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>
    /// Simple camera follow script that actually works
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 0, -10);
        public float smoothSpeed = 5f;
        
        void Start()
        {
            // Ensure we have proper orthographic settings
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
            }
        }
        
        void LateUpdate()
        {
            if (target != null)
            {
                // Direct positioning - no smoothing to avoid oscillation
                Vector3 desiredPosition = target.position + offset;
                transform.position = desiredPosition;
            }
        }
    }
}