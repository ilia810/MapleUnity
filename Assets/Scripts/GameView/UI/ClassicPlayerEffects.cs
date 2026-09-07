using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using UnityEngine;

namespace MapleClient.GameView.UI
{
    /// <summary>Independent original one-shots follow the player's feet and never replay on load.</summary>
    public sealed class ClassicPlayerEffects : MonoBehaviour
    {
        public enum Kind {LevelUp,JobChanged,QuestClear,Buff}
        private sealed class Track
        {
            public ClassicSpriteFrames Frames;public SpriteRenderer Renderer;public float Age;public bool Playing;
        }
        private readonly Dictionary<Kind,Track> tracks=new Dictionary<Kind,Track>();
        private GameWorld world;
        // Preserve the existing level-up presentation contract.
        public bool Playing=>IsPlaying(Kind.LevelUp);
        public SpriteRenderer Renderer=>RendererFor(Kind.LevelUp);
        public bool IsPlaying(Kind kind)=>tracks.TryGetValue(kind,out var t)&&t.Playing;
        public SpriteRenderer RendererFor(Kind kind)=>tracks.TryGetValue(kind,out var t)?t.Renderer:null;
        public void Bind(GameWorld value)
        {
            Unbind();world=value;if(world==null)return;
            world.Player.LeveledUp+=Level;world.FirstJobAdvanced+=Job;world.QuestCompleted+=Quest;
            world.Player.ItemUsed+=Item;world.MapLoaded+=MapChanged;
        }
        private void Level(int level)=>Play(Kind.LevelUp);
        private void Job(int job)
        {
            Play(Kind.JobChanged);
            GetComponent<ClassicNotifications>()?.Post("Job advanced: "+OfflineProgression.Job(job)?.Name+"!",ClassicNotifications.ExperienceColor);
        }
        private void Quest(int quest)
        {
            Play(Kind.QuestClear);
            GetComponent<ClassicNotifications>()?.Post("Quest completed: "+world.Quests.Get(quest)?.Name+".",ClassicNotifications.ExperienceColor);
        }
        private void Item(int id)
        {
            // Skills already play their own cast art. Use the generic burst only for a
            // successfully consumed stat-buff item, never for expiry, cancellation or loading.
            if(world.Player.GetItemInfo(id)?.IsStatBuffConsumable==true)Play(Kind.Buff);
        }
        private void Play(Kind kind)
        {
            if(!tracks.TryGetValue(kind,out var track))
            {
                track=new Track{Frames=new ClassicSpriteFrames("effect","BasicEff.img/"+kind)};tracks.Add(kind,track);
            }
            track.Age=0;track.Playing=track.Frames.Frames.Length>0;
        }
        private void Update()
        {
            foreach(var t in tracks.Values)if(t.Playing){t.Age+=Time.deltaTime;if(t.Age*1000>=t.Frames.Duration)Stop(t);}
        }
        private void LateUpdate()
        {
            if(world==null || !tracks.Values.Any(t=>t.Playing))return;
            var anchor=WorldLabelAnchor.Active.FirstOrDefault(a=>a.Visible && a.Kind==WorldLabelAnchor.ActorKind.Player);
            if(anchor==null)return;
            foreach(var pair in tracks)
            {
                var t=pair.Value;if(!t.Playing)continue;
                if(t.Renderer==null)t.Renderer=new GameObject("Player"+pair.Key).AddComponent<SpriteRenderer>();
                if(t.Renderer.transform.parent!=anchor.transform)t.Renderer.transform.SetParent(anchor.transform,false);
                var frame=t.Frames.Sample(t.Age);t.Renderer.enabled=true;t.Renderer.sprite=frame.Sprite;
                t.Renderer.transform.localPosition=new Vector3(-frame.Origin.x/100,-Player.Height/2+frame.Origin.y/100,0);
                t.Renderer.sortingLayerName=StageRenderOrder.SortingLayerName;
                t.Renderer.sortingOrder=StageRenderOrder.PlayerOrder(world.Player.CurrentFootholdLayer)+(pair.Key==Kind.JobChanged?-1:1);
            }
        }
        private static void Stop(Track t){t.Playing=false;if(t.Renderer!=null){t.Renderer.enabled=false;t.Renderer.sprite=null;}}
        private void Clear(){foreach(var t in tracks.Values)Stop(t);}
        private void MapChanged(MapleClient.GameLogic.MapData map)=>Clear();
        private void Unbind()
        {
            if(world!=null)
            {
                world.Player.LeveledUp-=Level;world.FirstJobAdvanced-=Job;world.QuestCompleted-=Quest;
                world.Player.ItemUsed-=Item;world.MapLoaded-=MapChanged;
            }
            Clear();
        }
        private void OnDisable()=>Clear();
        private void OnDestroy(){Unbind();foreach(var t in tracks.Values)if(t.Renderer!=null)Destroy(t.Renderer.gameObject);}
    }
}
