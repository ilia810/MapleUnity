using System;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MapleClient.GameView
{
    public class MonsterView : MonoBehaviour
    {
        private Monster monster;
        private NxMobAnimations animations;
        private MobAnimation animation;
        private SortingGroup group;
        private Transform visual;
        private SpriteRenderer body;
        private SpriteRenderer skillHit;
        private SpriteRenderer statusEffect;
        private ActiveMonsterDebuff statusSource;
        private MapleClient.GameLogic.Data.WeaponAfterimage statusAnimation;
        private int statusDuration;
        private double animationTime;
        private float showHealth;
        private float interpolationFactor = 1;
        public event Action<MonsterView> Removed;
        public Monster Model => monster;
        public string CurrentAnimationName { get; private set; }
        public int CurrentFrameIndex { get; private set; }
        public SpriteRenderer BodyRenderer => body;
        public bool HealthFeedbackVisible => monster != null && !monster.IsDead && showHealth > 0;
        public Vector3 HeadPosition
        {
            get
            {
                if(monster==null)return transform.position;
                var head=monster.ContactBounds;
                return transform.position+new Vector3((monster.FacingRight&&!monster.Template.NoFlip?-head.HeadX:head.HeadX)/100f,-head.HeadY/100f,0);
            }
        }

        private void Awake()
        {
            group = gameObject.AddComponent<SortingGroup>();
            group.sortingLayerName = StageRenderOrder.SortingLayerName;
            visual = new GameObject("VisualRoot").transform;
            visual.SetParent(transform, false);
            body = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            body.transform.SetParent(visual, false);
            skillHit = new GameObject("SkillHitEffect").AddComponent<SpriteRenderer>();
            skillHit.transform.SetParent(transform, false);
            statusEffect = new GameObject("MonsterStatusEffect").AddComponent<SpriteRenderer>();
            statusEffect.transform.SetParent(transform, false);
        }

        public void SetMonster(Monster value, NxMobAnimations source = null)
        {
            if (monster != null) { monster.DamageTaken -= OnDamageTaken; monster.Died -= OnDied; }
            monster = value;
            if (monster == null) return;
            (GetComponent<WorldLabelAnchor>() ?? gameObject.AddComponent<WorldLabelAnchor>()).Bind(monster);
            animations = source ?? new NxMobAnimations(global::GameData.NXDataManagerSingleton.Instance.DataManager);
            monster.DamageTaken += OnDamageTaken; monster.Died += OnDied;
            transform.position = new Vector3(monster.Position.X, monster.Position.Y, 0);
            SelectAnimation(monster.Template.CanFly ? "fly" : "stand");
            RenderFrame();
        }

        private void SelectAnimation(string action)
        {
            if (CurrentAnimationName == action && animation != null) return;
            CurrentAnimationName = action;
            animation = animations.Get(monster.MonsterId, action);
            if (animation == null && action != "die1") animation = animations.Get(monster.MonsterId, "stand");
            animationTime = 0;
        }

        private void Update()
        {
            if (monster == null) return;
            if (!monster.IsDead)
            {
                string action = monster.IsHit ? "hit1" : monster.Template.CanFly ? "fly" :
                    Math.Abs(monster.Velocity.X) > .01f ? "move" : "stand";
                SelectAnimation(action);
            }
            animationTime += Time.deltaTime;
            showHealth = Mathf.Max(0, showHealth - Time.deltaTime);
            if (monster.IsDead && animationTime >= (animation?.DurationSeconds ?? .1)) Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (monster == null) return;
            var previous = monster.PreviousPosition;
            var current = monster.Position;
            transform.position = Vector3.Lerp(new Vector3(previous.X, previous.Y, 0),
                new Vector3(current.X, current.Y, 0), monster.IsDead ? 1 : interpolationFactor);
            group.sortingOrder = StageRenderOrder.MonsterOrder(monster.CurrentFootholdLayer);
            RenderFrame();
        }

        private void RenderSkillHit()
        {
            skillHit.sprite = null;
            var effect = monster.SkillHitEffect;
            if (effect == null) return;
            var frame = effect.Sample(out float fraction); if (frame == null) return;
            skillHit.sprite = MapleClient.GameData.SkillSprites.Frame(effect.AssetFile, frame.Path);
            skillHit.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Lerp(frame.StartAlpha, frame.EndAlpha, fraction)));
            float scale = Mathf.Max(0, Mathf.Lerp(frame.StartScale, frame.EndScale, fraction));
            skillHit.transform.localScale = new Vector3(effect.FacingRight ? -scale : scale, scale, 1);
            skillHit.transform.localPosition = new Vector3(effect.Offset.X, effect.Offset.Y, 0);
            skillHit.sortingOrder = effect.Z < 0 ? -1 : 3;
        }

        private void RenderFrame()
        {
            RenderSkillHit();
            RenderStatus();
            if (animation == null) { body.sprite = null; return; }
            CurrentFrameIndex = animation.Sample(animationTime, !monster.IsDead, out float t);
            var frame = animation.Frames[CurrentFrameIndex];
            body.sprite = frame.Image.Sprite;
            body.transform.localPosition = new Vector3(-frame.Image.Origin.x / 100, frame.Image.Origin.y / 100, 0);
            body.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Lerp(frame.StartAlpha, frame.EndAlpha, t)));
            float scale = Mathf.Max(0, Mathf.Lerp(frame.StartScale, frame.EndScale, t));
            visual.localScale = new Vector3(monster.FacingRight && !monster.Template.NoFlip ? -scale : scale, scale, 1);
        }

        private void RenderStatus()
        {
            statusEffect.sprite = null;
            var active = monster.StatDebuff;
            if (active != statusSource)
            {
                statusSource = active; statusAnimation = null; statusDuration = 0;
                if (active?.Visual?.Frames?.Length > 0)
                {
                    statusAnimation = new MapleClient.GameLogic.Data.WeaponAfterimage { Frames = active.Visual.Frames };
                    foreach (var f in active.Visual.Frames) statusDuration += Math.Max(1, f.DelayMilliseconds);
                }
            }
            if (active == null || statusAnimation == null || statusDuration <= 0) return;
            int index = statusAnimation.Sample(active.ElapsedMilliseconds % statusDuration, out float fraction);
            if (index < 0) return;
            var frame = statusAnimation.Frames[index];
            statusEffect.sprite = SkillSprites.Frame(active.Visual.AssetFile, frame.Path);
            statusEffect.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Lerp(frame.StartAlpha, frame.EndAlpha, fraction)));
            float scale = Mathf.Max(0, Mathf.Lerp(frame.StartScale, frame.EndScale, fraction));
            statusEffect.transform.localScale = new Vector3(scale, scale, 1);
            statusEffect.transform.localPosition = active.Visual.Position == 0 ? HeadPosition - transform.position : Vector3.zero;
            statusEffect.sortingOrder = active.Visual.Z < 0 ? -1 : 4;
        }

        private void OnDamageTaken(Monster target, int damage) { if (damage > 0) showHealth = 2; }
        private void OnDied(Monster target) { SelectAnimation("die1"); RenderFrame(); }
        public void SetInterpolationFactor(float factor) { interpolationFactor = Mathf.Clamp01(factor); }
        private void OnDestroy()
        {
            Removed?.Invoke(this);
            if (monster != null) { monster.DamageTaken -= OnDamageTaken; monster.Died -= OnDied; }
            monster = null;
        }
    }
}
