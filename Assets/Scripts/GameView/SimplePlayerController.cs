using UnityEngine;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    /// <summary>
    /// Simple working player controller that replaces the broken MapleStory physics
    /// </summary>
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 10f;
        
        [Header("Components")]
        private Rigidbody2D rb;
        private BoxCollider2D col;
        private SpriteRenderer spriteRenderer;
        
        [Header("State")]
        private bool isGrounded = false;
        private float horizontalInput = 0f;
        private int groundContactCount = 0; // Track number of ground contacts
        private GameObject currentGroundObject = null; // Track what we're standing on
        
        // Reference to game logic player (for stats, inventory, etc)
        private Player gameLogicPlayer;
        
        // Debug tracking
        private float lastDebugTime = 0f;
        private float debugInterval = 1f; // Log position every second
        
        void Awake()
        {
            // Add required components
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.freezeRotation = true;
            
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.3f, 0.6f);
            
            Debug.Log($"[PLATFORM_DEBUG] Player Rigidbody2D created with gravity scale: {rb.gravityScale}");
            
            // Create simple blue sprite
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(30, 60);
            var pixels = new Color[30 * 60];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.2f, 0.4f, 0.8f, 1f); // Blue color
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 30, 60), new Vector2(0.5f, 0.5f), 100);
            spriteRenderer.sortingLayerName = "Player";
            spriteRenderer.sortingOrder = 100;
            
            // Set layer for physics
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
        
        public void SetGameLogicPlayer(Player player)
        {
            gameLogicPlayer = player;
            // Sync initial position
            if (player != null)
            {
                transform.position = new Vector3(player.Position.X, player.Position.Y, 0);
            }
        }
        
        void Update()
        {
            // Get input
            horizontalInput = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                horizontalInput = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                horizontalInput = 1f;
            
            // Jump
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt)) && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
            
            // Update facing direction
            if (horizontalInput != 0)
            {
                transform.localScale = new Vector3(horizontalInput > 0 ? 1 : -1, 1, 1);
            }
        }
        
        void FixedUpdate()
        {
            // Apply horizontal movement
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
            
            // Perform ground check using raycast as backup
            PerformGroundCheck();
            
            // Sync position back to game logic
            if (gameLogicPlayer != null)
            {
                gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(transform.position.x, transform.position.y);
            }
            
            // Debug logging
            if (Time.time - lastDebugTime > debugInterval)
            {
                lastDebugTime = Time.time;
                Debug.Log($"[PLATFORM_DEBUG] Player Position: {transform.position}, Velocity: {rb.velocity}, Grounded: {isGrounded}, GroundContacts: {groundContactCount}");
            }
        }
        
        void PerformGroundCheck()
        {
            // Cast a short ray downward to check for ground
            float rayLength = 0.05f;
            Vector2 rayOrigin = (Vector2)transform.position - new Vector2(0, col.size.y / 2);
            
            // Cast multiple rays across the width of the character
            bool groundDetected = false;
            for (int i = 0; i < 3; i++)
            {
                float xOffset = (i - 1) * col.size.x * 0.4f; // Left, center, right
                Vector2 origin = rayOrigin + new Vector2(xOffset, 0);
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength);
                
                if (hit.collider != null)
                {
                    groundDetected = true;
                    Debug.DrawRay(origin, Vector2.down * rayLength, Color.green);
                    
                    // If we detected ground but aren't marked as grounded, fix it
                    if (!isGrounded && rb.velocity.y <= 0.1f) // Only if we're not moving up
                    {
                        isGrounded = true;
                        groundContactCount = 1;
                        currentGroundObject = hit.collider.gameObject;
                        Debug.Log($"[PLATFORM_DEBUG] Ground check detected platform: {hit.collider.name}");
                    }
                    break;
                }
                else
                {
                    Debug.DrawRay(origin, Vector2.down * rayLength, Color.red);
                }
            }
            
            // If no ground detected and we think we're grounded, double-check
            if (!groundDetected && isGrounded && groundContactCount == 0)
            {
                isGrounded = false;
                currentGroundObject = null;
            }
        }
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"[PLATFORM_DEBUG] OnCollisionEnter2D: {collision.gameObject.name}, Normal: {(collision.contacts.Length > 0 ? collision.contacts[0].normal.ToString() : "No contacts")}");
            
            // Check if we landed on something from above
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // Only consider it ground if we're landing from above (normal pointing up)
                if (contact.normal.y > 0.5f)
                {
                    groundContactCount++;
                    if (!isGrounded)
                    {
                        isGrounded = true;
                        currentGroundObject = collision.gameObject;
                        Debug.Log($"[PLATFORM_DEBUG] Player grounded on: {collision.gameObject.name}");
                    }
                    break; // Only count one ground contact per object
                }
            }
        }
        
        void OnCollisionExit2D(Collision2D collision)
        {
            Debug.Log($"[PLATFORM_DEBUG] OnCollisionExit2D: {collision.gameObject.name}");
            
            // Only check if this was a ground contact
            // We need to verify if the collision that's exiting was actually supporting us
            bool wasGroundContact = false;
            
            // Check if this object was providing ground support
            if (collision.gameObject == currentGroundObject)
            {
                wasGroundContact = true;
            }
            // Note: OnCollisionExit2D doesn't provide valid contact points, 
            // so we can't check normals here. We rely on our tracking of currentGroundObject
            
            if (wasGroundContact)
            {
                groundContactCount--;
                if (groundContactCount <= 0)
                {
                    groundContactCount = 0;
                    isGrounded = false;
                    currentGroundObject = null;
                    Debug.Log("[PLATFORM_DEBUG] Player no longer grounded");
                }
                else
                {
                    Debug.Log($"[PLATFORM_DEBUG] Still grounded (contacts: {groundContactCount})");
                }
            }
        }
    }
}