using UnityEngine;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameView
{
    /// <summary>
    /// Manages visual effects for movement modifiers, skills, and environmental effects
    /// </summary>
    public class VisualEffectManager : MonoBehaviour
    {
        private static VisualEffectManager instance;
        public static VisualEffectManager Instance => instance;
        
        // Effect pools
        private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, List<GameObject>> activeEffects = new Dictionary<string, List<GameObject>>();
        
        // Effect prefabs (created at runtime for now)
        private Dictionary<string, GameObject> effectPrefabs = new Dictionary<string, GameObject>();
        
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            InitializeEffectPrefabs();
        }
        
        private void InitializeEffectPrefabs()
        {
            // Create basic effect prefabs
            CreateSpeedBoostEffect();
            CreateSlowEffect();
            CreateStunEffect();
            CreateIceEffect();
            CreateSwimmingEffect();
            CreateFlashJumpTrailEffect();
            CreateDoubleJumpEffect();
            CreateSkillCooldownIndicator();
        }
        
        private void CreateSpeedBoostEffect()
        {
            var prefab = new GameObject("SpeedBoostEffect");
            prefab.SetActive(false);
            
            // Create speed lines effect
            for (int i = 0; i < 5; i++)
            {
                var line = new GameObject($"SpeedLine_{i}");
                line.transform.SetParent(prefab.transform);
                
                var renderer = line.AddComponent<SpriteRenderer>();
                var texture = new Texture2D(20, 2);
                var pixels = new Color[40];
                for (int j = 0; j < pixels.Length; j++)
                {
                    pixels[j] = new Color(0.5f, 1f, 0.5f, 0.6f);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 20, 2), new Vector2(0.5f, 0.5f), 100);
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 97;
                
                // Position lines around player
                float angle = (i / 5f) * 360f;
                line.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.5f, 
                                                          Mathf.Sin(angle * Mathf.Deg2Rad) * 0.5f, 0);
                line.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            
            effectPrefabs["SpeedBoost"] = prefab;
        }
        
        private void CreateSlowEffect()
        {
            var prefab = new GameObject("SlowEffect");
            prefab.SetActive(false);
            
            // Create weight/chain visual
            var chain = new GameObject("Chain");
            chain.transform.SetParent(prefab.transform);
            chain.transform.localPosition = new Vector3(0, -0.3f, 0);
            
            var renderer = chain.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(10, 30);
            var pixels = new Color[300];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.4f, 0.4f, 0.4f, 0.7f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 10, 30), new Vector2(0.5f, 1f), 100);
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 96;
            
            effectPrefabs["Slow"] = prefab;
        }
        
        private void CreateStunEffect()
        {
            var prefab = new GameObject("StunEffect");
            prefab.SetActive(false);
            
            // Create spinning stars
            for (int i = 0; i < 3; i++)
            {
                var star = new GameObject($"Star_{i}");
                star.transform.SetParent(prefab.transform);
                
                var renderer = star.AddComponent<SpriteRenderer>();
                var texture = new Texture2D(8, 8);
                var pixels = new Color[64];
                
                // Simple star pattern
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        if ((x == 4 || y == 4) || (x == y || x == 7 - y))
                        {
                            pixels[y * 8 + x] = new Color(1f, 1f, 0f, 0.8f);
                        }
                        else
                        {
                            pixels[y * 8 + x] = Color.clear;
                        }
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 100);
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = 1003;
                
                // Position stars in a circle above player
                float angle = (i / 3f) * 360f;
                star.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.3f, 
                                                          0.4f + Mathf.Sin(angle * Mathf.Deg2Rad) * 0.1f, 0);
            }
            
            effectPrefabs["Stun"] = prefab;
        }
        
        private void CreateIceEffect()
        {
            var prefab = new GameObject("IceEffect");
            prefab.SetActive(false);
            
            // Create ice crystals at feet
            var ice = new GameObject("IceCrystals");
            ice.transform.SetParent(prefab.transform);
            ice.transform.localPosition = new Vector3(0, -0.3f, 0);
            
            var renderer = ice.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(30, 10);
            var pixels = new Color[300];
            
            // Ice crystal pattern
            for (int i = 0; i < pixels.Length; i++)
            {
                float noise = Random.Range(0.7f, 1f);
                pixels[i] = new Color(0.5f * noise, 0.8f * noise, 1f * noise, 0.6f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 30, 10), new Vector2(0.5f, 0.5f), 100);
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 95;
            
            effectPrefabs["Ice"] = prefab;
        }
        
        private void CreateSwimmingEffect()
        {
            var prefab = new GameObject("SwimmingEffect");
            prefab.SetActive(false);
            
            // Create bubble particles
            for (int i = 0; i < 5; i++)
            {
                var bubble = new GameObject($"Bubble_{i}");
                bubble.transform.SetParent(prefab.transform);
                
                var renderer = bubble.AddComponent<SpriteRenderer>();
                var texture = new Texture2D(6, 6);
                var pixels = new Color[36];
                
                // Circle pattern for bubble
                Vector2 center = new Vector2(3, 3);
                for (int y = 0; y < 6; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist < 2.5f)
                        {
                            pixels[y * 6 + x] = new Color(0.7f, 0.9f, 1f, 0.5f);
                        }
                        else
                        {
                            pixels[y * 6 + x] = Color.clear;
                        }
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                
                renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f), 100);
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 94;
                
                // Random positions around player
                bubble.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), 
                                                           Random.Range(-0.2f, 0.2f), 0);
            }
            
            effectPrefabs["Swimming"] = prefab;
        }
        
        private void CreateFlashJumpTrailEffect()
        {
            var prefab = new GameObject("FlashJumpTrail");
            prefab.SetActive(false);
            
            var trail = prefab.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.9f, 0.5f, 0.8f);
            trail.endColor = new Color(1f, 0.9f, 0.5f, 0f);
            trail.sortingLayerName = "Player";
            trail.sortingOrder = 99;
            
            effectPrefabs["FlashJumpTrail"] = prefab;
        }
        
        private void CreateDoubleJumpEffect()
        {
            var prefab = new GameObject("DoubleJumpEffect");
            prefab.SetActive(false);
            
            // Create jump ring effect
            var ring = new GameObject("JumpRing");
            ring.transform.SetParent(prefab.transform);
            
            var renderer = ring.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(40, 40);
            var pixels = new Color[1600];
            
            // Ring pattern
            Vector2 center = new Vector2(20, 20);
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > 15f && dist < 18f)
                    {
                        pixels[y * 40 + x] = new Color(0.5f, 0.8f, 1f, 0.7f);
                    }
                    else
                    {
                        pixels[y * 40 + x] = Color.clear;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 40, 40), new Vector2(0.5f, 0.5f), 100);
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 98;
            
            effectPrefabs["DoubleJump"] = prefab;
        }
        
        private void CreateSkillCooldownIndicator()
        {
            var prefab = new GameObject("SkillCooldown");
            prefab.SetActive(false);
            
            var indicator = new GameObject("CooldownCircle");
            indicator.transform.SetParent(prefab.transform);
            indicator.transform.localPosition = new Vector3(0, -0.5f, 0);
            
            var text = indicator.AddComponent<TextMesh>();
            text.text = "0.0s";
            text.fontSize = 10;
            text.color = new Color(1f, 0.5f, 0.5f, 0.8f);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.04f;
            
            var renderer = indicator.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = 1004;
            
            effectPrefabs["SkillCooldown"] = prefab;
        }
        
        public GameObject SpawnEffect(string effectType, Vector3 position, Transform parent = null, float duration = 0f)
        {
            if (!effectPrefabs.ContainsKey(effectType))
            {
                Debug.LogWarning($"Effect type '{effectType}' not found");
                return null;
            }
            
            GameObject effect = GetFromPool(effectType);
            effect.transform.position = position;
            if (parent != null)
            {
                effect.transform.SetParent(parent);
            }
            
            effect.SetActive(true);
            
            if (!activeEffects.ContainsKey(effectType))
            {
                activeEffects[effectType] = new List<GameObject>();
            }
            activeEffects[effectType].Add(effect);
            
            if (duration > 0)
            {
                StartCoroutine(ReturnToPoolAfterDelay(effect, effectType, duration));
            }
            
            return effect;
        }
        
        private GameObject GetFromPool(string effectType)
        {
            if (!effectPools.ContainsKey(effectType))
            {
                effectPools[effectType] = new Queue<GameObject>();
            }
            
            if (effectPools[effectType].Count > 0)
            {
                return effectPools[effectType].Dequeue();
            }
            
            // Create new instance
            return Instantiate(effectPrefabs[effectType]);
        }
        
        public void ReturnToPool(GameObject effect, string effectType)
        {
            effect.SetActive(false);
            effect.transform.SetParent(null);
            
            if (activeEffects.ContainsKey(effectType))
            {
                activeEffects[effectType].Remove(effect);
            }
            
            if (!effectPools.ContainsKey(effectType))
            {
                effectPools[effectType] = new Queue<GameObject>();
            }
            
            effectPools[effectType].Enqueue(effect);
        }
        
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject effect, string effectType, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(effect, effectType);
        }
        
        public void ShowMovementModifierEffect(IMovementModifier modifier, Transform target)
        {
            string effectType = GetEffectTypeForModifier(modifier);
            if (effectType != null)
            {
                SpawnEffect(effectType, target.position, target);
            }
        }
        
        private string GetEffectTypeForModifier(IMovementModifier modifier)
        {
            if (modifier.Id == "slippery_surface") return "Ice";
            if (modifier.Id == "swimming") return "Swimming";
            if (modifier.Id.StartsWith("stun_")) return "Stun";
            if (modifier.SpeedMultiplier > 1.2f) return "SpeedBoost";
            if (modifier.SpeedMultiplier < 0.8f) return "Slow";
            return null;
        }
        
        public void UpdateSkillCooldown(string skillName, float cooldownRemaining, Vector3 position)
        {
            var cooldownEffect = SpawnEffect("SkillCooldown", position);
            if (cooldownEffect != null)
            {
                var text = cooldownEffect.GetComponentInChildren<TextMesh>();
                if (text != null)
                {
                    text.text = $"{cooldownRemaining:F1}s";
                }
                
                StartCoroutine(UpdateCooldownText(cooldownEffect, cooldownRemaining));
            }
        }
        
        private System.Collections.IEnumerator UpdateCooldownText(GameObject cooldownEffect, float duration)
        {
            var text = cooldownEffect.GetComponentInChildren<TextMesh>();
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float remaining = duration - elapsed;
                
                if (text != null)
                {
                    text.text = $"{remaining:F1}s";
                    text.color = Color.Lerp(new Color(0.5f, 1f, 0.5f, 0.8f), 
                                          new Color(1f, 0.5f, 0.5f, 0.8f), 
                                          remaining / duration);
                }
                
                yield return null;
            }
            
            ReturnToPool(cooldownEffect, "SkillCooldown");
        }
        
        public void ClearAllEffects()
        {
            foreach (var kvp in activeEffects)
            {
                foreach (var effect in kvp.Value.ToArray())
                {
                    ReturnToPool(effect, kvp.Key);
                }
            }
            activeEffects.Clear();
        }
        
        void OnDestroy()
        {
            ClearAllEffects();
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}