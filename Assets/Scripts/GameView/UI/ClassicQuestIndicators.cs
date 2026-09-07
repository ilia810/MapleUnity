using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    [DefaultExecutionOrder(1100)]
    public sealed class ClassicQuestIndicators : MonoBehaviour
    {
        public enum Marker { None, Available, Ready }
        private sealed class Bubble { public Image Art; public Marker State; public float Since; }
        private GameWorld world;private Canvas canvas;private RectTransform root;
        private ClassicSpriteFrames available,ready;
        private readonly Dictionary<WorldLabelAnchor,Bubble> bubbles=new Dictionary<WorldLabelAnchor,Bubble>();
        private readonly List<WorldLabelAnchor> removed=new List<WorldLabelAnchor>();
        public Marker ForNpc(int npc)
        {
            var state=Marker.None;if(world==null)return state;
            foreach(var q in world.Quests.Definitions)
            {
                if(q.Finish.Npc==npc && world.Quests.CanFinish(q.Id))return Marker.Ready;
                if(q.Start.Npc==npc && world.Quests.CanStart(q.Id))state=Marker.Available;
            }
            return state;
        }
        public void Bind(GameWorld value)
        {
            world=value;if(root!=null)return;canvas=GetComponent<Canvas>();
            root=ClassicUI.Rect("NpcQuestIndicators",transform,0,0);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;root.SetAsFirstSibling();
            available=new ClassicSpriteFrames("ui","UIWindow.img/QuestIcon/0");ready=new ClassicSpriteFrames("ui","UIWindow.img/QuestIcon/1");
        }
        private void OnEnable()=>Canvas.willRenderCanvases+=PositionBubbles;
        private void OnDisable()=>Canvas.willRenderCanvases-=PositionBubbles;
        private void LateUpdate()
        {
            if(world==null || root==null)return;
            removed.Clear();foreach(var pair in bubbles)if(pair.Key==null || !pair.Key.Visible || ForNpc(pair.Key.NpcId)==Marker.None)removed.Add(pair.Key);
            foreach(var anchor in removed){GetComponent<ClassicTooltipView>()?.Hide(bubbles[anchor].Art);bubbles[anchor].Art.gameObject.SetActive(false);Destroy(bubbles[anchor].Art.gameObject);bubbles.Remove(anchor);}
            foreach(var anchor in WorldLabelAnchor.Active)
            {
                if(!anchor.Visible || anchor.Kind!=WorldLabelAnchor.ActorKind.Npc)continue;
                var state=ForNpc(anchor.NpcId);if(state==Marker.None)continue;
                if(!bubbles.TryGetValue(anchor,out var bubble))
                {
                    var image=ClassicUI.Rect("NpcQuest_"+anchor.NpcId,root,44,44).gameObject.AddComponent<Image>();image.rectTransform.pivot=new Vector2(0,1);
                    var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
                    button.onClick.AddListener(()=>{var dialogue=GetComponent<ClassicNpcDialogue>();if(!dialogue.TryOpen(dialogue.SpawnFor(anchor)))GetComponent<StatusBar>()?.PostMessage("Move closer to speak to "+anchor.Label+".");});
                    var hover=image.gameObject.AddComponent<ClassicHover>();hover.Enter=e=>ClassicTooltipView.For(transform).ShowText(image,e.position,anchor.Label,ForNpc(anchor.NpcId)==Marker.Ready?"Quest ready to turn in. Click to talk.":"Quest available. Click to talk.");hover.Exit=()=>GetComponent<ClassicTooltipView>()?.Hide(image);
                    bubble=new Bubble{Art=image};bubbles.Add(anchor,bubble);
                }
                if(bubble.State!=state){bubble.State=state;bubble.Since=Time.unscaledTime;}
            }
            PositionBubbles();
        }
        private void PositionBubbles()
        {
            var camera=Camera.main;if(camera==null || root==null)return;
            foreach(var pair in bubbles)
            {
                var anchor=pair.Key;var bubble=pair.Value;if(anchor==null || bubble.Art==null)continue;
                var renderer=anchor.GetComponentInChildren<SpriteRenderer>();if(renderer==null)continue;
                var bounds=renderer.bounds;var screen=camera.WorldToScreenPoint(new Vector3(bounds.center.x,bounds.max.y,0));
                var frame=(bubble.State==Marker.Ready?ready:available).Sample(Time.unscaledTime-bubble.Since);
                bool visible=anchor.Visible && screen.z>0 && camera.pixelRect.Contains(screen) && frame!=null;
                bubble.Art.gameObject.SetActive(visible);if(!visible)continue;
                bubble.Art.sprite=frame.Sprite;bubble.Art.rectTransform.sizeDelta=frame.Sprite.rect.size;
                var point=ClassicSpriteFrames.CanvasPoint(root,canvas,screen)+new Vector2(-frame.Origin.x,frame.Origin.y+26);
                bubble.Art.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x),Mathf.Round(point.y));
            }
        }
    }
}
