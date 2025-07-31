using UnityEngine;
using MapleClient.GameLogic.Core;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    public class MonsterView : MonoBehaviour
    {
        private Monster monster;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private float hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.2f;
        
        // Interpolation for smooth rendering
        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private bool useInterpolation = true;
        private GameWorld gameWorld;
        private float manualInterpolationFactor = -1f; // -1 means use automatic

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                // Create a simple colored square as placeholder
                var texture = new Texture2D(32, 32);
                var pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.red; // Red for monsters
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            }
            
            originalColor = spriteRenderer.color;
        }

        public void SetMonster(Monster monster)
        {
            if (this.monster != null)
            {
                this.monster.DamageTaken -= OnDamageTaken;
                this.monster.Died -= OnDied;
            }

            this.monster = monster;
            
            if (this.monster != null)
            {
                this.monster.DamageTaken += OnDamageTaken;
                this.monster.Died += OnDied;
                
                // Initialize positions
                var pos = new Vector3(monster.Position.X, monster.Position.Y, 0);
                transform.position = pos;
                previousPosition = pos;
                currentPosition = pos;
            }
        }

        private void Start()
        {
            // Find GameWorld for interpolation
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var fieldInfo = typeof(GameManager).GetField("gameWorld", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    gameWorld = fieldInfo.GetValue(gameManager) as GameWorld;
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (monster != null && !monster.IsDead)
            {
                // Store positions for interpolation (coordinates are already in units)
                previousPosition = currentPosition;
                currentPosition = new Vector3(monster.Position.X, monster.Position.Y, 0);
            }
        }
        
        private void Update()
        {
            if (monster != null && !monster.IsDead)
            {
                // Interpolate position for smooth visual movement
                if (useInterpolation)
                {
                    float interpolationFactor = manualInterpolationFactor >= 0f ? 
                        manualInterpolationFactor : 
                        (gameWorld != null ? gameWorld.GetPhysicsInterpolationFactor() : 0f);
                    transform.position = Vector3.Lerp(previousPosition, currentPosition, interpolationFactor);
                }
                else
                {
                    // Fallback to direct position sync (coordinates are already in units)
                    transform.position = new Vector3(monster.Position.X, monster.Position.Y, 0);
                }
                
                // Update hit flash
                if (hitFlashTimer > 0)
                {
                    hitFlashTimer -= Time.deltaTime;
                    if (hitFlashTimer <= 0)
                    {
                        spriteRenderer.color = originalColor;
                    }
                }
            }
        }

        private void OnDamageTaken(Monster monster, int damage)
        {
            // Flash white when hit
            spriteRenderer.color = Color.white;
            hitFlashTimer = HitFlashDuration;
            
            // Could also spawn damage number here
            Debug.Log($"Monster took {damage} damage! HP: {monster.HP}/{monster.MaxHP}");
        }

        private void OnDied(Monster monster)
        {
            // Play death animation (for now, just fade out)
            StartCoroutine(DeathAnimation());
        }

        private System.Collections.IEnumerator DeathAnimation()
        {
            float fadeTime = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                var color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
                yield return null;
            }
            
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            SetMonster(null);
        }
        
        public void SetInterpolationFactor(float factor)
        {
            manualInterpolationFactor = Mathf.Clamp01(factor);
        }
    }
}