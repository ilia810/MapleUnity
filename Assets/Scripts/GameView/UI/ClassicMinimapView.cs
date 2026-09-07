using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.GameView.UI
{
    public sealed class ClassicMinimapView : MonoBehaviour
    {
        private GameWorld world;
        private MapData map;
        private RectTransform root, content, viewport;
        private Image playerMarker;
        private Font font;
        private ClassicTooltipView tooltip;
        private readonly Dictionary<WorldLabelAnchor, Image> npcs = new Dictionary<WorldLabelAnchor, Image>();
        private readonly List<WorldLabelAnchor> removed = new List<WorldLabelAnchor>();
        public int Mode { get; private set; } = 2;
        public int ShownMapId => map?.MapId ?? -1;
        public int NpcMarkerCount => npcs.Count;
        public Vector2 PlayerPoint { get; private set; }

        public void Bind(GameWorld value)
        {
            if (world != null) world.MapLoaded -= OnMapLoaded;
            world = value; world.MapLoaded += OnMapLoaded;
            if (root == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 12);
                tooltip = ClassicTooltipView.For(transform);
                root = ClassicUI.Rect("MiniMapPanel", transform, 200, 20);
                ClassicUI.Place(root, 1, 1, 200, 20);
                root.gameObject.AddComponent<ClassicMinimapBounds>();
            }
            if (world.CurrentMap != null) OnMapLoaded(world.CurrentMap);
        }
        public void SetMode(int value) { Mode = Mathf.Clamp(value, 0, 2); Rebuild(); }
        public void OpenWorldMap()
        {
            var view = GetComponent<ClassicWorldMapView>() ?? gameObject.AddComponent<ClassicWorldMapView>();
            view.Toggle(world);
        }
        private void OnMapLoaded(MapData value)
        {
            map = value; Rebuild();
            GetComponent<ClassicWorldMapView>()?.RefreshLocation();
        }
        private void Rebuild()
        {
            if (root == null || map == null) return;
            tooltip.Hide();
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            npcs.Clear(); playerMarker = null; content = viewport = null;
            string path = $"Map/Map{map.MapId / 100000000}/{map.MapId:D9}.img/miniMap/canvas";
            var mini = map.MiniMap;
            var sprite = mini != null && mini.HasCanvas && !mini.Hidden ? MapSprite(path) : null;
            int displayMode = sprite == null ? 0 : Mode;
            string location = string.IsNullOrEmpty(map.StreetName) ? map.Name : map.StreetName+" : "+map.Name;
            float width = Mathf.Clamp(sprite != null ? sprite.rect.width + 12 : 200, displayMode == 1 ? 140 : 180, 300);
            // Small interior previews still need room for their complete location names.
            foreach (string title in displayMode == 1 ? System.Array.Empty<string>() : displayMode == 0 ? new[] {location ?? ""} : new[] {map.StreetName ?? "", map.Name ?? ""})
            {
                font.RequestCharactersInTexture(title,12,FontStyle.Bold); float textWidth = 0;
                foreach (char c in title) if (font.GetCharacterInfo(c,out var glyph,12,FontStyle.Bold)) textWidth += glyph.advance;
                width = Mathf.Max(width,Mathf.Min(displayMode == 0 ? 400 : 300,textWidth+(displayMode == 0 ? 78 : 59)));
            }
            float top = displayMode == 2 ? 72 : 29, bottom = displayMode == 2 ? 15 : 14;
            float height = displayMode == 0 ? 20 : top + Mathf.Min(180, sprite.rect.height) + bottom;
            root.sizeDelta = new Vector2(width, height);
            string frame = "UIWindow.img/MiniMap/" + (displayMode == 2 ? "MaxMap" : "MinMap") + "/";
            if (displayMode == 0)
            {
                Part("UIWindow.img/MiniMap/Min/w", 0, 0, 8, 20);
                Part("UIWindow.img/MiniMap/Min/c", 8, 0, width-12, 20);
                Part("UIWindow.img/MiniMap/Min/e", width-4, 0, 4, 20);
            }
            else
            {
                Part(frame+"c", 6, top, width-12, height-top-bottom);
                Part(frame+"nw", 0, 0, 6, top); Part(frame+"n", 6, 0, width-12, top); Part(frame+"ne", width-6, 0, 6, top);
                Part(frame+"w", 0, top, 6, height-top-bottom); Part(frame+"e", width-6, top, 6, height-top-bottom);
                Part(frame+"sw", 0, height-bottom, 6, bottom); Part(frame+"s", 6, height-bottom, width-12, bottom); Part(frame+"se", width-6, height-bottom, 6, bottom);
            }
            var drag = ClassicWindowSkin.Fill("MinimapDrag", root, 0, 0, width-72, 20, Color.clear, true);
            drag.gameObject.AddComponent<ClassicWindowDrag>().Window = root;
            if (displayMode == 0)
            {
                var label = ClassicWindowSkin.Label("CollapsedMinimapName",root,font,location,7,3,width-78,17,12);
                label.supportRichText = false;
            }
            else ClassicUI.Art("MinimapTitle", root, "UIWindow.img/MiniMap/title", 7, 8);
            ClassicUI.ArtButton("MinimapMinus", root, "UIWindow.img/SoftKeyboard/Bt/0/BtMin", width-68, 5, () => SetMode(Mode-1)).interactable = sprite != null && Mode > 0;
            ClassicUI.ArtButton("MinimapPlus", root, "UIWindow.img/SoftKeyboard/Bt/0/BtMax", width-55, 5, () => SetMode(Mode+1)).interactable = sprite != null && Mode < 2;
            ClassicUI.ArtButton("WorldMapToggle", root, "UIWindow.img/MiniMap/BtMap", width-41, 5, OpenWorldMap);
            if (displayMode == 0) return;
            if (displayMode == 2)
            {
                var mark = MapArt("MapMark", root, "MapHelper.img/mark/" + mini?.MapMark, 8, 25);
                mark.preserveAspect = true; mark.rectTransform.sizeDelta = new Vector2(38,38);
                var region = ClassicWindowSkin.Label("MinimapRegion", root, font, map.StreetName, 51, 28, width-59, 17, 12, Color.white, true);
                var town = ClassicWindowSkin.Label("MinimapLocation", root, font, map.Name, 51, 45, width-59, 22, 12, Color.white, true);
                foreach (var label in new[] {region,town}) { label.supportRichText = false; var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.3f); shadow.effectDistance = new Vector2(1,-1); }
                var hover = root.Find("MinimapDrag").gameObject.AddComponent<ClassicHover>();
                hover.Enter = e => tooltip.ShowText(root, e.position, map.Name, map.StreetName + "\nTab: change minimap size · M: world map"); hover.Exit = () => tooltip.Hide(root);
            }
            viewport = ClassicUI.Rect("MinimapViewport", root, width-12, height-top-bottom);
            ClassicUI.Place(viewport, 6, top, width-12, height-top-bottom);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = ClassicUI.Rect("MinimapContent", viewport, sprite.rect.width, sprite.rect.height);
            ClassicUI.Place(content, 0, 0, sprite.rect.width, sprite.rect.height);
            var image = ClassicWindowSkin.Fill("MinimapCanvas", content, 0, 0, sprite.rect.width, sprite.rect.height, Color.white);
            image.sprite = sprite;
            foreach (var portal in map.Portals)
            {
                // HeavenClient displays ordinary visible entrances (raw pt == 2), not spawn/hidden/script markers.
                if (portal.Type != PortalType.Regular) continue;
                var marker = Marker("PortalMarker_" + portal.Id, "portal");
                PlaceMarker(marker, mini.ProjectSource(portal.X, portal.Y));
                var hover = marker.gameObject.AddComponent<ClassicHover>(); marker.raycastTarget = true;
                hover.Enter = e => tooltip.ShowText(marker, e.position, NxMapNames.Name(NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Map.img"), portal.TargetMapId), "Portal");
                hover.Exit = () => tooltip.Hide(marker);
            }
            playerMarker = Marker("MinimapPlayer", "user");
        }
        private void LateUpdate()
        {
            if (content == null || world?.Player == null) return;
            var player = world.Player;
            var point = map.MiniMap.ProjectFeet(new LogicVector(player.Position.X, player.Position.Y - Player.Height/2));
            PlayerPoint = new Vector2(point.X, point.Y);
            PlaceMarker(playerMarker, point);
            content.anchoredPosition = new Vector2(CropOffset(point.X,content.rect.width,viewport.rect.width), -CropOffset(point.Y,content.rect.height,viewport.rect.height));
            removed.Clear(); foreach (var pair in npcs) if (pair.Key == null || !pair.Key.Visible) removed.Add(pair.Key);
            foreach (var npc in removed) { tooltip.Hide(npcs[npc]); Destroy(npcs[npc].gameObject); npcs.Remove(npc); }
            foreach (var actor in WorldLabelAnchor.Active)
            {
                if (!actor.Visible || actor.Kind != WorldLabelAnchor.ActorKind.Npc) continue;
                if (!npcs.TryGetValue(actor, out var marker))
                {
                    marker = Marker("NpcMarker_" + actor.NpcId + "_" + actor.GetInstanceID(), "npc");
                    npcs.Add(actor, marker); marker.raycastTarget = true;
                    var owner = marker; var hover = marker.gameObject.AddComponent<ClassicHover>();
                    hover.Enter = e => tooltip.ShowText(owner, e.position, actor.Label, actor.Service); hover.Exit = () => tooltip.Hide(owner);
                }
                PlaceMarker(marker, map.MiniMap.ProjectFeet(new LogicVector(actor.Feet.x, actor.Feet.y)));
            }
            playerMarker.transform.SetAsLastSibling();
        }
        public static float CropOffset(float position, float contentSize, float viewSize) =>
            contentSize <= viewSize ? Mathf.Round((viewSize-contentSize)/2) : -Mathf.Round(Mathf.Clamp(position-viewSize/2,0,contentSize-viewSize));
        private Image Marker(string name, string kind) => MapArt(name, content, "MapHelper.img/minimap/"+kind, 0, 0);
        private static void PlaceMarker(Image image, LogicVector point)
        { image.rectTransform.anchoredPosition = new Vector2(Mathf.Round(point.X-image.rectTransform.rect.width/2), -Mathf.Round(point.Y)); }
        private void Part(string path, float x, float y, float w, float h)
        { var image = ClassicUI.Art(path.Substring(path.LastIndexOf('/')+1), root, path, x,y); image.rectTransform.sizeDelta = new Vector2(w,h); image.raycastTarget = true; }
        internal static Sprite MapSprite(string path) => MapleClient.GameData.SpriteLoader.LoadSprite(NXAssetLoader.Instance.GetNxFile("map")?.GetNode(path), "map/"+path);
        internal static Image MapArt(string name, Transform parent, string path, float x, float y)
        {
            var sprite = MapSprite(path);
            var image = ClassicWindowSkin.Fill(name, parent, x,y,sprite != null ? sprite.rect.width : 0,sprite != null ? sprite.rect.height : 0,Color.white);
            image.sprite = sprite; image.enabled = sprite != null; return image;
        }
        private void OnDestroy() { if(world != null)world.MapLoaded -= OnMapLoaded; if(font != null)Destroy(font); }
    }
    public sealed class ClassicMinimapBounds : MonoBehaviour
    {
        private void LateUpdate()
        {
            var r = (RectTransform)transform; var size = ((RectTransform)r.parent).rect.size;
            r.anchoredPosition = new Vector2(Mathf.Clamp(r.anchoredPosition.x,0,Mathf.Max(0,size.x-r.rect.width)),Mathf.Clamp(r.anchoredPosition.y,-Mathf.Max(0,size.y-r.rect.height-80),0));
        }
    }
}
