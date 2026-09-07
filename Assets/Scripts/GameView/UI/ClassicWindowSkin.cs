using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    /// <summary>Recomposes the original frame bevels around reference-sized, native-pixel content.</summary>
    public static class ClassicWindowSkin
    {
        public static readonly Color Paper=new Color32(225,225,214,255);
        public static readonly Color List=new Color32(207,220,229,255);
        public static readonly Color Footer=new Color32(157,180,202,255);
        public static readonly Color Ink=new Color32(50,59,65,255);
        public static Image Fill(string name,Transform parent,float x,float y,float w,float h,Color color,bool blocks=false)
        { var r=ClassicUI.Rect(name,parent,w,h);ClassicUI.Place(r,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=blocks;return image; }
        public static Image Border(string name,Transform parent,float x,float y,float w,float h,string path="UIWindow.img/Item/backgrnd",int edge=3)
        {
            var image=ClassicUI.Art(name,parent,path,x,y);ClassicUI.Place(image.rectTransform,x,y,w,h);
            if(image.sprite!=null)
            {
                var s=image.sprite;var frame=Sprite.Create(s.texture,s.rect,Vector2.zero,s.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(edge,edge,edge,edge));
                image.sprite=frame;image.gameObject.AddComponent<ClassicSpriteOwner>().Value=frame;image.type=Image.Type.Sliced;image.fillCenter=false;
            }
            return image;
        }
        public static RectTransform Surface(string name,Transform parent,float w,float h,Font font,string title,string titleArt=null,bool footer=true)
        {
            var root=ClassicUI.Rect(name,parent,w,h);ClassicUI.Place(root,0,0,w,h);
            Fill("Paper",root,4,4,w-8,h-8,new Color32(247,247,244,255),true);
            Fill("TitleWhite",root,5,4,w-10,17,Color.white);
            Fill("TitleLine",root,4,22,w-8,3,new Color32(186,201,212,255));
            if(footer)
            { Fill("Footer",root,4,h-27,w-8,23,Footer);Fill("FooterLine",root,4,h-29,w-8,2,Color.white); }
            Border("Frame",root,0,0,w,h);
            if(titleArt!=null)ClassicUI.Region("TitleArt",root,titleArt,new Rect(6,4,Mathf.Min(135,w-30),14),6,4,Mathf.Min(135,w-30),14);
            else Label("Title",root,font,title,7,5,w-27,15,10,Ink,true);
            return root;
        }
        public static Text Label(string name,Transform parent,Font font,string value,float x,float y,float w,float h,int size=12,Color? color=null,bool bold=false)
        {
            var text=ClassicUI.Text(name,parent,font,value,size);ClassicUI.Place(text.rectTransform,x,y,w,h);
            text.color=color??Ink;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;return text;
        }
        public static Button Action(string name,Transform parent,Font font,string text,float x,float y,float w,UnityAction action,Color? tint=null)
        {
            var image=Fill(name,parent,x,y,w,18,tint??new Color32(233,158,69,255),true);
            // A shallow highlight/shade keeps custom labels readable inside classic bevels.
            // These overlays retain the caller's color and Button's native hover/press tint.
            for(int band=0;band<6;band++)
            {
                Fill("TopSheen"+band,image.transform,2,2+band,w-4,1,new Color(1,1,1,(6-band)*.025f));
                Fill("BottomShade"+band,image.transform,2,9+band,w-4,1,new Color(0,0,0,band*.012f));
            }
            Fill("TopEdge",image.transform,0,0,w,1,new Color32(90,96,98,255));
            Fill("BottomEdge",image.transform,0,17,w,1,new Color32(90,96,98,255));
            Fill("LeftEdge",image.transform,0,0,1,18,new Color32(90,96,98,255));
            Fill("RightEdge",image.transform,w-1,0,1,18,new Color32(90,96,98,255));
            Fill("Highlight",image.transform,2,1,w-4,1,new Color(1,1,1,.65f));
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            var t=Label("Label",image.transform,font,text,2,0,w-4,18,11,Color.white);t.alignment=TextAnchor.MiddleCenter;t.resizeTextForBestFit=true;t.resizeTextMinSize=8;t.resizeTextMaxSize=11;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-1);shadow.effectColor=new Color(0,0,0,.35f);return b;
        }
        public static Button ScrollArrow(string name,Transform parent,float x,float y,bool up,UnityAction action)
        {
            string key=up?"prev":"next";
            var image=ClassicUI.Art(name,parent,"Basic.img/VScr4/enabled/"+key+"0",x,y);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.SpriteSwap;
            button.spriteState=new SpriteState{highlightedSprite=ClassicUI.Sprite("Basic.img/VScr4/enabled/"+key+"1"),pressedSprite=ClassicUI.Sprite("Basic.img/VScr4/enabled/"+key+"1"),disabledSprite=ClassicUI.Sprite("Basic.img/VScr4/disabled/"+key)};
            button.onClick.AddListener(action);return button;
        }
        public static Scrollbar Scrollbar(string name,Transform parent,float x,float y,float height,UnityAction<float> changed,bool withArrows=false)
        {
            var track=ClassicUI.Art(name,parent,"Basic.img/VScr4/enabled/base",x,y);
            track.rectTransform.sizeDelta=new Vector2(15,height);track.type=Image.Type.Tiled;track.raycastTarget=true;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            var slide=ClassicUI.Rect("SlidingArea",track.transform,15,height);
            ClassicUI.Place(slide,0,withArrows?13:0,15,height-(withArrows?26:0));
            var thumb=ClassicUI.Art("Thumb",slide,"Basic.img/VScr4/enabled/thumb0",0,0);
            thumb.raycastTarget=true;bar.handleRect=thumb.rectTransform;bar.targetGraphic=thumb;
            thumb.rectTransform.offsetMin=thumb.rectTransform.offsetMax=Vector2.zero;
            bar.transition=Selectable.Transition.SpriteSwap;
            bar.spriteState=new SpriteState{highlightedSprite=ClassicUI.Sprite("Basic.img/VScr4/enabled/thumb1"),pressedSprite=ClassicUI.Sprite("Basic.img/VScr4/enabled/thumb1")};
            track.gameObject.AddComponent<ClassicScrollbarSkin>().Initialize(bar,withArrows);
            if(changed!=null)bar.onValueChanged.AddListener(changed);
            return bar;
        }
    }
    public sealed class ClassicScrollWheel : MonoBehaviour, IScrollHandler
    {
        public System.Action<int> Scroll;
        public void OnScroll(PointerEventData e) { if(e.scrollDelta.y!=0){Scroll?.Invoke(e.scrollDelta.y>0?-1:1);e.Use();} }
    }
}
