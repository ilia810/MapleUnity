using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MapleClient.GameView.UI
{
    /// <summary>One focus stack for independently open windows. Escape dismisses only its top.</summary>
    public sealed class ClassicWindowManager : MonoBehaviour
    {
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        public static bool IsTyping => EventSystem.current?.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null;
        private void Update()
        {
            if((!Input.GetMouseButtonDown(0)&&!Input.GetMouseButtonDown(1)) || EventSystem.current==null)return;
            FocusAt(Input.mousePosition);
        }
        public ClassicWindow FocusAt(Vector2 position)
        {
            if(EventSystem.current==null)return null;
            hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            var window=hits.Count>0?hits[0].gameObject.GetComponentInParent<ClassicWindow>():null;
            foreach(Transform child in transform)
            {
                var popup=child.GetComponent<ClassicWindow>();
                if(popup!=null&&popup!=window&&popup.DismissOnOutsideClick&&child.gameObject.activeInHierarchy)popup.Close();
            }
            window?.Focus();return window;
        }
        private void LateUpdate()
        { if(GetComponent<SkillBar>()==null&&Input.GetKeyDown(KeyCode.Escape)&&!IsTyping)CloseFrontmost(); }
        public void CloseFrontmost() => TryCloseFrontmost();
        public bool TryCloseFrontmost()
        {
            for(int i=transform.childCount-1;i>=0;i--)
            {
                var child=transform.GetChild(i);var window=child.GetComponent<ClassicWindow>();
                if(window==null || !child.gameObject.activeInHierarchy)continue;
                var group=child.GetComponent<CanvasGroup>();if(group!=null&&group.alpha==0)continue;
                window.Close();return true;
            }
            return false;
        }
    }
}
