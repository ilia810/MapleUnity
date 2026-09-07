using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Software glove cursor with native frame origins as hotspots. Never intercepts UI input.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed class ClassicCursorView : MonoBehaviour
    {
        public const int Idle=0, Clickable=1, Grabbable=5, Grabbing=11, Clicking=12;
        private readonly Dictionary<int,ClassicSpriteFrames> art=new Dictionary<int,ClassicSpriteFrames>();
        private readonly List<RaycastResult> hits=new List<RaycastResult>();
        private Canvas canvas;private Image pointer;private Vector2 lastPosition;
        private bool ownsCursor, previousVisibility, pressedGrab;private float idleSince,stateSince;
        public int State {get;private set;}=-1;
        public Image Pointer=>pointer;
        private void Awake()
        {
            canvas=GetComponent<Canvas>();
            foreach(int state in new[]{Idle,Clickable,Grabbable,Grabbing,Clicking})art[state]=new ClassicSpriteFrames("ui","Basic.img/Cursor/"+state);
            pointer=ClassicUI.Rect("ClassicCursor",transform,0,0).gameObject.AddComponent<Image>();
            pointer.rectTransform.pivot=new Vector2(0,1);pointer.raycastTarget=false;pointer.enabled=false;
            idleSince=Time.unscaledTime;
        }
        public int StateAt(Vector2 screen,bool held=false,bool dragging=false)
        {
            if(held)return dragging?Grabbing:Clicking;
            if(EventSystem.current!=null)
            {
                hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
                if(hits.Count>0)
                {
                    var target=hits[0].gameObject;
                    var selectable=target.GetComponentInParent<Selectable>();
                    if(selectable!=null && (!selectable.IsActive() || !selectable.IsInteractable()))return Idle;
                    var slot=target.GetComponentInParent<InventorySlotInteraction>();
                    var shortcut=target.GetComponentInParent<ClassicQuickslotDrag>();
                    if(shortcut!=null && shortcut.Owner!=null && shortcut.GetBinding().Kind!=MapleClient.GameLogic.Core.QuickslotKind.Empty && shortcut.Owner.CanBind(shortcut.GetBinding()))return Grabbable;
                    if((slot!=null && slot.ItemId>0) || target.GetComponentInParent<ClassicWindowDrag>()!=null || target.GetComponentInParent<ClassicQuestHelperDrag>()!=null || target.GetComponentInParent<ClassicHistoryResize>()!=null || selectable is Scrollbar)return Grabbable;
                    return selectable!=null?Clickable:Idle;
                }
            }
            return GetComponent<ClassicNpcDialogue>()?.NpcAtScreen(screen)!=null?Clickable:Idle;
        }
        private void LateUpdate()
        {
            Vector2 screen=Input.mousePosition;
            bool inside=new Rect(0,0,Screen.width,Screen.height).Contains(screen);
            if(Application.isBatchMode || !Application.isFocused || !inside){Release();return;}
            if(screen!=lastPosition || Input.GetMouseButton(0) || Input.GetMouseButton(1)){lastPosition=screen;idleSince=Time.unscaledTime;}
            if(Input.GetMouseButtonDown(0))pressedGrab=StateAt(screen)==Grabbable;
            if(!Input.GetMouseButton(0))pressedGrab=false;
            if(!ownsCursor){previousVisibility=Cursor.visible;ownsCursor=true;}
            DrawPointer(screen,StateAt(screen,Input.GetMouseButton(0)||Input.GetMouseButton(1),pressedGrab),Time.unscaledTime);
            Cursor.visible=!pointer.enabled && previousVisibility;
            if(Time.unscaledTime-idleSince>15)pointer.enabled=false;
        }
        public void DrawPointer(Vector2 screen,int state,float seconds)
        {
            if(State!=state){State=state;stateSince=seconds;}
            var frame=(art.TryGetValue(state,out var frames)?frames:art[Idle]).Sample(seconds-stateSince);
            pointer.enabled=frame!=null;if(frame==null)return;
            pointer.sprite=frame.Sprite;pointer.rectTransform.sizeDelta=frame.Sprite.rect.size;
            var point=ClassicSpriteFrames.CanvasPoint((RectTransform)transform,canvas,screen)+new Vector2(-frame.Origin.x,frame.Origin.y);
            pointer.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x),Mathf.Round(point.y));pointer.transform.SetAsLastSibling();
        }
        private void Release(){if(pointer!=null)pointer.enabled=false;if(ownsCursor){Cursor.visible=previousVisibility;ownsCursor=false;}}
        private void OnApplicationFocus(bool focused){if(!focused)Release();}
        private void OnDisable()=>Release();
        private void OnDestroy()=>Release();
    }
}
