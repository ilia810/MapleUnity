using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameView.UI;
using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>Source NPC stances run at authored delays and keep their shared feet origin.</summary>
    public sealed class ClassicNpcAnimator : MonoBehaviour
    {
        private readonly Dictionary<string,ClassicSpriteFrames> clips=new Dictionary<string,ClassicSpriteFrames>();
        private SpriteRenderer art;
        private string[] stances=new string[0];
        private double age;
        private int cycle;
        private bool dialogue,ambient;
        public string Stance {get;private set;}
        public ClassicSpriteFrames.Frame Frame {get;private set;}
        public IReadOnlyList<string> Stances=>stances;
        public Rect PortraitBounds {get;private set;}
        public bool DialogueSpeaking {get=>dialogue;set{if(dialogue==value)return;dialogue=value;RefreshSpeech();}}
        public bool AmbientSpeaking {get=>ambient;set{if(ambient==value)return;ambient=value;RefreshSpeech();}}
        public void Bind(int id,SpriteRenderer renderer)
        {
            art=renderer;clips.Clear();Frame=null;Stance=null;age=0;
            var npcs=NXAssetLoader.Instance.GetNxFile("npc");string path=NxNpcAnimations.ResolvePath(npcs,id);
            foreach(string state in NxNpcAnimations.Stances(npcs,path))
            {
                var clip=new ClassicSpriteFrames("npc",path+"/"+state);
                if(clip.Frames.Length>0)clips.Add(state,clip);
            }
            var frames=clips.Values.SelectMany(c=>c.Frames).ToArray();
            if(frames.Length>0)PortraitBounds=Rect.MinMaxRect(frames.Min(f=>-f.Origin.x),frames.Min(f=>-f.Origin.y),
                frames.Max(f=>f.Sprite.rect.width-f.Origin.x),frames.Max(f=>f.Sprite.rect.height-f.Origin.y));
            stances=clips.Keys.ToArray();cycle=stances.Length==0?0:id%stances.Length;
            if(stances.Length>0)SetStance(clips.ContainsKey("stand")?"stand":stances[0]);
        }
        private string SpeechStance=>clips.ContainsKey("say")?"say":clips.ContainsKey("talk")?"talk":null;
        private void RefreshSpeech()
        {
            if((dialogue||ambient) && SpeechStance!=null)SetStance(SpeechStance);
            else if(clips.ContainsKey("stand"))SetStance("stand");
        }
        private void SetStance(string stance){if(Stance==stance)return;Stance=stance;age=0;PresentAt(0);}
        private void Update()
        {
            if(Stance==null || art==null)return;
            age+=Time.deltaTime;
            var clip=clips[Stance];
            if(age*1000>=clip.Duration)
            {
                age%=clip.Duration/1000.0;
                // The source picks a new authored stance at the end of a loop. A stable per-NPC
                // sequence keeps nearby actors out of sync without inventing movement or frames.
                if(!(dialogue||ambient) && stances.Length>1)
                {
                    cycle=(cycle+1)%stances.Length;SetStance(stances[cycle]);
                }
            }
            PresentAt(age);
        }
        public void PresentAt(double seconds)
        {
            if(art==null || Stance==null)return;
            Frame=clips[Stance].Sample(seconds);art.sprite=Frame.Sprite;
            art.transform.localPosition=new Vector3(-Frame.Origin.x/100,Frame.Origin.y/100,art.transform.localPosition.z);
        }
    }
}
