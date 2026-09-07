using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Original regional atlas, with authored links and location markers. Browsing never moves the player.</summary>
    public sealed class ClassicWorldMapView : MonoBehaviour
    {
        private GameWorld world;
        private RectTransform panel, body;
        private Font font;
        private ClassicTooltipView tooltip;
        private string region;
        public bool Visible => panel != null && panel.gameObject.activeSelf;
        public string Region => region;
        private INxFile Maps => NXAssetLoader.Instance.GetNxFile("map");
        public void Toggle(GameWorld value)
        {
            world = value;
            if (Visible) { Close(); return; }
            if (panel == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial",12);
                tooltip = ClassicTooltipView.For(transform);
                panel = ClassicUI.Window("WorldMapPanel",transform,654,521);
                panel.GetComponent<ClassicWindow>().Configure(new Vector2(.5f,.58f),Close);
            }
            panel.gameObject.SetActive(true); ShowCurrent(); panel.GetComponent<ClassicWindow>().Focus();
        }
        private void Close() { tooltip.Hide(); panel.gameObject.SetActive(false); }
        public void RefreshLocation() { if (Visible) Browse(region); }
        public void ShowCurrent()
        {
            int id = world?.CurrentMap?.MapId ?? -1;
            // Choose the most detailed authored atlas that contains this map.
            var found = NxWorldMapIndex.FindRegion(Maps?.GetNode("WorldMap"),id);
            Browse(found?.Name ?? "WorldMap.img");
        }
        public void Browse(string name)
        {
            name = name.EndsWith(".img") ? name : name+".img";
            var node = Maps?.GetNode("WorldMap/"+name);
            if (node == null) return;
            var baseSprite = ClassicMinimapView.MapSprite("WorldMap/"+name+"/BaseImg/0");
            if (baseSprite == null) return;
            region = name; tooltip.Hide();
            if (body != null) { body.gameObject.SetActive(false); Destroy(body.gameObject); }
            body = ClassicUI.Rect("WorldMapContents",panel,654,521); ClassicUI.Place(body,0,0,654,521);
            Part(0,0,0,7,33); Part(1,7,0,640,33); Part(2,647,0,7,32);
            Part(3,0,33,7,470); Part(4,647,33,7,470);
            Part(5,0,503,7,18); Part(6,7,503,640,18); Part(7,647,503,7,18);
            ClassicUI.Art("WorldMapTitle",body,"UIWindow.img/WorldMap/title",8,9);
            ClassicUI.DragTitle(body,panel,654);
            ClassicUI.ArtButton("CloseWorldMap",body,"Basic.img/BtClose",633,6,Close);
            ClassicWindowSkin.Action("WorldMapCurrent",body,font,"My location",544,5,82,ShowCurrent);
            var paper = ClassicWindowSkin.Fill("WorldMapImage",body,7,33,640,470,Color.white,true); paper.sprite = baseSprite;
            var imageRoot = paper.rectTransform; imageRoot.gameObject.AddComponent<RectMask2D>();
            var origin = MapleClient.GameData.SpriteLoader.GetOrigin(node["BaseImg"]?["0"]);
            foreach (var spot in node["MapList"]?.Children ?? Enumerable.Empty<INxNode>())
            {
                var ids = (spot["mapNo"]?.Children ?? Enumerable.Empty<INxNode>()).Select(n=>n.GetValue<int>()).ToArray();
                var point = origin + (spot["spot"]?.GetValue<Vector2>() ?? Vector2.zero);
                string markerPath = "MapHelper.img/worldMap/mapImage/" + (spot["type"]?.GetValue<int>() ?? 0);
                var marker = AtPoint("WorldMapSpot_"+spot.Name,imageRoot,markerPath,point); marker.raycastTarget = true;
                string[] names = ids.Select(id=>NxMapNames.Name(NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Map.img"),id)).Distinct().ToArray();
                var hover = marker.gameObject.AddComponent<ClassicHover>();
                hover.Enter = e => tooltip.ShowText(marker,e.position,names.FirstOrDefault() ?? "Map",string.Join("\n",names.Skip(1)));
                hover.Exit = () => tooltip.Hide(marker);
                if (ids.Contains(world?.CurrentMap?.MapId ?? -1))
                    AtPoint("WorldMapCurrentPosition",imageRoot,"MapHelper.img/worldMap/curPos/0",point);
            }
            foreach (var link in node["MapLink"]?.Children ?? Enumerable.Empty<INxNode>())
            {
                string target = link["link"]?["linkMap"]?.GetValue<string>(); if (string.IsNullOrEmpty(target)) continue;
                string path = "WorldMap/"+name+"/MapLink/"+link.Name+"/link/linkImg";
                var highlight = AtPoint("WorldMapLink_"+link.Name,imageRoot,path,origin);
                highlight.raycastTarget = true; highlight.color = Color.clear;
                var button = highlight.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
                button.onClick.AddListener(()=>Browse(target));
                var hover = highlight.gameObject.AddComponent<ClassicHover>();
                string title = link["toolTip"]?.GetValue<string>() ?? "View region";
                hover.Enter = e => { highlight.color = Color.white; tooltip.ShowText(highlight,e.position,title,"Click to view this region."); };
                hover.Exit = () => { highlight.color = Color.clear; tooltip.Hide(highlight); };
            }
            string parent = node["info"]?["parentMap"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(parent)) ClassicWindowSkin.Action("WorldMapParent",body,font,"Overview",10,502,80,()=>Browse(parent));
            var caption = ClassicWindowSkin.Label("WorldMapLocation",body,font,world?.CurrentMap?.StreetName+" : "+world?.CurrentMap?.Name,98,503,544,17,11);
            caption.supportRichText = false;
        }
        private Image AtPoint(string name, Transform parent, string path, Vector2 point)
        {
            var offset = MapleClient.GameData.SpriteLoader.GetOrigin(Maps?.GetNode(path));
            return ClassicMinimapView.MapArt(name,parent,path,point.x-offset.x,point.y-offset.y);
        }
        private void Part(int part,float x,float y,float w,float h)
        {
            var image = ClassicUI.Art("Border"+part,body,"UIWindow.img/WorldMap/Border/"+part,x,y);
            image.rectTransform.sizeDelta = new Vector2(w,h); image.raycastTarget = true;
        }
        private void OnDestroy() { if (font != null) Destroy(font); }
    }
}
