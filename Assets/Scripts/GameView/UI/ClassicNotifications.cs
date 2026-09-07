using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Bounded, nonblocking event messages above the right edge of the HUD.</summary>
    public sealed class ClassicNotifications : MonoBehaviour
    {
        private sealed class Entry {public Text Text;public CanvasGroup Group;public float Age, Width = -1;public string Message;}
        private readonly List<Entry> entries=new List<Entry>();
        private RectTransform root;private Font font;
        public int Count=>entries.Count;
        public static readonly Color ExperienceColor=new Color32(255,221,64,255);
        private void Awake()
        {
            font=Font.CreateDynamicFontFromOSFont("Arial",13);root=ClassicUI.Rect("EventNotifications",transform,360,0);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,0);
        }
        public void Post(string message,Color color)
        {
            if(string.IsNullOrWhiteSpace(message))return;
            while(entries.Count>=6)Remove(0);
            var text=ClassicUI.Text("EventNotification",root,font,message,13);text.supportRichText=false;text.color=color;
            text.alignment=TextAnchor.UpperRight;text.verticalOverflow=VerticalWrapMode.Truncate;
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);
            var group=text.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            entries.Add(new Entry{Text=text,Group=group,Message=message});Layout();
        }
        private void Update()
        {
            for(int i=entries.Count-1;i>=0;i--){entries[i].Age+=Time.unscaledDeltaTime;entries[i].Group.alpha=Mathf.Clamp01(6-entries[i].Age);if(entries[i].Age>=6)Remove(i);}
        }
        private void LateUpdate()=>Layout();
        private void Layout()
        {
            if(root==null)return;var canvas=(RectTransform)transform;float width=Mathf.Min(360,canvas.rect.width-16);
            root.anchoredPosition=new Vector2(-8,Mathf.Ceil(154*Mathf.Min(1,canvas.rect.width/800))+8);
            float y=0;
            for(int i=entries.Count-1;i>=0;i--)
            {
                var entry=entries[i];var text=entry.Text;text.rectTransform.sizeDelta=new Vector2(width,40);
                if(entry.Width!=width){entry.Width=width;ClassicMessageText.Fit(text,entry.Message,true);}
                float h=Mathf.Min(40,Mathf.Ceil(text.preferredHeight));var r=text.rectTransform;
                r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(width,h);y+=h+2;
            }
            root.sizeDelta=new Vector2(width,y);
        }
        private void Remove(int index){entries[index].Text.gameObject.SetActive(false);Destroy(entries[index].Text.gameObject);entries.RemoveAt(index);}
        public void Clear(){while(entries.Count>0)Remove(0);}
        private void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
