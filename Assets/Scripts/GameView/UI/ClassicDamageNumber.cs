// Digit sets, advances, row heights and fade follow HeavenClient DamageNumber.cpp.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System.Collections.Generic;
using MapleClient.GameData;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    public enum ClassicDamageStyle { Normal, Critical, Incoming }
    public sealed class ClassicDamageNumber : MonoBehaviour
    {
        public int Value { get; private set; }
        public ClassicDamageStyle Style { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public CanvasGroup Group { get; private set; }
        public bool HasArtwork { get; private set; }
        private static readonly int[] advances={24,20,22,22,24,23,24,22,24,24};
        public static int RowHeight(bool critical)=>critical?36:30;
        public static float Opacity(float age)=>Mathf.Clamp01(1.5f-age*2);
        public static int Advance(char digit,bool first,ClassicDamageStyle style)
            =>advances[digit-'0']+(style==ClassicDamageStyle.Critical?(first?8:4):(first?2:0));
        public void Bind(Text fallback,int damage,ClassicDamageStyle style)
        {
            Value=damage;Style=style;
            Group=gameObject.AddComponent<CanvasGroup>();Group.blocksRaycasts=false;Group.interactable=false;
            string family=style==ClassicDamageStyle.Critical?"NoCri":style==ClassicDamageStyle.Incoming?"NoViolet":"NoRed";
            var glyphs=new List<Image>();float x=0;float minX=float.MaxValue,maxX=float.MinValue;
            string digits=damage>0?damage.ToString():"";
            if(damage<=0)glyphs.Add(Glyph(family=="NoCri"?"NoRed0/Miss":family+"0/Miss",0,0));
            else for(int i=0;i<digits.Length;i++)
            {
                glyphs.Add(Glyph(family+(i==0?"1/":"0/")+digits[i],x,i==0?0:i%2==1?2:-2));
                x+=i==0?Advance(digits[0],true,style):i<digits.Length-1?
                    (Advance(digits[i],false,style)+Advance(digits[i+1],false,style))/2:Advance(digits[i],false,style);
            }
            HasArtwork=glyphs.TrueForAll(g=>g.sprite!=null);
            foreach(var glyph in glyphs)
            {
                minX=Mathf.Min(minX,glyph.rectTransform.anchoredPosition.x);
                maxX=Mathf.Max(maxX,glyph.rectTransform.anchoredPosition.x+glyph.rectTransform.rect.width);
            }
            // Center the visible digits over the target. The old C++ draw path subtracts its width from Y.
            float shift=Mathf.Round((minX+maxX)/2);
            foreach(var glyph in glyphs)glyph.rectTransform.anchoredPosition-=new Vector2(shift,0);
            if(damage>0 && style==ClassicDamageStyle.Critical)
            {
                var burst=Glyph("NoCri1/effect",-shift,20);burst.name="CriticalBurst";burst.transform.SetAsFirstSibling();
            }
            fallback.enabled=!HasArtwork;
            foreach(var graphic in GetComponentsInChildren<Image>())graphic.enabled=HasArtwork && graphic.sprite!=null;
        }
        private Image Glyph(string suffix,float x,float sourceY)
        {
            string path="BasicEff.img/"+suffix;var node=NXAssetLoader.Instance.GetNxFile("effect")?.GetNode(path);
            var sprite=MapleClient.GameData.SpriteLoader.LoadSprite(node,"effect/"+path);
            var origin=MapleClient.GameData.SpriteLoader.GetOrigin(node);
            var r=ClassicUI.Rect("DamageGlyph_"+suffix.Replace('/','_'),transform,sprite?.rect.width ?? 0,sprite?.rect.height ?? 0);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x-origin.x,origin.y-sourceY);
            var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
        }
    }
}
