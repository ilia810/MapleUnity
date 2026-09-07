using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;
using MapleClient.GameData;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Renders MapleStory characters with proper layered sprites
    /// </summary>
    public class MapleCharacterRenderer : MonoBehaviour, IPlayerViewListener
    {
        private bool isPortrait;
        private Player player;
        private ICharacterDataProvider characterData;
        
        // Sprite layers in correct rendering order
        private SpriteRenderer backBodyRenderer; // Behind body
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer armRenderer;      // Body's arm part
        private SpriteRenderer armOverHairRenderer; // Arm that goes over hair
        private SpriteRenderer headRenderer;
        private SpriteRenderer hairRenderer;
        private SpriteRenderer hairOverHeadRenderer;
        private SpriteRenderer hairBelowBodyRenderer;
        private int cachedHatId = -1;
        private string capSlots = "";
        private SpriteRenderer faceRenderer;
        private SpriteRenderer hairShadeRenderer;
        private SpriteRenderer defaultTopRenderer;
        private SpriteRenderer defaultBottomRenderer;
        private readonly Dictionary<string, SpriteRenderer> bodyLayerRenderers = new Dictionary<string, SpriteRenderer>();
        private SpriteRenderer hatRenderer;
        private SpriteRenderer topRenderer;
        private SpriteRenderer bottomRenderer;
        private SpriteRenderer shoesRenderer;
        private SpriteRenderer weaponRenderer;
        private SpriteRenderer capeRenderer;
        private SpriteRenderer gloveRenderer;
        private SpriteRenderer shieldRenderer;
        private SpriteRenderer handRenderer;     // Hand part (over gloves)
        private SpriteRenderer afterimageRenderer;
        private SpriteRenderer skillEffectRenderer;
        private Transform armorEcho;
        private readonly Dictionary<SpriteRenderer, SpriteRenderer> armorEchoLayers = new Dictionary<SpriteRenderer, SpriteRenderer>();
        
        // Animation state
        private CharacterState currentState = CharacterState.Stand;
        private int currentFrame = 0;
        private CharacterExpression currentExpression;
        private int currentExpressionFrame;
        private GameManager simulationGame;
        private Transform visualRoot;
        private SortingGroup stageGroup;
        private bool isFacingRight = true;
        private readonly Dictionary<string, SpriteRenderer> equipmentLayers = new Dictionary<string, SpriteRenderer>();
        private readonly List<SpriteRenderer> opacityLayers = new List<SpriteRenderer>();
        private Sprite fallbackBodySprite;
        
        // Attachment points for current frame (neck, navel, hand, etc.)
        private Dictionary<string, Vector2> currentAttachmentPoints = new Dictionary<string, Vector2>();
        
        // Character appearance
        private int skinColor = 0;
        private int faceId = 20000; // Default face
        private int hairId = 30000; // Default hair
        
        [System.Diagnostics.Conditional("MAPLE_RENDERING_DEBUG")]
        private static void LogRendering(string message) { Debug.Log(message); }

        public void Initialize(Player player, ICharacterDataProvider characterData)
        {
            if (this.player != null) { this.player.RemoveViewListener(this); this.player.EquipmentChanged -= UpdateAppearance; }
            this.player = player;
            if (player != null) player.EquipmentChanged += UpdateAppearance;
            this.characterData = characterData;
            simulationGame = FindFirstObjectByType<GameManager>();
            player?.StanceAnimation.SetData(characterData as IStanceDataProvider);
            player?.SynchronizeBodyStance();
            player?.FaceAnimation.SetData((characterData as IFaceDataProvider)?.GetFaceAnimation(faceId));
            if (bodyRenderer == null) CreateSpriteRenderers();
            RefreshStageOrder();
            currentAttachmentPoints.Clear();
            currentFrame = 0;
            foreach (var layer in GetComponentsInChildren<SpriteRenderer>())
                if (layer.transform != transform) layer.sprite = null;
            if (player == null || characterData == null || player.IsDead) return;
            if (player.FacingRight.HasValue && player.FacingRight.Value != isFacingRight)
                SetFlipX(player.FacingRight.Value);
            currentState = GetCharacterState();
            SetFlipX(player.Velocity.X >= 0f);
            player.AddViewListener(this);
            UpdateSprites();
        }

        // UIShop requests STAND1/default expression; CharLook adjusts standing to the weapon.
        // This composition reads
        // appearance only and never registers as a player view or changes simulation clocks.
        public string AppearanceKey => skinColor+":"+faceId+":"+hairId+":"+
            (player==null?"":string.Join(",",player.GetEquippedItems().OrderBy(e=>e.Key).Select(e=>e.Key+"="+e.Value)));
        public Transform ComposeStandingPortrait(MapleCharacterRenderer source)
        {
            isPortrait=true;enabled=false;player=source.player;characterData=source.characterData;
            skinColor=source.skinColor;faceId=source.faceId;hairId=source.hairId;
            if(bodyRenderer==null)CreateSpriteRenderers();
            currentState=player.CurrentWeapon?.Stand??CharacterState.Stand;currentFrame=0;visualRoot.localScale=Vector3.one;
            UpdateSprites();return visualRoot;
        }

        private void OnDestroy()
        {
            if (player != null) { player.RemoveViewListener(this); player.EquipmentChanged -= UpdateAppearance; }
            player = null;
            if (fallbackBodySprite != null)
            {
                var texture = fallbackBodySprite.texture;
                if (Application.isPlaying) { Destroy(fallbackBodySprite); Destroy(texture); }
                else { DestroyImmediate(fallbackBodySprite); DestroyImmediate(texture); }
            }
        }

        private void CreateSpriteRenderers()
        {
            // Unity simulates the player center; HeavenClient draws at its feet.
            visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = new Vector3(0, -Player.Height / 2f, 0);
            // Sort the assembled character as one actor. Internal equipment/body
            // depths remain local to this group; UI attached to Player stays outside.
            stageGroup = visualRoot.gameObject.AddComponent<SortingGroup>();
            stageGroup.sortingLayerName = StageRenderOrder.SortingLayerName;
            // Create sprite renderers in correct layer order matching C++ client CharLook::draw()
            // The order here matches the actual drawing order in MapleStory
            
            // Back layers (behind body)
            shieldRenderer = CreateSpriteLayer("Shield", -3);
            capeRenderer = CreateSpriteLayer("Cape", -2);
            backBodyRenderer = CreateSpriteLayer("BackBody", -1);
            
            // Main body
            bodyRenderer = CreateSpriteLayer("Body", 0);
            
            // Arm below head (drawn after body but before head)
            armRenderer = CreateSpriteLayer("Arm", 18);
            
            // Equipment on body
            shoesRenderer = CreateSpriteLayer("Shoes", 2);
            bottomRenderer = CreateSpriteLayer("Bottom", 5);
            topRenderer = CreateSpriteLayer("Top", 7);
            
            // Gloves (first layer)
            gloveRenderer = CreateSpriteLayer("Glove", 5);
            
            // Head layer - MUST be higher than body and initial equipment
            headRenderer = CreateSpriteLayer("Head", 10);
            
            // Source draw order: head, default hair, face, uncovered hair.
            hairShadeRenderer = CreateSpriteLayer("HairShade", 11);
            hairRenderer = CreateSpriteLayer("Hair", 12);
            faceRenderer = CreateSpriteLayer("Face", 13);
            hairOverHeadRenderer = CreateSpriteLayer("HairOverHead", 14);
            hairBelowBodyRenderer = CreateSpriteLayer("HairBelowBody", -5);
            
            // Hat/cap layer
            hatRenderer = CreateSpriteLayer("Hat", 15);
            
            // Arm/hand layers that go over hair
            armOverHairRenderer = CreateSpriteLayer("ArmOverHair", 20);
            handRenderer = CreateSpriteLayer("Hand", 19);
            
            // Weapon on top
            weaponRenderer = CreateSpriteLayer("Weapon", 17);
            defaultBottomRenderer = CreateSpriteLayer("DefaultBottom", 4);
            defaultTopRenderer = CreateSpriteLayer("DefaultTop", 6);
            afterimageRenderer = CreateSpriteLayer("WeaponAfterimage", 24);
            skillEffectRenderer = CreateSpriteLayer("SkillUseEffect", 31);
            bodyLayerRenderers["body"] = bodyRenderer;
            bodyLayerRenderers["arm"] = armRenderer;
            bodyLayerRenderers["armOverHair"] = armOverHairRenderer;
            bodyLayerRenderers["handBelowWeapon"] = handRenderer;
        }
        
        private SpriteRenderer CreateSpriteLayer(string layerName, int sortingOrder)
        {
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(visualRoot, false); // Use SetParent with worldPositionStays = false
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localScale = Vector3.one;
            layerObj.transform.localRotation = Quaternion.identity;
            
            SpriteRenderer renderer = layerObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sortingOrder * 10;
            
            // Enable the renderer
            renderer.enabled = true;
            
            LogRendering($"Created sprite layer: {layerName} with sorting order {sortingOrder} on layer 'Player' at local position {layerObj.transform.localPosition}");
            
            return renderer;
        }
        
        private void RefreshStageOrder()
        {
            if (stageGroup != null)
                stageGroup.sortingOrder = StageRenderOrder.PlayerOrder(player?.CurrentFootholdLayer ?? 0);
        }

        private void LateUpdate()
        {
            // After simulation, including stationary climbing and contact changes
            // which do not move the root. The solver retains the layer in the air.
            RefreshStageOrder();
            if (player == null || visualRoot == null) return;
            var actionMove = player.IsBasicAttacking ? player.BasicAttack.VisualOffset : MapleClient.GameLogic.Vector2.Zero;
            visualRoot.localPosition = new Vector3(actionMove.X / 100, -Player.Height / 2f - actionMove.Y / 100, 0);
            // CharLook's action moves its assembled body; Char draws trails and
            // use effects separately at the original feet, even while facing left.
            var effectOrigin = visualRoot.InverseTransformPoint(transform.TransformPoint(new Vector3(0, -Player.Height / 2f, 0)));
            if (afterimageRenderer != null) afterimageRenderer.transform.localPosition = effectOrigin;
            if (skillEffectRenderer != null) skillEffectRenderer.transform.localPosition = effectOrigin;
            float alpha = player.IsDead ? .4f : player.IsInvulnerable ?
                .45f + .55f * Mathf.Abs(Mathf.Sin(player.InvulnerableMilliseconds * .015f)) : 1f;
            alpha *= player.ConcealmentOpacity;
            visualRoot.GetComponentsInChildren<SpriteRenderer>(opacityLayers);
            foreach (var layer in opacityLayers)
            {
                var color = layer.color; color.a = alpha; layer.color = color;
            }
            UpdateAfterimage();
            UpdateSkillEffect();
            UpdateArmorEcho();
        }

        private void UpdateArmorEcho()
        {
            // Char::draw / IronBodyUseEffect (HeavenClient, AGPL-3.0-or-later):
            // draw the assembled look again, growing from 1x to 2x while fading
            // over 500 ms. There is deliberately no replacement texture.
            bool visible = !isPortrait && !player.IsDead && player.ArmorEchoMilliseconds > 0;
            if (armorEcho != null) armorEcho.gameObject.SetActive(visible);
            if (!visible) return;
            if (armorEcho == null)
            {
                armorEcho = new GameObject("ArmorEcho").transform; armorEcho.SetParent(visualRoot, false);
                var group = armorEcho.gameObject.AddComponent<SortingGroup>();
                group.sortingLayerName = "Player"; group.sortingOrder = 400;
            }
            float elapsed = 1f - player.ArmorEchoMilliseconds / 500f;
            armorEcho.localScale = new Vector3(1f + elapsed, 1f + elapsed, 1);
            foreach (var source in opacityLayers)
            {
                if (source.transform.parent != visualRoot || source == afterimageRenderer || source == skillEffectRenderer) continue;
                if (!armorEchoLayers.TryGetValue(source, out var copy))
                {
                    copy = new GameObject(source.name).AddComponent<SpriteRenderer>();
                    copy.transform.SetParent(armorEcho, false); armorEchoLayers[source] = copy;
                }
                copy.sprite = source.sprite; copy.enabled = source.enabled;
                copy.transform.localPosition = source.transform.localPosition;
                copy.transform.localRotation = source.transform.localRotation;
                copy.transform.localScale = source.transform.localScale;
                copy.sortingLayerID = source.sortingLayerID; copy.sortingOrder = source.sortingOrder;
                copy.flipX = source.flipX; copy.flipY = source.flipY;
                copy.color = new Color(1, 1, 1, (1f - elapsed) * player.ConcealmentOpacity);
            }
        }

        private void UpdateSkillEffect()
        {
            if (skillEffectRenderer == null) return;
            skillEffectRenderer.sprite = null;
            var effect = player?.SkillEffect;
            if (effect == null) return;
            var frame = effect.Sample(out float fraction);
            if (frame == null) return;
            skillEffectRenderer.sprite = MapleClient.GameData.SkillSprites.Frame(effect.AssetFile, frame.Path);
            skillEffectRenderer.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Lerp(frame.StartAlpha, frame.EndAlpha, fraction)));
            skillEffectRenderer.sortingOrder = effect.Z < 0 ? -100 : 310;
            float scale = Mathf.Max(0, Mathf.Lerp(frame.StartScale, frame.EndScale, fraction));
            bool facing = player.IsBasicAttacking ? player.BasicAttack.FacingRight : (player.FacingRight ?? true);
            skillEffectRenderer.transform.localScale = new Vector3(effect.FacingRight == facing ? scale : -scale, scale, 1);
        }

        private void UpdateAfterimage()
        {
            if (afterimageRenderer == null) return;
            afterimageRenderer.sprite = null;
            var motion = player?.BasicAttack;
            var effect = motion?.Afterimage;
            if (player == null || player.IsDead || motion == null || motion.IsComplete || effect == null || motion.Frame < effect.FirstFrame) return;
            int frame = effect.Sample(motion.AfterimageMilliseconds, out float fraction);
            if (frame < 0) return;
            var data = effect.Frames[frame];
            afterimageRenderer.sprite = NxAfterimageSprites.Load(data.Path);
            afterimageRenderer.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Lerp(data.StartAlpha, data.EndAlpha, fraction)));
            float scale = Mathf.Max(0, Mathf.Lerp(data.StartScale, data.EndScale, fraction));
            afterimageRenderer.transform.localScale = new Vector3(scale, scale, 1);
        }

        void Update()
        {
            if (player == null || characterData == null) return;
            // Direction can change on a standing tick or at zero horizontal air speed.
            if (player.FacingRight.HasValue && player.FacingRight.Value != isFacingRight)
                SetFlipX(player.FacingRight.Value);
            
            // DON'T update position here - PlayerView handles that
            // The character renderer is a child of PlayerView GameObject
            
            float interpolation = simulationGame?.World?.GetPhysicsInterpolationFactor() ?? 1f;
            CharacterState newState = GetCharacterState();
            int frame = player.IsBasicAttacking ? player.BasicAttack.Frame : player.StanceAnimation.Sample(interpolation);
            if (newState != currentState || currentFrame != frame)
            {
                currentState = newState; currentFrame = frame;
                UpdateSprites();
            }
            else if (currentExpression != player.FaceAnimation.SampleExpression(interpolation) ||
                currentExpressionFrame != player.FaceAnimation.SampleFrame(interpolation))
            {
                UpdateFaceSprite();
                UpdateEquipmentSprites();
            }
        }

        private CharacterState GetCharacterState() => player.BodyStance;

        private string ConvertStateToAnimationName(CharacterState state) => CharacterStances.Name(state);

        private string ConvertExpressionToName(CharacterExpression expression) => CharacterExpressions.Name(expression);

        private void UpdateSprites()
        {
            if (player == null || characterData == null || bodyRenderer == null) return;
            // Update body parts
            UpdateBodySprite();
            UpdateHeadSprite();
            UpdateFaceSprite();
            UpdateHairSprite();
            
            // Update equipment
            UpdateEquipmentSprites();
            
            // Apply attachment offsets after all sprites are loaded
            // This ensures we have all the necessary sprites before computing offsets
            ApplyAttachmentOffsets();
        }
        
        private void UpdateBodySprite()
        {
            currentAttachmentPoints.Clear();
            foreach (var layer in bodyLayerRenderers.Values) layer.sprite = null;
            string stateName = ConvertStateToAnimationName(currentState);
            var parts = NXAssetLoader.Instance.LoadCharacterBodyParts(skinColor, stateName, currentFrame, out var attachments);
            currentAttachmentPoints = attachments ?? new Dictionary<string, Vector2>();
            foreach (var part in parts ?? new Dictionary<string, Sprite>())
            {
                if (!bodyLayerRenderers.TryGetValue(part.Key, out var layer))
                {
                    int order = BodyLayerOrder(part.Key);
                    if (order < 0) continue;
                    layer = CreateSpriteLayer(char.ToUpperInvariant(part.Key[0]) + part.Key.Substring(1), order);
                    bodyLayerRenderers[part.Key] = layer;
                }
                layer.sprite = part.Value;
            }
            if (bodyRenderer.sprite == null)
            {
                var fallback = characterData.GetBodySprite(skinColor, currentState, currentFrame) as UnitySpriteData;
                if (fallback?.UnitySprite != null) bodyRenderer.sprite = fallback.UnitySprite;
                else
                {
                    if (fallbackBodySprite == null) fallbackBodySprite = CreateColoredSprite(Color.blue, 32, 48, "Body");
                    bodyRenderer.sprite = fallbackBodySprite;
                }
            }
        }

        private static int BodyLayerOrder(string layer)
        {
            // CharLook::draw: each authored z layer keeps its own renderer.
            switch (layer)
            {
                case "body": return 0;
                case "armBelowHead": return 3;
                case "armBelowHeadOverMailChest": return 8;
                case "arm": return 18;
                case "handBelowWeapon": return 19;
                case "armOverHair": return 20;
                case "armOverHairBelowWeapon": return 20;
                case "handOverHair": return 21;
                case "handOverWeapon": return 22;
                default: return -1;
            }
        }

        private void ApplyAttachmentOffsets()
        {
            // C++ client positioning logic (from BodyDrawInfo.cpp and Body.cpp):
            // =================================================================
            // C++ Client Rendering Approach:
            // All character part sprites have their origins pre-shifted during loading.
            // This means when all parts are drawn at position (0,0), they align correctly.
            //
            // The shifts are computed during sprite loading in NXAssetLoader:
            // - Body parts: shift = body_position - part.map.navel
            // - Head: shift = head_position = body.neck - head.neck
            // - Face: shift = face_position = body.neck - head.neck + head.brow
            // - Hair: shift = hair_position = head.brow - head.neck + body.neck
            //
            // Since shifts are baked into the sprite pivots, all parts go at (0,0).
            // =================================================================

            LogRendering("[ApplyAttachmentOffsets] C++ style: All parts at (0,0) - shifts baked into sprites");

            // All body parts are positioned at (0,0) - the shift is in the sprite pivot
            if (bodyRenderer != null && bodyRenderer.sprite != null)
            {
                bodyRenderer.transform.localPosition = Vector3.zero;
            }

            if (armRenderer != null && armRenderer.sprite != null)
            {
                armRenderer.transform.localPosition = Vector3.zero;
            }

            if (armOverHairRenderer != null && armOverHairRenderer.sprite != null)
            {
                armOverHairRenderer.transform.localPosition = Vector3.zero;
            }

            if (handRenderer != null && handRenderer.sprite != null)
            {
                handRenderer.transform.localPosition = Vector3.zero;
            }

            // Head position is (0,0) since shift is baked into sprite
            UpdateHeadPosition(Vector3.zero);

            LogRendering("=== All parts positioned at (0,0) ===");
        }
        
        private Vector2 GetAttachmentPoint(params string[] keys)
        {
            // Try each key in order until we find a valid attachment point
            foreach (var key in keys)
            {
                if (currentAttachmentPoints.TryGetValue(key, out Vector2 point))
                {
                    return point;
                }
            }
            return Vector2.zero;
        }
        
        private void UpdateHeadSprite()
        {
            headRenderer.sprite = null;
            // C++ Client Approach (from BodyDrawInfo.cpp):
            // head_position = body.neck - head.neck
            // The head sprite is shifted by this position during loading

            string stateName = ConvertStateToAnimationName(currentState);

            // Get body.neck from current attachment points (collected during body loading)
            Vector2 bodyNeck = GetAttachmentPoint("body.map.neck", "neck");

            // First, load head to get its attachment points (including head.neck)
            Dictionary<string, Vector2> headAttachmentPoints;
            var headSprite = NXAssetLoader.Instance.LoadCharacterHead(skinColor, stateName, currentFrame, out headAttachmentPoints);

            if (headAttachmentPoints != null)
            {
                // Merge head attachment points
                foreach (var kvp in headAttachmentPoints)
                {
                    currentAttachmentPoints[kvp.Key] = kvp.Value;
                }
            }

            // Now get head.neck from the merged points
            Vector2 headNeck = GetAttachmentPoint("head.map.neck", "head.neck");

            // Calculate head shift: head_position = body.neck - head.neck
            Vector2 headShift = bodyNeck - headNeck;

            LogRendering($"[UpdateHeadSprite] Head shift calculation:");
            LogRendering($"  body.neck: {bodyNeck}");
            LogRendering($"  head.neck: {headNeck}");
            LogRendering($"  head_shift (body.neck - head.neck): {headShift}");

            // Reload head with the computed shift applied
            var headSpriteWithShift = NXAssetLoader.Instance.LoadCharacterHeadWithShift(skinColor, stateName, currentFrame, headShift, out headAttachmentPoints);

            if (headSpriteWithShift != null)
            {
                headRenderer.sprite = headSpriteWithShift;
                LogRendering($"Loaded head sprite with shift applied");
            }
            else if (headSprite != null)
            {
                // Fallback to non-shifted sprite
                headRenderer.sprite = headSprite;
                Debug.LogWarning("Using fallback head sprite without shift");
            }
        }
        
        private void UpdateFaceSprite()
        {
            float interpolation = simulationGame?.World?.GetPhysicsInterpolationFactor() ?? 1f;
            currentExpression = isPortrait ? CharacterExpression.Default : player.FaceAnimation.SampleExpression(interpolation);
            currentExpressionFrame = isPortrait ? 0 : player.FaceAnimation.SampleFrame(interpolation);
            faceRenderer.sprite = null;
            if (currentState == CharacterState.Ladder || currentState == CharacterState.Rope) return;
            // C++ Client Approach (from BodyDrawInfo.cpp):
            // face_position = body.neck - head.neck + head.brow
            // The face sprite is shifted by this position during loading

            string expressionName = ConvertExpressionToName(currentExpression);

            // Get attachment points for face position calculation
            Vector2 bodyNeck = GetAttachmentPoint("body.map.neck", "neck");
            Vector2 headNeck = GetAttachmentPoint("head.map.neck", "head.neck");
            Vector2 headBrow = GetAttachmentPoint("head.map.brow", "head.brow", "brow");

            // Calculate face shift: face_position = body.neck - head.neck + head.brow
            Vector2 faceShift = bodyNeck - headNeck + headBrow;

            LogRendering($"[UpdateFaceSprite] Face shift calculation:");
            LogRendering($"  body.neck: {bodyNeck}");
            LogRendering($"  head.neck: {headNeck}");
            LogRendering($"  head.brow: {headBrow}");
            LogRendering($"  face_shift (body.neck - head.neck + head.brow): {faceShift}");

            // Load face with shift applied
            Dictionary<string, Vector2> faceAttachmentPoints;
            var faceSprite = NXAssetLoader.Instance.LoadFaceWithShift(faceId, expressionName, faceShift, out faceAttachmentPoints, currentExpressionFrame);

            if (faceSprite != null)
            {
                faceRenderer.sprite = faceSprite;

                // Merge face attachment points
                if (faceAttachmentPoints != null)
                {
                    foreach (var kvp in faceAttachmentPoints)
                    {
                        currentAttachmentPoints[kvp.Key] = kvp.Value;
                    }
                }
                LogRendering($"Loaded face sprite with shift applied");
            }
            // An absent expression bitmap remains absent, as in Face::draw. Do
            // not replace an animated frame with an unshifted default face.
        }

        private void UpdateHairSprite()
        {
            hairRenderer.sprite = null;
            hairOverHeadRenderer.sprite = null;
            hairShadeRenderer.sprite = null;
            hairBelowBodyRenderer.sprite = null;
            string stateName = ConvertStateToAnimationName(currentState);
            Vector2 hairShift = GetAttachmentPoint("body.map.neck", "neck")
                - GetAttachmentPoint("head.map.neck", "head.neck")
                + GetAttachmentPoint("head.map.brow", "head.brow", "brow");
            bool climbing = currentState == CharacterState.Ladder || currentState == CharacterState.Rope;
            var loader = NXAssetLoader.Instance;
            player.GetEquippedItems().TryGetValue(EquipSlot.Hat, out int hatId);
            if (hatId != cachedHatId)
            {
                cachedHatId = hatId;
                capSlots = hatId <= 0 ? "" : loader.GetNxFile("character")?
                    .GetNode($"Cap/{hatId:D8}.img/info/vslot")?.GetValue<string>() ?? "";
            }
            // CharEquips::getcaptype / CharLook::draw (HeavenClient, AGPL-3.0-or-later).
            bool headband = capSlots == "CpH5";
            bool halfCover = capSlots == "CpH1H5";
            bool fullCover = capSlots == "CpH1H5AyAs" || capSlots.Contains("Hb") || capSlots.Contains("Hd");
            hairRenderer.sortingOrder = headband ? 152 : 120;
            hairOverHeadRenderer.sortingOrder = headband ? 154 : 140;
            if (climbing && fullCover) return;
            hairRenderer.sprite = loader.LoadHairWithShift(hairId, stateName, currentFrame, hairShift,
                out var attachments, climbing ? (halfCover ? "backHairBelowCap" : "backHair") : "hair");
            foreach (var point in attachments) currentAttachmentPoints[point.Key] = point.Value;
            if (climbing) return;
            hairShadeRenderer.sprite = loader.LoadHairWithShift(hairId, stateName, currentFrame, hairShift,
                out _, "hairShade");
            hairBelowBodyRenderer.sprite = loader.LoadHairWithShift(hairId, stateName, currentFrame,
                hairShift, out _, "hairBelowBody");
            if (hatId <= 0 || headband || capSlots == "Cp")
                hairOverHeadRenderer.sprite = loader.LoadHairWithShift(hairId, stateName, currentFrame,
                    hairShift, out _, "hairOverHead");
        }

        private void UpdateEquipmentSprites()
        {
            var equipped = player.GetEquippedItems();
            bool overall = equipped.TryGetValue(EquipSlot.Top, out int topId) && topId / 10000 == 105;
            defaultTopRenderer.sprite = equipped.ContainsKey(EquipSlot.Top) ? null : LoadDefaultClothing(1042399, "Coat");
            defaultBottomRenderer.sprite = overall || equipped.ContainsKey(EquipSlot.Bottom) ? null : LoadDefaultClothing(1060026, "Pants");
            foreach (var layer in equipmentLayers.Values) layer.sprite = null;
            string stance = ConvertStateToAnimationName(currentState);
            foreach (var equip in equipped)
            {
                foreach (var part in NxEquipmentFrames.Load(equip.Value, stance, currentFrame, currentAttachmentPoints,
                    currentExpression.ToString().ToLowerInvariant()))
                {
                    string key = equip.Key + "/" + part.Name;
                    if (!equipmentLayers.TryGetValue(key, out var layer))
                    {
                        layer = CreateSpriteLayer("Equipment_" + equip.Key + "_" + part.Name, 0);
                        equipmentLayers[key] = layer;
                    }
                    layer.sortingOrder = EquipmentOrder(equip.Key, part.Name, part.Layer,
                        currentState == CharacterState.Ladder || currentState == CharacterState.Rope,
                        player.CurrentWeapon?.UsesTwoHandedDrawOrder(currentState) == true);
                    layer.sprite = part.Sprite;
                }
            }
        }

        private static int EquipmentOrder(EquipSlot slot, string part, string z, bool climbing, bool twoHanded)
        {
            // CharLook draws ARM, MAILARM, WEAPON for two-handed poses; WEAPON precedes ARM otherwise.
            if (climbing)
            {
                switch (slot) {
                    case EquipSlot.Glove: return 10; case EquipSlot.Shoes: return 20;
                    case EquipSlot.Bottom: return 50; case EquipSlot.Top: return 70;
                    case EquipSlot.Cape: return 80; case EquipSlot.Hat: return 150;
                    case EquipSlot.Earring: return 105;
                    case EquipSlot.Shield: return 160; case EquipSlot.Weapon: return 170;
                }
            }
            if (part == "mailArm") return 182;
            switch (z) {
                case "weaponBelowBody": return -20; case "shieldBelowBody": return -30; case "capBelowBody": return -10;
                case "gloveWristOverBody": return 5; case "gloveOverBody": return 10;
                case "shieldOverHair": return 85; case "capOverHair": return 155;
                case "weaponBelowArm": return 160; case "weaponOverGlove": return 188;
                case "weaponOverHand": case "weaponOverBody": return 205;
                case "gloveWristOverHair": return 225; case "gloveOverHair": return 230;
            }
            switch (slot) {
                case EquipSlot.Cape: return -40; case EquipSlot.Shoes: return 20;
                case EquipSlot.Bottom: return 50; case EquipSlot.Top: return 70;
                case EquipSlot.Hat: return 150; case EquipSlot.Shield: return 135;
                case EquipSlot.Earring: return 90; case EquipSlot.FaceAccessory: return 131;
                case EquipSlot.EyeAccessory: return 132;
                case EquipSlot.Weapon: return twoHanded ? 183 : 170; case EquipSlot.Glove: return 185;
                default: return 0;
            }
        }

        private Sprite LoadDefaultClothing(int id, string category)
        {
            var loader = NXAssetLoader.Instance;
            string state = ConvertStateToAnimationName(currentState);
            // This NX pack omits the source client's default top.
            if (loader.GetNxFile("character")?.GetNode($"{category}/{id:D8}.img/{state}/{currentFrame}") == null)
                return null;
            return loader.LoadEquipment(id, category, state, currentFrame, currentAttachmentPoints);
        }

        private string GetEquipmentCategory(int itemId)
        {
            // MapleStory equipment categories based on item ID ranges
            int subtype = itemId / 10000 - 100;
            
            switch (subtype)
            {
                case 0: return "Cap"; // Hats
                case 1: return "FaceAccessory"; // Face accessories
                case 2: return "EyeAccessory"; // Eye accessories
                case 3: return "Earring"; // Earrings
                case 4: return "Coat"; // Top/Overall
                case 5: return "Longcoat"; // Overall
                case 6: return "Pants"; // Bottom
                case 7: return "Shoes"; // Shoes
                case 8: return "Glove"; // Gloves
                case 9: return "Shield"; // Shields
                case 10: return "Cape"; // Capes
                case 11: return "Ring"; // Rings
                case 12: return "Pendant"; // Pendants
                case 13: return "Belt"; // Belts
                case 14: return "Medal"; // Medals
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                case 49: return "Weapon"; // Various weapon types
                default: return "Etc";
            }
        }
        
        private void SetFlipX(bool flip)
        {
            if (player?.IsBasicAttacking == true) flip = player.BasicAttack.FacingRight;
            isFacingRight = flip;
            // Use scale-based flipping
            float scaleX = flip ? -1f : 1f;
            visualRoot.localScale = new Vector3(scaleX, 1f, 1f);

            LogRendering($"[MapleCharacterRenderer] Set character scale.x to {scaleX} (flip={flip})");
        }
        
        public void UpdateAppearance()
        {
            // Only an adjusted stance change resets CharLook; clothes and facing
            // changes keep the current body phase and refresh every attached layer.
            player?.SynchronizeBodyStance();
            currentState = GetCharacterState();
            currentFrame = player.IsBasicAttacking ? player.BasicAttack.Frame :
                player.StanceAnimation.Sample(simulationGame?.World?.GetPhysicsInterpolationFactor() ?? 1f);
            UpdateSprites();
        }
        
        public void SetCharacterAppearance(int skin, int face, int hair)
        {
            skinColor = skin;
            faceId = face;
            player?.FaceAnimation.SetData((characterData as IFaceDataProvider)?.GetFaceAnimation(faceId));
            hairId = hair;
            UpdateAppearance();
        }
        
        private void UpdateHeadPosition(Vector3 position)
        {
            // C++ Client Approach:
            // All part sprites have their origins pre-shifted during loading.
            // Head, face, hair all go at (0,0) - the shift is in the sprite pivot.

            if (headRenderer != null && headRenderer.gameObject != null)
            {
                headRenderer.transform.localPosition = position;
            }

            if (faceRenderer != null && faceRenderer.gameObject != null)
            {
                // Face shift is baked into sprite pivot
                faceRenderer.transform.localPosition = position;
            }

            if (hairRenderer != null && hairRenderer.gameObject != null)
            {
                // Hair shift is baked into sprite pivot
                hairRenderer.transform.localPosition = position;
            }

            if (hairOverHeadRenderer != null && hairOverHeadRenderer.gameObject != null)
                hairOverHeadRenderer.transform.localPosition = position;
            if (hairShadeRenderer != null) hairShadeRenderer.transform.localPosition = position;
            if (hairBelowBodyRenderer != null) hairBelowBodyRenderer.transform.localPosition = position;

            if (hatRenderer != null && hatRenderer.gameObject != null)
            {
                hatRenderer.transform.localPosition = position;
            }

            LogRendering($"Head and related parts positioned at: {position}");
        }
        
        
        private Sprite CreateColoredSprite(Color color, int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0f), // Fallback shares the authored feet anchor
                100f // Pixels per unit
            );
            sprite.name = name;
            return sprite;
        }
        
        #region IPlayerViewListener Implementation
        
        public void OnPositionChanged(MapleClient.GameLogic.Vector2 position)
        {
            // Position is handled by parent PlayerView
            RefreshStageOrder();
        }
        
        public void OnStateChanged(PlayerState state)
        {
            // Map PlayerState to CharacterState for animations
            LogRendering($"[MapleCharacterRenderer] State changed to: {state}");
            
            CharacterState oldState = currentState;
            CharacterState newState = GetCharacterState();
            
            if (oldState != newState)
            {
                currentState = newState;
                currentFrame = 0;
                UpdateSprites();
            }
        }
        
        public void OnVelocityChanged(MapleClient.GameLogic.Vector2 velocity)
        {
            if (velocity.X == 0f) return;
            bool faceRight = player?.FacingRight ?? (velocity.X > 0f);
            if (faceRight != isFacingRight) SetFlipX(faceRight);
        }

        public void OnGroundedStateChanged(bool isGrounded)
        {
            // Can use this for landing detection
            if (isGrounded && currentState == CharacterState.Jump)
            {
                LogRendering("[MapleCharacterRenderer] Landed!");
                // Landing will be handled by state change or animation event
            }
        }
        
        public void OnAnimationEvent(PlayerAnimationEvent animEvent)
        {
            if (player.IsBasicAttacking && animEvent != PlayerAnimationEvent.Attack) return;
            currentState = GetCharacterState();
            currentFrame = player.IsBasicAttacking ? player.BasicAttack.Frame :
                player.StanceAnimation.Sample(simulationGame?.World?.GetPhysicsInterpolationFactor() ?? 1f);
            UpdateSprites();
        }

        public void OnMovementModifiersChanged(System.Collections.Generic.List<IMovementModifier> modifiers)
        {
            // Handle movement modifiers if needed (ice, slow, etc.)
            LogRendering($"[MapleCharacterRenderer] Movement modifiers changed: {modifiers?.Count ?? 0} modifiers");
        }
        
        #endregion
        
        private void ExploreNxStructure()
        {
            LogRendering("=== Exploring NX File Structure ===");
            
            var loader = MapleClient.GameData.NXAssetLoader.Instance;
            var charFile = loader.GetNxFile("character");
            if (charFile != null && charFile.Root != null)
            {
                LogRendering("Character NX file root children:");
                int count = 0;
                foreach (var child in charFile.Root.Children)
                {
                    LogRendering($"  - {child.Name}");
                    if (count++ > 10) 
                    {
                        LogRendering("  ... (more children)");
                        break;
                    }
                }
                
                // Look for body sprites specifically
                var bodyNode = charFile.GetNode("00002000.img");
                if (bodyNode != null)
                {
                    LogRendering("Found 00002000.img (body sprites), children:");
                    count = 0;
                    foreach (var child in bodyNode.Children)
                    {
                        LogRendering($"  - {child.Name}");
                        if (count++ > 5) 
                        {
                            LogRendering("  ... (more children)");
                            break;
                        }
                    }
                    
                    // Try to explore stand animation - looks like animations are directly under bodyNode
                    var standNode = bodyNode["stand"];
                    if (standNode != null)
                    {
                        LogRendering("Found stand animation directly under 00002000.img, children:");
                        count = 0;
                        foreach (var child in standNode.Children)
                        {
                            LogRendering($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                            if (count++ > 5)
                            {
                                LogRendering("  ... (more children)");
                                break;
                            }
                        }
                        
                        // Look at frame 0
                        var frame0 = standNode["0"];
                        if (frame0 != null)
                        {
                            LogRendering("Found frame 0 of stand, children:");
                            foreach (var child in frame0.Children)
                            {
                                LogRendering($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                            }
                            
                            // Look for body parts
                            var bodyPart = frame0["body"];
                            if (bodyPart != null)
                            {
                                LogRendering("Found body part, exploring:");
                                LogRendering($"  Value type: {bodyPart.Value?.GetType().Name ?? "null"}");
                                foreach (var child in bodyPart.Children)
                                {
                                    LogRendering($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No stand node found directly under 00002000.img");
                    }
                }
            }
            else
            {
                Debug.LogError("Character NX file not loaded!");
            }
        }
    }
    
    // EquipSlot enum is now in MapleClient.GameLogic.Data
}
