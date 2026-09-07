using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Keep mouse hover separate from persistent keyboard selection.</summary>
    public sealed class ClassicHudButton : Button
    {
        private bool pointerInside, keyboardFocus;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) keyboardFocus = false;
            base.OnPointerDown(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            keyboardFocus = true;
            base.OnSubmit(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            base.OnPointerEnter(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            base.OnPointerExit(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            keyboardFocus = !(eventData is PointerEventData);
            base.OnSelect(eventData);
        }

        public override void OnMove(AxisEventData eventData)
        {
            keyboardFocus = true;
            base.OnMove(eventData);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            // Unity keeps a clicked Button selected. Its mouse highlight must still
            // follow the pointer, while keyboard selection remains visibly focused.
            if (state == SelectionState.Selected && !keyboardFocus)
                state = pointerInside ? SelectionState.Highlighted : SelectionState.Normal;
            base.DoStateTransition(state, instant);
        }

        protected override void OnDisable()
        {
            pointerInside = keyboardFocus = false;
            base.OnDisable();
        }
    }
}
