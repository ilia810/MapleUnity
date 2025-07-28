using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;

namespace MapleClient.GameView
{
    /// <summary>
    /// Enhanced camera follow script with special movement handling
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour, IPlayerViewListener
    {
        [Header("Basic Settings")]
        public Transform target;
        public Vector3 offset = new Vector3(0, 0, -10);
        public float smoothSpeed = 5f;
        
        [Header("Advanced Settings")]
        public bool enableSmoothing = true;
        public float flashJumpSmoothSpeed = 2f; // Slower for flash jump
        public float climbingSmoothSpeed = 3f; // Medium speed for climbing
        public bool enableLookahead = true;
        public float lookaheadAmount = 1f;
        public float lookaheadSmoothing = 0.5f;
        
        [Header("Camera Bounds")]
        public bool useCameraBounds = false; // Disabled temporarily to fix camera follow issue
        public float minX = -100f;
        public float maxX = 100f;
        public float minY = -50f;
        public float maxY = 50f;
        
        [Header("Debug")]
        public bool debugMode = false;
        
        // State tracking
        private PlayerState currentPlayerState = PlayerState.Standing;
        private Vector3 currentLookahead = Vector3.zero;
        private Vector3 targetLookahead = Vector3.zero;
        private Vector3 lastTargetPosition;
        private float currentSmoothSpeed;
        
        // References
        private GameWorld gameWorld;
        private Player player;
        private Camera cam;
        
        void Start()
        {
            // Ensure we have proper orthographic settings
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f; // Show more of the scene (600 pixels height / 100)
            }
            
            currentSmoothSpeed = smoothSpeed;
            
            // Find GameWorld and register as listener
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var fieldInfo = typeof(GameManager).GetField("gameWorld", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    gameWorld = fieldInfo.GetValue(gameManager) as GameWorld;
                    if (gameWorld != null)
                    {
                        player = gameWorld.Player;
                        if (player != null)
                        {
                            player.AddViewListener(this);
                        }
                    }
                }
            }
            
            // If target wasn't set, try to find the player
            if (target == null)
            {
                var playerController = FindObjectOfType<SimplePlayerController>();
                if (playerController != null)
                {
                    target = playerController.transform;
                    Debug.Log($"[SimpleCameraFollow] Found player controller, setting as target: {target.name}");
                }
                else
                {
                    Debug.LogWarning("[SimpleCameraFollow] No target set and couldn't find player controller!");
                }
            }
            
