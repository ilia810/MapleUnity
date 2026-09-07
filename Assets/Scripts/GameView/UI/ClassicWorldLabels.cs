using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Screen-sized type at interpolated feet positions, behind windows and the HUD.</summary>
    // CameraBounds clamps at 900, after the ordinary follow camera. Project only after that clamp.
    [DefaultExecutionOrder(1100)]
    public sealed class ClassicWorldLabels : MonoBehaviour
    {
        private sealed class Plate { public RectTransform Root; public Text Name, Service; public string Key; }
        private readonly Dictionary<WorldLabelAnchor, Plate> plates = new Dictionary<WorldLabelAnchor, Plate>();
        private readonly List<WorldLabelAnchor> removed = new List<WorldLabelAnchor>();
        private readonly List<Rect> occupied=new List<Rect>();
        private RectTransform root;
        private Canvas canvas;
        private Font font;
        private void OnEnable() => Canvas.willRenderCanvases += PositionPlates;
        private void OnDisable() => Canvas.willRenderCanvases -= PositionPlates;
        private void Awake()
        {
            canvas = GetComponent<Canvas>(); font = Font.CreateDynamicFontFromOSFont("Arial", 13);
            root = ClassicUI.Rect("WorldNameplates", transform, 0, 0);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
        }
        private void LateUpdate()
        {
            var camera = Camera.main; if (camera == null) return;
            removed.Clear();
            foreach (var entry in plates) if (entry.Key == null || !entry.Key.Visible || !entry.Key.ShowName) removed.Add(entry.Key);
            foreach (var actor in removed) { Destroy(plates[actor].Root.gameObject); plates.Remove(actor); }
            foreach (var actor in WorldLabelAnchor.Active)
            {
                if (!actor.Visible || !actor.ShowName) continue;
                if (!plates.TryGetValue(actor, out var plate))
                {
                    var r = ClassicUI.Rect("Nameplate_" + actor.Kind + "_" + actor.GetInstanceID(), root, 1, 34);
                    r.pivot = new Vector2(.5f, 1);
                    plate = new Plate { Root = r };
                    var color = actor.Kind == WorldLabelAnchor.ActorKind.Npc ? new Color32(242,231,80,255) : (Color32)Color.white;
                    plate.Name = Line("ActorName", r, color, 0);
                    plate.Service = Line("NpcService", r, color, 17);
                    if (actor.Kind == WorldLabelAnchor.ActorKind.Npc) plate.Name.fontStyle = plate.Service.fontStyle = FontStyle.Bold;
                    plates.Add(actor, plate);
                }
                string key = actor.Label + "\n" + actor.Service;
                if (key != plate.Key)
                {
                    plate.Key = key; plate.Name.text = actor.Label; plate.Service.text = actor.Service;
                    Size(plate.Name); Size(plate.Service);
                    bool service=!string.IsNullOrEmpty(actor.Service);
                    plate.Service.transform.parent.gameObject.SetActive(service);
                    float width=Mathf.Max(plate.Name.preferredWidth,service?plate.Service.preferredWidth:0)+6;
                    plate.Root.sizeDelta=new Vector2(Mathf.Ceil(width),service?34:17);
                }
            }
            PositionPlates();
        }
        private void PositionPlates()
        {
            var camera = Camera.main;
            if (root == null || canvas == null || camera == null) return;
            var pixels = canvas.pixelRect;
            if (pixels.width <= 0 || pixels.height <= 0) return;
            occupied.Clear();
            // NPC names are landmarks: reserve their anchored rectangles first. Only
            // player and monster labels may move to avoid them.
            foreach (var entry in plates.OrderBy(p=>p.Key==null?int.MaxValue:p.Key.Kind==WorldLabelAnchor.ActorKind.Npc?0:(int)p.Key.Kind)
                .ThenBy(p=>p.Key==null?0:p.Key.NpcId).ThenBy(p=>p.Key==null?0:p.Key.GetInstanceID()))
            {
                var actor = entry.Key; var plate = entry.Value;
                if (actor == null || plate.Root == null) continue;
                var screen = camera.WorldToScreenPoint(actor.Feet);
                bool onscreen = actor.Visible && actor.ShowName && screen.z > 0 && camera.pixelRect.Contains(screen);
                plate.Root.gameObject.SetActive(onscreen);
                if (!onscreen) continue;
                // A camera-space canvas may not have updated its world transform after the camera
                // clamp yet. Map pixel coordinates directly to its local rect, without that transform.
                var point = new Vector2(root.rect.xMin + (screen.x-pixels.xMin)/pixels.width*root.rect.width,
                    root.rect.yMin + (screen.y-pixels.yMin)/pixels.height*root.rect.height);
                float width=plate.Root.rect.width,height=plate.Root.rect.height;
                float x=Mathf.Round(Mathf.Clamp(point.x,root.rect.xMin+width/2+2,root.rect.xMax-width/2-2));
                float y=Mathf.Round(Mathf.Clamp(point.y-2,root.rect.yMin+height+2,root.rect.yMax-2));
                var bounds=new Rect(x-width/2,y-height,width,height);
                int row=0;
                bool fixedNpc=actor.Kind==WorldLabelAnchor.ActorKind.Npc;
                while(!fixedNpc && occupied.Any(r=>r.Overlaps(bounds)) && row++<6)bounds.y-=18;
                bool fits=fixedNpc || bounds.yMin>=root.rect.yMin+2 && !occupied.Any(r=>r.Overlaps(bounds));
                plate.Root.gameObject.SetActive(fits);
                if(!fits)continue;
                plate.Root.anchoredPosition=new Vector2(x,bounds.yMax);
                bounds.xMin-=2;bounds.xMax+=2;bounds.yMin-=1;bounds.yMax+=1;occupied.Add(bounds);
            }
        }
        private Text Line(string name, Transform parent, Color color, float y)
        {
            var back = ClassicWindowSkin.Fill(name + "Background", parent, 0, y, 1, 17, new Color(0,0,0,.65f));
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = back.rectTransform.pivot = new Vector2(.5f,1);
            var text = ClassicWindowSkin.Label(name, back.transform, font, "", 3, 0, 1, 17, 13, color);
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter; return text;
        }
        private static void Size(Text text)
        {
            float width = Mathf.Ceil(text.preferredWidth);
            text.rectTransform.sizeDelta = new Vector2(width,17);
            ((RectTransform)text.transform.parent).sizeDelta = new Vector2(width+6,17);
        }
        private void OnDestroy() { if (font != null) Destroy(font); }
    }
}
