using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Offline combat feedback and a reachable recovery action after defeat.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class PlayerCombatFeedback : MonoBehaviour
    {
        private GameWorld world;
        private Player player;
        private Font font;
        private RectTransform root, damageRoot;
        private Canvas canvas;
        private GameObject defeatPanel;
        private Button reviveButton;
        private ClassicNotifications notifications;
        public void ShowStats(bool visible) => GetComponent<CharacterProgressionView>()?.Show(visible);
        private sealed class Number { public Text Text; public ClassicDamageNumber Art; public Vector3 Position; public Monster Target; public float Age, ScreenOffsetY; }
        private readonly List<Number> numbers = new List<Number>();

        public void Bind(GameWorld value)
        {
            Unbind();
            world = value; player = world?.Player;
            if (root == null) CreateUI();
            if (player == null) return;
            (GetComponent<ClassicBuffView>()??gameObject.AddComponent<ClassicBuffView>()).Bind(player,world.SkillManager);
            notifications=GetComponent<ClassicNotifications>()??gameObject.AddComponent<ClassicNotifications>();
            (GetComponent<ClassicPlayerEffects>()??gameObject.AddComponent<ClassicPlayerEffects>()).Bind(world);
            player.DamageTaken += ShowDamage;
            player.ExperienceGained += ShowExperience;
            player.LeveledUp += ShowLevel;
            world.MapLoaded += OnMapLoaded;
            world.AttackResolved += ShowAttack;
            world.ItemPickedUp += ShowPickup;
            world.PlayerRecovered += ClearDamage;
            world.PlayerTeleported += ClearDamage;
        }

        private void CreateUI()
        {
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Verdana" }, 18);
            root = new GameObject("CombatFeedback", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            damageRoot=ClassicUI.Rect("WorldDamageNumbers",transform,0,0);
            damageRoot.anchorMin=Vector2.zero;damageRoot.anchorMax=Vector2.one;damageRoot.offsetMin=damageRoot.offsetMax=Vector2.zero;
            damageRoot.SetAsFirstSibling();
            defeatPanel = new GameObject("DefeatPanel", typeof(RectTransform));
            var panel = defeatPanel.GetComponent<RectTransform>(); panel.SetParent(root, false); panel.sizeDelta = new Vector2(266,150);
            panel.gameObject.AddComponent<ClassicWindow>();
            var defeatFrame = ClassicUI.Companion("DefeatFrame",panel,0,0,150);
            var message = ClassicUI.Text("DefeatMessage",defeatFrame,font,"You have been defeated.",14,true); ClassicUI.Place(message.rectTransform,12,40,242,35);
            reviveButton = ClassicUI.Button("ReviveButton",defeatFrame,font,"Revive at a safe spawn",()=>world?.RevivePlayer());
            ClassicUI.Place(reviveButton.GetComponent<RectTransform>(),12,116,242,19);
            defeatPanel.SetActive(false);
        }

        private Text CreateText(string name, Transform parent, int size, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
            text.transform.SetParent(parent, false); text.font = font; text.fontSize = size;
            text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.GetComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            return text;
        }

        private void ShowDamage(int damage)
        {
            var text = CreateText("PlayerDamage", damageRoot, 24, new Color(1, .35f, .45f));
            text.text = damage > 0 ? damage.ToString() : "MISS";
            text.rectTransform.sizeDelta = new Vector2(160, 50);
            var art=text.gameObject.AddComponent<ClassicDamageNumber>();art.Bind(text,damage,ClassicDamageStyle.Incoming);
            numbers.Add(new Number { Text = text, Art=art, Position = new Vector3(player.Position.X, player.Position.Y + .4f, 0) });
        }
        private void ShowAttack(Monster target, AttackHit hit)
        {
            var text = CreateText(hit.Miss ? "MonsterMiss" : hit.Critical ? "MonsterCritical" : "MonsterDamage",
                damageRoot, hit.Critical ? 24 : 22, hit.Miss ? new Color(.8f, .65f, 1) : hit.Critical ? new Color(1, .65f, .15f) : new Color(1, .94f, .55f));
            text.text = hit.Miss ? "MISS" : hit.Damage.ToString();
            text.rectTransform.sizeDelta = new Vector2(180, 50);
            var art=text.gameObject.AddComponent<ClassicDamageNumber>();art.Bind(text,hit.Miss?0:hit.Damage,hit.Critical?ClassicDamageStyle.Critical:ClassicDamageStyle.Normal);
            numbers.Add(new Number { Text = text, Art=art, Target=target, Position = new Vector3(target.Position.X,
                target.Position.Y - target.ContactBounds.HeadY / 100f, 0), ScreenOffsetY = 32+hit.LineIndex * ClassicDamageNumber.RowHeight(hit.Critical) });
        }

        private void ShowLevel(int level)
        {
            string message=$"Level up!  Level {level}";notifications.Post(message,ClassicNotifications.ExperienceColor);GetComponent<StatusBar>()?.PostMessage(message,ClassicNotifications.ExperienceColor);
        }
        public void ShowActionMessage(string message)
        { notifications.Post(message,new Color32(239,186,189,255));GetComponent<StatusBar>()?.PostMessage(message,new Color32(239,186,189,255),true); }
        private void ShowExperience(long amount)
        {
            string message=$"You received EXP (+{amount}).";notifications.Post(message,ClassicNotifications.ExperienceColor);GetComponent<StatusBar>()?.PostMessage(message,ClassicNotifications.ExperienceColor);
        }
        private void ShowPickup(int id,int amount)
        {
            string message=id==0?$"You received {amount} mesos.":$"{world.Player.GetItemInfo(id)?.Name??"Item"} ×{amount} obtained.";
            notifications.Post(message,Color.white);GetComponent<StatusBar>()?.PostMessage(message);
        }

        private void Update()
        {
            if (player == null || root == null) return;
            defeatPanel.SetActive(player.IsDead);
            if (player.IsDead) root.SetAsLastSibling();
            reviveButton.interactable = world.CanRevivePlayer;
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                var number = numbers[i]; number.Age += Time.deltaTime;
                if (number.Age >= .75f) { Destroy(number.Text.gameObject); numbers.RemoveAt(i); continue; }
                number.Art.Group.alpha=ClassicDamageNumber.Opacity(number.Age);
            }
        }
        private void OnEnable()=>Canvas.willRenderCanvases+=PositionNumbers;
        private void OnDisable()=>Canvas.willRenderCanvases-=PositionNumbers;
        private void LateUpdate()=>PositionNumbers();
        private void PositionNumbers()
        {
            var camera=Camera.main;if(camera==null || canvas==null || root==null)return;
            foreach(var number in numbers)
            {
                if(number.Text==null)continue;
                var position=number.Position;
                if(number.Target!=null)
                {
                    var head=number.Target.ContactBounds;
                    position.x=number.Target.Position.X+(number.Target.FacingRight&&!number.Target.Template.NoFlip?-head.HeadX:head.HeadX)/100f;
                }
                var screen=camera.WorldToScreenPoint(position);
                number.Text.gameObject.SetActive(screen.z>0);
                var point=ClassicSpriteFrames.CanvasPoint(damageRoot,canvas,screen)+Vector2.up*(number.ScreenOffsetY+number.Age*31.25f);
                number.Text.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x),Mathf.Round(point.y));
            }
        }

        private void OnMapLoaded(MapleClient.GameLogic.MapData map) { ClearDamage();notifications?.Clear(); }
        private void ClearDamage()
        {
            foreach (var number in numbers) if (number.Text != null) Destroy(number.Text.gameObject);
            numbers.Clear();
        }

        private void Unbind()
        {
            if (player != null) { player.DamageTaken -= ShowDamage; player.ExperienceGained -= ShowExperience; player.LeveledUp -= ShowLevel; }
            if (world != null) { world.MapLoaded -= OnMapLoaded; world.AttackResolved -= ShowAttack; world.ItemPickedUp -= ShowPickup; world.PlayerRecovered -= ClearDamage; world.PlayerTeleported -= ClearDamage; }
            ClearDamage();
        }
        private void OnDestroy() { Unbind(); if (font != null) Destroy(font); }
    }
}
