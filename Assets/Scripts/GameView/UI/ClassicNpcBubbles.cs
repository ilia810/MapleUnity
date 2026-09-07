using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Original NPC speech artwork with staggered four-second ambient lines.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class ClassicNpcBubbles : MonoBehaviour
    {
        private sealed class Bubble {public RectTransform Root,Arrow;public Text Text;public string Line;public bool Scheduled;}
        private readonly Dictionary<int,string[]> lines=new Dictionary<int,string[]>();
        private readonly Dictionary<WorldLabelAnchor,Bubble> bubbles=new Dictionary<WorldLabelAnchor,Bubble>();
        private readonly List<Rect> occupied=new List<Rect>();
        private GameWorld world;private RectTransform root;private Canvas canvas;private Font font;private float started;
        public void Bind(GameWorld value)
        {
            world=value;if(root!=null)return;started=Time.unscaledTime;canvas=GetComponent<Canvas>();font=Font.CreateDynamicFontFromOSFont("Arial",12);
            root=ClassicUI.Rect("NpcSpeechBubbles",transform,0,0);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            // Bubbles cover nearby world nameplates, while every ordinary UI window stays above them.
            var names=transform.Find("WorldNameplates");root.SetSiblingIndex(names!=null?names.GetSiblingIndex()+1:0);
        }
        public string SpeechAt(int npc,float seconds)
        {
            if(!lines.TryGetValue(npc,out var source)){source=NxNpcSpeech.Read(NXAssetLoader.Instance.GetNxFile("npc"),NXAssetLoader.Instance.GetNxFile("string"),npc);lines[npc]=source;}
            double clock=System.Math.Max(0,seconds)+npc%120/10.0;
            return source.Length==0 || clock%12>=4?null:source[(int)(clock/12)%source.Length];
        }
        private void OnEnable()=>Canvas.willRenderCanvases+=Position;
        private void OnDisable()=>Canvas.willRenderCanvases-=Position;
        private void LateUpdate()=>PresentAt(Time.unscaledTime-started);
        public void PresentAt(float seconds)
        {
            if(world==null || root==null)return;
            foreach(var anchor in bubbles.Keys.Where(a=>a==null || !a.Visible).ToArray())
            {bubbles[anchor].Root.gameObject.SetActive(false);Destroy(bubbles[anchor].Root.gameObject);bubbles.Remove(anchor);}
            var dialogue=GetComponent<ClassicNpcDialogue>();
            foreach(var anchor in WorldLabelAnchor.Active.Where(a=>a.Visible && a.Kind==WorldLabelAnchor.ActorKind.Npc))
            {
                string line=dialogue!=null && dialogue.Visible && dialogue.NpcId==anchor.NpcId?null:SpeechAt(anchor.NpcId,seconds);
                if(!bubbles.TryGetValue(anchor,out var bubble))
                {
                    if(line==null)continue;
                    bubble=new Bubble{Root=ClassicUI.Rect("NpcSpeechBubble_"+anchor.NpcId,root,108,70)};bubble.Root.pivot=new Vector2(.5f,0);bubbles.Add(anchor,bubble);
                }
                bubble.Scheduled=line!=null;
                if(line!=null && line!=bubble.Line)Draw(bubble,line);
            }
            Position();
        }
        private void Draw(Bubble bubble,string line)
        {
            foreach(Transform child in bubble.Root){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            bubble.Line=line;
            string text=ClassicQuestText.Read(world,null,line);
            bubble.Text=ClassicWindowSkin.Label("NpcAmbientText",bubble.Root,font,text,8,6,80,1,12,new Color32(128,0,0,255));
            bubble.Text.alignment=TextAnchor.UpperCenter;
            // Long original lines get a wider card, keeping normal bubbles close to the reference's narrow shape.
            int inner=96;
            if(bubble.Text.preferredHeight>238){inner=156;bubble.Text.rectTransform.sizeDelta=new Vector2(140,1);}
            int height=Mathf.CeilToInt((bubble.Text.preferredHeight+8)/14)*14;
            float width=inner+12;
            bubble.Root.sizeDelta=new Vector2(width,height+19);
            Piece("c",bubble.Root,6,6,inner,height,true);
            Piece("n",bubble.Root,6,0,inner,6,true);Piece("s",bubble.Root,6,height+6,inner,6,true);
            Piece("w",bubble.Root,0,6,6,height,true);Piece("e",bubble.Root,inner+6,6,6,height,true);
            Piece("nw",bubble.Root,0,0,6,6);Piece("ne",bubble.Root,inner+6,0,6,6);
            Piece("sw",bubble.Root,0,height+6,6,6);Piece("se",bubble.Root,inner+6,height+6,6,6);
            bubble.Arrow=Piece("arrow",bubble.Root,width/2-1,height+6,13,13).rectTransform;
            ClassicUI.Place(bubble.Text.rectTransform,14,9,inner-16,height-4);bubble.Text.transform.SetAsLastSibling();
        }
        private static Image Piece(string key,Transform parent,float x,float y,float w,float h,bool tile=false)
        {
            var image=ClassicUI.Art("Speech_"+key,parent,"ChatBalloon.img/npc/"+key,x,y);ClassicUI.Place(image.rectTransform,x,y,w,h);
            if(tile)image.type=Image.Type.Tiled;return image;
        }
        private void Position()
        {
            var camera=Camera.main;if(camera==null || world==null || root==null)return;occupied.Clear();
            var markers=GetComponent<ClassicQuestIndicators>();
            foreach(var pair in bubbles.OrderBy(b=>b.Key==null?float.MaxValue:Mathf.Abs(b.Key.Feet.x-world.Player.Position.X)))
            {
                var anchor=pair.Key;var bubble=pair.Value;
                bool visible=anchor!=null && anchor.Visible && bubble.Scheduled && occupied.Count<3;
                if(visible)
                {
                    var renderer=anchor.GetComponentInChildren<SpriteRenderer>();visible=renderer!=null && renderer.sprite!=null;
                    if(visible)
                    {
                        var b=renderer.bounds;var screen=camera.WorldToScreenPoint(new Vector3(b.center.x,b.max.y,0));
                        var point=ClassicSpriteFrames.CanvasPoint(root,canvas,screen);
                        float y=point.y+8+(markers!=null && markers.ForNpc(anchor.NpcId)!=ClassicQuestIndicators.Marker.None?54:0);
                        float x=Mathf.Clamp(point.x,root.rect.xMin+bubble.Root.rect.width/2+4,root.rect.xMax-bubble.Root.rect.width/2-4);
                        var bounds=new Rect(Mathf.Round(x-bubble.Root.rect.width/2),Mathf.Round(y),bubble.Root.rect.width,bubble.Root.rect.height);
                        visible=screen.z>0 && camera.pixelRect.Contains(screen) && bounds.yMax<=root.rect.yMax-4 && !occupied.Any(r=>r.Overlaps(bounds));
                        if(visible)
                        {
                            bubble.Root.anchoredPosition=new Vector2(Mathf.Round(x),Mathf.Round(y));
                            bubble.Arrow.anchoredPosition=new Vector2(Mathf.Clamp(point.x-x+bubble.Root.rect.width/2-1,7,bubble.Root.rect.width-20),bubble.Arrow.anchoredPosition.y);
                            bounds.xMin-=8;bounds.xMax+=8;occupied.Add(bounds);
                        }
                    }
                }
                bubble.Root.gameObject.SetActive(visible);
                var animator=anchor!=null?anchor.GetComponent<ClassicNpcAnimator>():null;
                if(animator!=null)animator.AmbientSpeaking=visible;
            }
        }
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
