// Frame geometry follows HeavenClient Graphics/Geometry.cpp::MobHpBar::draw.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    [DefaultExecutionOrder(1100)]
    public sealed class ClassicMonsterHealthView : MonoBehaviour
    {
        private sealed class Bar { public RectTransform Root, Fill, Shade; }
        private readonly Dictionary<MonsterView,Bar> bars=new Dictionary<MonsterView,Bar>();
        private readonly List<MonsterView> removed=new List<MonsterView>();
        private RectTransform root;private Canvas canvas;
        public bool HasVisibleBar(MonsterView view)=>view!=null && bars.TryGetValue(view,out var bar) && bar.Root.gameObject.activeInHierarchy;
        private void Awake()
        {
            canvas=GetComponent<Canvas>();root=ClassicUI.Rect("MonsterHealthBars",transform,0,0);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;root.SetAsFirstSibling();
        }
        private void OnEnable()=>Canvas.willRenderCanvases+=PositionBars;
        private void OnDisable()=>Canvas.willRenderCanvases-=PositionBars;
        private void LateUpdate()
        {
            removed.Clear();foreach(var pair in bars)if(pair.Key==null || !pair.Key.HealthFeedbackVisible)removed.Add(pair.Key);
            foreach(var key in removed){bars[key].Root.gameObject.SetActive(false);Destroy(bars[key].Root.gameObject);bars.Remove(key);}
            foreach(var anchor in WorldLabelAnchor.Active)
            {
                if(anchor.Kind!=WorldLabelAnchor.ActorKind.Monster || !anchor.Visible)continue;
                var view=anchor.GetComponent<MonsterView>();if(view==null || !view.HealthFeedbackVisible || bars.ContainsKey(view))continue;
                var r=ClassicUI.Rect("MonsterHealth_"+view.GetInstanceID(),root,50,10);r.pivot=new Vector2(.5f,1);
                ClassicWindowSkin.Fill("BlackBorder",r,0,0,50,10,Color.black);
                ClassicWindowSkin.Fill("TopBorder",r,1,1,48,1,Color.white);ClassicWindowSkin.Fill("BottomBorder",r,1,8,48,1,Color.white);
                ClassicWindowSkin.Fill("LeftBorder",r,1,2,1,6,Color.white);ClassicWindowSkin.Fill("RightBorder",r,48,2,1,6,Color.white);
                var bar=new Bar{Root=r,Fill=ClassicWindowSkin.Fill("Health",r,3,3,44,3,Color.green).rectTransform,
                    Shade=ClassicWindowSkin.Fill("HealthShade",r,3,6,44,1,new Color(0,.5f,0)).rectTransform};bars.Add(view,bar);
            }
            PositionBars();
        }
        private void PositionBars()
        {
            var camera=Camera.main;if(camera==null || canvas==null || root==null)return;
            foreach(var pair in bars)
            {
                var view=pair.Key;var bar=pair.Value;if(view==null || bar.Root==null)continue;
                var screen=camera.WorldToScreenPoint(view.HeadPosition);
                bool visible=view.HealthFeedbackVisible && screen.z>0 && camera.pixelRect.Contains(screen);
                bar.Root.gameObject.SetActive(visible);if(!visible)continue;
                var point=ClassicSpriteFrames.CanvasPoint(root,canvas,screen);
                bar.Root.anchoredPosition=new Vector2(Mathf.Round(point.x),Mathf.Round(point.y+30));
                int percent=view.Model.MaxHP>0?(int)((long)view.Model.HP*100/view.Model.MaxHP):0;
                int width=44*Mathf.Clamp(percent,0,100)/100;
                bar.Fill.sizeDelta=new Vector2(width,3);bar.Shade.sizeDelta=new Vector2(width,1);
            }
        }
    }
}
