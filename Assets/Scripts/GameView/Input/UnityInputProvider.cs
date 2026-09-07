using UnityEngine;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Core;
using MapleClient.GameView.UI;

namespace MapleClient.GameView
{
    public class UnityInputProvider : IInputProvider, IExpressionInputProvider
    {
        public SkillBar Quickslots { get; set; }
        private static readonly KeyboardMap fallback = new KeyboardMap();
        private bool Pressed(QuickslotAction action)
        {
            if (Quickslots != null) return Quickslots.ActionPressed(action);
            if (ClassicWindowManager.IsTyping) return false;
            return fallback.IsPressed(action, key => action == QuickslotAction.Attack ?
                Input.GetKeyDown(ClassicKeyboardLayout.Native(key)) : Input.GetKey(ClassicKeyboardLayout.Native(key)));
        }
        public bool IsLeftPressed => Pressed(QuickslotAction.Left);
        public bool IsRightPressed => Pressed(QuickslotAction.Right);
        public bool IsUpPressed => Pressed(QuickslotAction.Up);
        public bool IsDownPressed => Pressed(QuickslotAction.Down);
        public bool IsJumpPressed => Pressed(QuickslotAction.Jump);
        public bool IsAttackPressed => Pressed(QuickslotAction.Attack);
        public CharacterExpression? ExpressionPressed
        {
            get
            {
                if (Quickslots != null) return Quickslots.ExpressionPressed();
                if (ClassicWindowManager.IsTyping) return null;
                for (int face = 0; face < 7; face++)
                    if (fallback.IsPressed((QuickslotAction)(100 + face), key => Input.GetKeyDown(ClassicKeyboardLayout.Native(key))))
                        return MapleClient.GameLogic.Data.CharacterExpressions.ForFunctionKey(face + 1);
                return null;
            }
        }
        public void Update() { }
        public void ResetJump() { }
        public void ConsumeJump() { }
    }
}
