using UnityEngine;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    /// <summary>Draggable HUD helper. Its preferred position survives temporary viewport clamps.</summary>
    [DefaultExecutionOrder(1250)]
    public sealed class ClassicQuestHelperBounds : MonoBehaviour
    {
        private Vector2 preferred;
        private bool moved,loaded;
        private string Key=>"ClassicUI."+name;
        public void Layout()
        {
            var r=(RectTransform)transform;var canvas=(RectTransform)r.parent;var size=canvas.rect.size;
            if(size.x<=0||size.y<=0)return;
            if(!loaded)
            {
                loaded=true;
                if(!Application.isBatchMode&&PlayerPrefs.HasKey(Key+".x"))
                {preferred=new Vector2(PlayerPrefs.GetFloat(Key+".x"),PlayerPrefs.GetFloat(Key+".y"));moved=true;}
            }
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            var buffs=canvas.Find("ActiveBuffs") as RectTransform;
            float top=buffs!=null&&buffs.rect.width>0?Mathf.Max(16,buffs.rect.height+12):16;
            float bottom=151*Mathf.Min(1,size.x/800f)+8;
            float scale=Mathf.Clamp(Mathf.Min(1,(size.x-16)/r.rect.width,(size.y-bottom-top-8)/r.rect.height),.1f,1);
            r.localScale=Vector3.one*scale;var bounds=r.rect.size*scale;
            var point=moved?Vector2.Scale(preferred,size):new Vector2(size.x-bounds.x-8,top);
            point.x=Mathf.Clamp(point.x,8,Mathf.Max(8,size.x-bounds.x-8));
            point.y=Mathf.Clamp(point.y,top,Mathf.Max(top,size.y-bottom-bounds.y));
            r.anchoredPosition=new Vector2(point.x,-point.y);
        }
        public void MoveBy(Vector2 screenDelta)
        {
            Layout();var r=(RectTransform)transform;var size=((RectTransform)r.parent).rect.size;
            var point=new Vector2(r.anchoredPosition.x,-r.anchoredPosition.y)+new Vector2(screenDelta.x,-screenDelta.y)/GetComponentInParent<Canvas>().scaleFactor;
            preferred=new Vector2(point.x/size.x,point.y/size.y);moved=true;Layout();
        }
        public void SavePosition()
        {
            // Save the visible location after a deliberate drag, not an offscreen overshoot.
            var r=(RectTransform)transform;var size=((RectTransform)r.parent).rect.size;
            preferred=new Vector2(r.anchoredPosition.x/size.x,-r.anchoredPosition.y/size.y);
            if(Application.isBatchMode)return;
            PlayerPrefs.SetFloat(Key+".x",preferred.x);PlayerPrefs.SetFloat(Key+".y",preferred.y);PlayerPrefs.Save();
        }
        private void LateUpdate()=>Layout();
    }
    public sealed class ClassicQuestHelperDrag : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public ClassicQuestHelperBounds Panel;
        public void OnBeginDrag(PointerEventData e){Panel.transform.SetAsLastSibling();Panel.GetComponentInParent<ClassicTooltipView>()?.Hide();}
        public void OnDrag(PointerEventData e)=>Panel.MoveBy(e.delta);
        public void OnEndDrag(PointerEventData e)=>Panel.SavePosition();
    }
}
