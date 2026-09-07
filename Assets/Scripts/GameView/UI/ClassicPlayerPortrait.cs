using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Original standing shop look, composed from current equipment without changing the actor.</summary>
    public sealed class ClassicPlayerPortrait : MonoBehaviour
    {
        private MapleCharacterRenderer source,model;
        private string appearance;
        private readonly List<Image> layers=new List<Image>();
        private void LateUpdate()=>Present();
        public void Present()
        {
            var actor=GameObject.Find("Player")?.GetComponent<MapleCharacterRenderer>();
            if(actor==null)return;
            string key=actor.AppearanceKey;
            if(actor==source && key==appearance)return;
            source=actor;appearance=key;
            if(model==null)
            {
                var hidden=new GameObject("ShopPortraitModel");hidden.transform.SetParent(transform,false);
                hidden.SetActive(false);model=hidden.AddComponent<MapleCharacterRenderer>();
            }
            var composition=model.ComposeStandingPortrait(actor);
            foreach(var image in layers){image.gameObject.SetActive(false);Destroy(image.gameObject);}layers.Clear();
            var sprites=composition.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r=>r.enabled&&r.sprite!=null&&r.name!="WeaponAfterimage"&&r.name!="SkillUseEffect").OrderBy(r=>r.sortingOrder).ToArray();
            if(sprites.Length==0)return;
            var bounds=sprites.Select(r=>new Rect((Vector2)r.transform.localPosition*r.sprite.pixelsPerUnit-r.sprite.pivot,r.sprite.rect.size)).ToArray();
            float left=bounds.Min(r=>r.xMin),right=bounds.Max(r=>r.xMax),bottom=bounds.Min(r=>r.yMin),top=bounds.Max(r=>r.yMax);
            var area=((RectTransform)transform).rect;float scale=Mathf.Min(1,(area.width-4)/(right-left),(area.height-6)/(top-bottom));
            foreach(var renderer in sprites)
            {
                var image=ClassicWindowSkin.Fill(renderer.name,transform,0,0,1,1,Color.white);layers.Add(image);
                var sprite=renderer.sprite;image.sprite=sprite;
                var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=sprite.pivot/sprite.rect.size;rect.sizeDelta=sprite.rect.size;
                rect.anchoredPosition=((Vector2)renderer.transform.localPosition*sprite.pixelsPerUnit-new Vector2((left+right)/2,bottom))*scale+new Vector2(0,4);
                rect.localScale=Vector3.one*scale;
            }
        }
    }
}
