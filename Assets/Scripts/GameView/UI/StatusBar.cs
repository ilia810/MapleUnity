using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameView.UI
{
    public class StatusBar : MonoBehaviour
    {
        private Player player;
        private Font font;
        private Image hpMissing, mpMissing, hpMissingBacking, mpMissingBacking;
        private Text hpText, mpText, levelText, nameText, jobText;
        private ClassicMessageHistory messages;
        private readonly List<(string Text, Color Color, bool Attention)> pendingMessages = new List<(string, Color, bool)>();
        private void Start()
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            var hud = ClassicUI.Hud(transform);
            ClassicUI.GaugeInterior("HPFill",hud,219,53,105,new Color(1,0,0));
            ClassicUI.GaugeInterior("MPFill",hud,327,53,105,new Color(0,.62f,1));
            hpMissing = Missing(hud, "HPMissing", 219, out hpMissingBacking);
            mpMissing = Missing(hud, "MPMissing", 327, out mpMissingBacking);
            ClassicUI.Art("Graduation", hud, "StatusBar.img/gauge/graduation", 217, 38);
            hpText = Label(hud, "HP Text", 236, 38, 88, 13);
            mpText = Label(hud, "MP Text", 345, 38, 87, 13);
            hpText.alignment = mpText.alignment = TextAnchor.MiddleLeft;
            levelText = Label(hud, "Level Text", 33, 40, 44, 28); levelText.fontSize = 18; levelText.fontStyle = FontStyle.Bold;
            levelText.gameObject.AddComponent<ClassicNumberLabel>().Bind(levelText, "Basic.img/LevelNo");
            messages = hud.gameObject.AddComponent<ClassicMessageHistory>(); messages.Initialize(font);
            foreach (var entry in pendingMessages) messages.Post(entry.Text, entry.Color, entry.Attention); pendingMessages.Clear();
            jobText = Label(hud,"CharacterJob",83,37,125,14);
            nameText = Label(hud, "CharacterName", 83,51,125,14);
            jobText.alignment = nameText.alignment = TextAnchor.MiddleLeft;
            foreach (var label in new[] {jobText, nameText})
            { label.fontSize = 12; label.resizeTextForBestFit = true; label.resizeTextMinSize = 10; label.resizeTextMaxSize = 12; }
            ClassicUI.HudButton("CashShop",hud,"StatusBar.img/BtShop",572,
                () => GetComponent<PlayerCombatFeedback>()?.ShowActionMessage("Cash Shop is unavailable in local play."));
            ClassicUI.HudButton("Shortcuts",hud,"StatusBar.img/BtShort",724,() => GetComponent<SkillBar>()?.ShowShortcuts());
            ClassicUI.ArtButton("QuickslotToggle",hud,"StatusBar.img/QuickSlotD",766,5,() => GetComponent<SkillBar>()?.ToggleVisible());
            StartCoroutine(WaitForPlayer());
        }
        private System.Collections.IEnumerator WaitForPlayer()
        {
            GameManager manager;
            while ((manager = FindFirstObjectByType<GameManager>()) == null || manager.Player == null) yield return null;
            SetPlayer(manager.Player);
        }
        private Image Missing(Transform parent, string name, int x, out Image backing)
        {
            // The original flash gradient is translucent; block the colored fill before drawing it.
            backing=ClassicUI.Art(name+"Backing",parent,"StatusBar.img/gauge/gray",x,53);
            backing.rectTransform.sizeDelta=new Vector2(105,14);backing.color=new Color(.3f,.3f,.3f);
            backing.type=Image.Type.Filled;backing.fillMethod=Image.FillMethod.Horizontal;backing.fillOrigin=(int)Image.OriginHorizontal.Right;
            var i = ClassicUI.GaugeInterior(name,parent,x,53,105,Color.white);
            i.type = Image.Type.Filled; i.fillMethod = Image.FillMethod.Horizontal; i.fillOrigin = (int)Image.OriginHorizontal.Right; return i;
        }
        public void PostMessage(string value) => PostMessage(value, Color.white);
        public void PostMessage(string value, Color color, bool attention = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (messages != null) messages.Post(value, color, attention);
            else { if (pendingMessages.Count == 50) pendingMessages.RemoveAt(0); pendingMessages.Add((value, color, attention)); }
        }
        // Explorer names follow HeavenClient Character/Job.cpp; expanded names retain the existing UI wording.
        public static string JobName(int job)
        {
            switch(job)
            {
                case 0:return "Beginner"; case 100:return "Swordsman";
                case 110:return "Fighter"; case 111:return "Crusader"; case 112:return "Hero";
                case 120:return "Page"; case 121:return "White Knight"; case 122:return "Paladin";
                case 130:return "Spearman"; case 131:return "Dragon Knight"; case 132:return "Dark Knight";
                case 200:return "Magician"; case 210:return "Wizard (Fire/Poison)"; case 211:return "Mage (Fire/Poison)"; case 212:return "Archmage (Fire/Poison)";
                case 220:return "Wizard (Ice/Lightning)"; case 221:return "Mage (Ice/Lightning)"; case 222:return "Archmage (Ice/Lightning)";
                case 230:return "Cleric"; case 231:return "Priest"; case 232:return "Bishop";
                case 300:return "Bowman"; case 310:return "Hunter"; case 311:return "Ranger"; case 312:return "Bowmaster";
                case 320:return "Crossbowman"; case 321:return "Sniper"; case 322:return "Marksman";
                case 400:return "Thief"; case 410:return "Assassin"; case 411:return "Hermit"; case 412:return "Nightlord";
                case 420:return "Bandit"; case 421:return "Chief Bandit"; case 422:return "Shadower";
                case 500:return "Pirate"; case 510:return "Brawler"; case 511:return "Marauder"; case 512:return "Buccaneer";
                case 520:return "Gunslinger"; case 521:return "Outlaw"; case 522:return "Corsair";
                default:return "Job "+job;
            }
        }
        public static string JobFamily(int job)
        {
            switch(job/100)
            {case 1:return "Warrior";case 2:return "Magician";case 3:return "Bowman";case 4:return "Thief";case 5:return "Pirate";default:return "Beginner";}
        }
        public static string SkillBookName(int job)=>job/10==21?"Fire & Poison Basics":job/10==22?"Ice & Lightning Basics":JobName(job)+" Basics";
        private Text Label(Transform parent, string name, int x, int y, int w, int h)
        {
            var t = ClassicUI.Text(name, parent, font, "", 11, true); t.alignment = TextAnchor.MiddleCenter;
            ClassicUI.Place(t.rectTransform, x, y, w, h); var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectDistance = new Vector2(0, -1);shadow.effectColor = new Color(0,0,0,.65f); return t;
        }
        public void SetPlayer(Player value) { player = value; Update(); }
        private void Update()
        {
            if (player == null || hpMissing == null) return;
            hpMissing.fillAmount = 1 - Mathf.Clamp01(player.MaxHP > 0 ? (float)player.CurrentHP / player.MaxHP : 0);
            mpMissing.fillAmount = 1 - Mathf.Clamp01(player.MaxMP > 0 ? (float)player.CurrentMP / player.MaxMP : 0);
            hpMissingBacking.fillAmount=hpMissing.fillAmount;mpMissingBacking.fillAmount=mpMissing.fillAmount;
            hpText.text = $"[{player.CurrentHP}/{player.MaxHP}]"; mpText.text = $"[{player.CurrentMP}/{player.MaxMP}]";
            levelText.text = player.Level.ToString(); nameText.text = player.Name; jobText.text=JobName(player.JobId);
            ClassicMessageText.Fit(hpText,hpText.text,false);ClassicMessageText.Fit(mpText,mpText.text,false);
            ClassicMessageText.Fit(nameText,nameText.text,false);
        }
        private void OnDestroy() { if (font != null) Destroy(font); }
    }
}
