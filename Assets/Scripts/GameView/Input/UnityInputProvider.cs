using UnityEngine;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameView
{
    public class UnityInputProvider : IInputProvider
    {
        public bool IsLeftPressed => Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        public bool IsRightPressed => Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        public bool IsJumpPressed => Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt);
        public bool IsAttackPressed => Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Z);
        public bool IsUpPressed => Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        public bool IsDownPressed => Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        public void Update()
        {
            // No longer needed for simple jumping
        }

        public void ResetJump()
        {
            // No longer needed for simple jumping
        }

        public void ConsumeJump()
        {
            // No longer needed for simple jumping
        }
    }
}