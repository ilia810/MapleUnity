using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
namespace MapleClient.GameView.UI
{
    public class ExperienceBar : MonoBehaviour
    {
        private Player player;
        private Image missing, missingBacking;
        private Text text;
        private Font font;
        private void Start()
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 11);
            var root = ClassicUI.Rect("ExperienceBar", ClassicUI.Hud(transform), 115, 14); ClassicUI.Place(root, 440, 53, 115, 14);
            ClassicUI.GaugeInterior("Fill",root,0,0,115,new Color(.78f,1,0));
            missingBacking=ClassicUI.Art("MissingBacking",root,"StatusBar.img/gauge/gray",0,0);
            missingBacking.color=new Color(.3f,.3f,.3f);
            missing = ClassicUI.GaugeInterior("Missing",root,0,0,115,Color.white);
            text = ClassicUI.Text("Text", root, font, "", 11, true); text.alignment = TextAnchor.MiddleLeft;
            ClassicUI.Place(text.rectTransform, 22, -15, 94, 13);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-1);shadow.effectColor=new Color(0,0,0,.65f);
            text.resizeTextForBestFit=true; text.resizeTextMinSize=9; text.resizeTextMaxSize=11;
            StartCoroutine(WaitForPlayer());
        }
        private System.Collections.IEnumerator WaitForPlayer()
        {
            GameManager manager;
            while ((manager = FindFirstObjectByType<GameManager>()) == null || manager.Player == null) yield return null;
            SetPlayer(manager.Player);
        }
        public void SetPlayer(Player value) { player = value; Update(); }
        private void Update()
        {
            if (player == null || missing == null) return;
            float ratio = player.ExperienceToNextLevel > 0 ? Mathf.Clamp01((float)((double)player.Experience / player.ExperienceToNextLevel)) : 1;
            float filled=Mathf.Round(115*ratio);
            ClassicUI.Place(missing.rectTransform, filled, 0, 115-filled, 14);
            ClassicUI.Place(missingBacking.rectTransform, filled, 0, 115-filled, 14);
            missing.enabled = ratio < 1;
            missingBacking.enabled=missing.enabled;
            text.text = player.ExperienceToNextLevel > 0 ? $"{player.Experience} [{ratio * 100:F2}%]" : "MAX LEVEL";
        }
        private void OnDestroy() { if (font != null) Destroy(font); }
    }
}