            if (target != null)
            {
                lastTargetPosition = target.position;
                Vector3 initialPosition = target.position + offset;
                transform.position = initialPosition;
                currentLookahead = Vector3.zero; // Reset lookahead
                targetLookahead = Vector3.zero;
                Debug.Log($"[SimpleCameraFollow] Camera initialized at position: {transform.position}, following target: {target.name} at {target.position}");
                
                // Force immediate update to ensure camera is in correct position
                UpdateCameraPosition();
            }
            else
            {
                Debug.LogError("[SimpleCameraFollow] No target found! Camera will not follow.");
            }
        }
        
        void LateUpdate()
        {
            if (target != null)
            {
                UpdateCameraPosition();
            }
            else if (Time.frameCount % 60 == 0) // Log every second
            {
                Debug.LogWarning("[SimpleCameraFollow] No target in LateUpdate! Trying to find player...");
                var playerController = FindObjectOfType<SimplePlayerController>();
                if (playerController != null)
                {
                    target = playerController.transform;
                    Debug.Log($"[SimpleCameraFollow] Found player controller in LateUpdate: {target.name}");
                }
            }
        }
        
        private void UpdateCameraPosition()
        {
            // Calculate lookahead based on velocity
            if (enableLookahead && player != null)
            {
                targetLookahead = new Vector3(player.Velocity.X * lookaheadAmount * 0.1f, 
                                            player.Velocity.Y * lookaheadAmount * 0.05f, 0);
                currentLookahead = Vector3.Lerp(currentLookahead, targetLookahead, 
                                              lookaheadSmoothing * Time.deltaTime);
            }
            
            // Calculate desired position
            Vector3 desiredPosition = target.position + offset + currentLookahead;
            Vector3 originalDesired = desiredPosition; // Store for debug
            
            // Apply small deadzone to reduce jitter when standing still
            Vector3 currentPos = transform.position;
            float distance = Vector3.Distance(new Vector3(desiredPosition.x, desiredPosition.y, currentPos.z), 
                                            new Vector3(currentPos.x, currentPos.y, currentPos.z));
            if (distance < 0.01f && player != null && Mathf.Abs(player.Velocity.X) < 0.1f && Mathf.Abs(player.Velocity.Y) < 0.1f)
            {
                return; // Don't update if very close and player is still
            }
            
            // Apply camera bounds if enabled
            if (useCameraBounds && gameWorld?.CurrentMap != null)
            {
                // Use map bounds if available
                var mapData = gameWorld.CurrentMap;
                if (mapData.Width > 0 && mapData.Height > 0)
                {
                    float mapMinX = 0f;
                    float mapMaxX = mapData.Width / 100f;
                    float mapMinY = -mapData.Height / 100f;  // Negative because Y goes down in MapleStory
                    float mapMaxY = 0f;
                    
                    desiredPosition.x = Mathf.Clamp(desiredPosition.x, mapMinX + cam.orthographicSize * cam.aspect, 
                                                  mapMaxX - cam.orthographicSize * cam.aspect);
                    desiredPosition.y = Mathf.Clamp(desiredPosition.y, mapMinY + cam.orthographicSize, 
                                                  mapMaxY - cam.orthographicSize);
                }
                else
                {
                    // Use manual bounds
                    desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                    desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
                }
            }
            
            // Debug logging for camera issues
            if (debugMode && Time.frameCount % 60 == 0) // Log every second when debug is on
            {
                Debug.Log($"[SimpleCameraFollow] Target: {target.position}, Desired: {originalDesired}, Clamped: {desiredPosition}, Current: {transform.position}");
                if (useCameraBounds && gameWorld?.CurrentMap != null)
                {
                    var mapData = gameWorld.CurrentMap;
                    Debug.Log($"[SimpleCameraFollow] Map bounds - Width: {mapData.Width}, Height: {mapData.Height}");
                }
            }
            
            // Check if camera is very far from target (more than 2 units)
            float distanceToTarget = Vector3.Distance(transform.position, desiredPosition);
            if (distanceToTarget > 2f)
            {
                // Snap to target if too far away
                transform.position = desiredPosition;
                if (debugMode)
                {
                    Debug.Log($"[SimpleCameraFollow] Camera was too far ({distanceToTarget:F2}), snapping to position: {desiredPosition}");
                }
            }
            else if (enableSmoothing && currentSmoothSpeed > 0)
            {
                // Apply smoothing based on state
                // Use unscaled delta time for consistent smoothing
                float smoothFactor = 1f - Mathf.Exp(-currentSmoothSpeed * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);
            }
            else
            {
                transform.position = desiredPosition;
            }
            
            lastTargetPosition = target.position;
        }
        
        // IPlayerViewListener implementation
        public void OnPositionChanged(MapleClient.GameLogic.Vector2 position)
        {
            // Handle teleport detection for Flash Jump
            if (lastTargetPosition != Vector3.zero && target != null)
            {
                float distance = Vector3.Distance(new Vector3(position.X, position.Y, 0), lastTargetPosition);
                if (distance > 1f && currentPlayerState == PlayerState.FlashJumping)
                {
                    // Instant camera move for teleport
                    transform.position = new Vector3(position.X, position.Y, 0) + offset;
                }
            }
        }
        
        public void OnStateChanged(PlayerState state)
        {
            currentPlayerState = state;
            
            // Adjust camera smoothing based on state
            switch (state)
            {
                case PlayerState.FlashJumping:
                    currentSmoothSpeed = flashJumpSmoothSpeed;
                    break;
                case PlayerState.Climbing:
                    currentSmoothSpeed = climbingSmoothSpeed;
                    break;
                case PlayerState.Jumping:
                case PlayerState.DoubleJumping:
                    currentSmoothSpeed = smoothSpeed * 1.2f; // Slightly faster for jumps
                    break;
                default:
                    currentSmoothSpeed = smoothSpeed;
                    break;
            }
        }
        
        public void OnVelocityChanged(MapleClient.GameLogic.Vector2 velocity)
        {
            // Velocity changes are handled in UpdateCameraPosition for lookahead
        }
        
        public void OnGroundedStateChanged(bool isGrounded)
        {
            // Can add camera shake or other effects on landing
            if (isGrounded && !enableSmoothing)
            {
                // Small camera adjustment on landing
                StartCoroutine(LandingCameraEffect());
            }
        }
        
        public void OnAnimationEvent(PlayerAnimationEvent animEvent)
        {
            // Handle specific animation events if needed
        }
        
        public void OnMovementModifiersChanged(List<IMovementModifier> modifiers)
        {
            // Adjust camera behavior based on active modifiers
            bool hasSpeedModifier = false;
            foreach (var modifier in modifiers)
            {
                if (modifier.SpeedMultiplier > 1.5f)
                {
                    hasSpeedModifier = true;
                    break;
                }
            }
            
            // Increase lookahead for high-speed movement
            if (hasSpeedModifier && enableLookahead)
            {
                lookaheadAmount = 2f;
            }
            else
            {
                lookaheadAmount = 1f;
            }
        }
        
        private System.Collections.IEnumerator LandingCameraEffect()
        {
            float elapsed = 0f;
            float duration = 0.1f;
            float intensity = 0.05f;
            Vector3 originalPos = transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 50f) * intensity * (1f - elapsed / duration);
                transform.position = originalPos + Vector3.up * shake;
                yield return null;
            }
            
            transform.position = originalPos;
        }
        
        public void SetCameraBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            useCameraBounds = true;
        }
        
        public void ResetToTarget()
        {
            if (target != null)
            {
                Vector3 resetPosition = target.position + offset;
                transform.position = resetPosition;
                currentLookahead = Vector3.zero;
                targetLookahead = Vector3.zero;
                lastTargetPosition = target.position;
                
                // Force camera to update immediately
                cam = GetComponent<Camera>();
                if (cam != null)
                {
                    cam.transform.position = resetPosition;
                }
                
                Debug.Log($"[SimpleCameraFollow] Camera force reset to: {transform.position}, target at: {target.position}");
            }
        }
        
        void OnDestroy()
        {
            // Unregister from player
            if (player != null)
            {
                player.RemoveViewListener(this);
            }
        }
    }
}