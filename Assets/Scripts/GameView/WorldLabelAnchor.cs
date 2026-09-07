using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>A view anchor shared by nameplates and minimap markers. Never inherits sprite flipping.</summary>
    public sealed class WorldLabelAnchor : MonoBehaviour
    {
        public enum ActorKind { None, Player, Npc, Monster }
        private static readonly HashSet<WorldLabelAnchor> active = new HashSet<WorldLabelAnchor>();
        public static IEnumerable<WorldLabelAnchor> Active => active;
        public ActorKind Kind { get; private set; }
        public int NpcId { get; private set; }
        private bool showNpcName = true;
        private MonsterView monsterView;
        public bool ShowName => monster != null ? monsterView != null && monsterView.HealthFeedbackVisible : showNpcName;
        private Player player;
        private Monster monster;
        private string npcName, service;
        public string Label => player != null ? player.Name : monster != null ? $"Lv. {monster.Template.Level} {monster.Name}" : npcName;
        public string Service => service ?? "";
        public bool Visible => isActiveAndEnabled && Kind != ActorKind.None && monster?.IsDead != true;
        public Vector3 Feet => transform.position + (Kind == ActorKind.Player ? Vector3.down * Player.Height / 2 : Vector3.zero);

        public void Bind(Player value) { player = value; Kind = ActorKind.Player; }
        public void Bind(Monster value) { monster = value; monsterView = GetComponent<MonsterView>(); Kind = ActorKind.Monster; }
        public void BindNpc(int id)
        {
            NpcId = id; Kind = ActorKind.Npc;
            var strings = NXAssetLoader.Instance.GetNxFile("string")?.GetNode("Npc.img/" + id);
            npcName = strings?["name"]?.GetValue<string>() ?? $"NPC {id}";
            service = strings?["func"]?.GetValue<string>() ?? "";
            var file = NXAssetLoader.Instance.GetNxFile("npc");
            var seen = new HashSet<int>(); int artId = id; INxNode info = null;
            while (seen.Add(artId))
            {
                info = file?.GetNode(artId.ToString("D7")+".img/info");
                var link = info?["link"]?.Value?.ToString();
                if (string.IsNullOrEmpty(link) || !int.TryParse(link.Replace(".img",""),out artId)) break;
            }
            showNpcName = (info?["hideName"]?.GetValue<int>() ?? 0) == 0;
        }
        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);
        private void OnDestroy() => active.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
    }
}
