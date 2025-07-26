using UnityEngine;

namespace MapleClient.GameView
{
    public class CameraController : MonoBehaviour
    {
        public Transform target;
        public float smoothSpeed = 0.125f;
        public Vector3 offset = new Vector3(0, 2, -10);
        
        // Map bounds
        private float mapLeft, mapRight, mapTop, mapBottom;
        private bool hasBounds = false;
        private Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            // Set orthographic camera for 2D
            cam.orthographic = true;
            cam.orthographicSize = 8f; // Larger view to see more of the map
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 desiredPosition = target.position + offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
                
                // Clamp camera position within map bounds
                if (hasBounds)
                {
                    smoothedPosition = ClampCameraPosition(smoothedPosition);
                }
                
                transform.position = smoothedPosition;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        public void SetMapBounds(float left, float right, float bottom, float top)
        {
            mapLeft = left;
            mapRight = right;
            mapBottom = bottom;
            mapTop = top;
            hasBounds = true;
        }
        
        private Vector3 ClampCameraPosition(Vector3 position)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            
            float clampedX = position.x;
            float clampedY = position.y;
            
            // Clamp X position
            if (mapRight - mapLeft > halfWidth * 2)
            {
                clampedX = Mathf.Clamp(position.x, mapLeft + halfWidth, mapRight - halfWidth);
            }
            else
            {
                // Map is narrower than camera view
                clampedX = (mapLeft + mapRight) / 2f;
            }
            
            // Clamp Y position
            if (mapTop - mapBottom > halfHeight * 2)
            {
                clampedY = Mathf.Clamp(position.y, mapBottom + halfHeight, mapTop - halfHeight);
            }
            else
            {
                // Map is shorter than camera view
                clampedY = (mapBottom + mapTop) / 2f;
            }
            
            return new Vector3(clampedX, clampedY, position.z);
        }
    }
}