using System;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public partial class Player
    {
        public SourceStanceAnimation StanceAnimation { get; } = new SourceStanceAnimation();
        public SourceFaceAnimation FaceAnimation { get; } = new SourceFaceAnimation();
        private CharacterExpression? pendingExpression;

        public bool RequestExpression(CharacterExpression expression)
        {
            if (IsDead || pendingExpression.HasValue || !FaceAnimation.HasExpression(expression) ||
                !FaceAnimation.CanSetExpression(expression)) return false;
            pendingExpression = expression;
            return true;
        }

        private void ApplyExpressionInput()
        {
            if (pendingExpression.HasValue) FaceAnimation.TrySetExpression(pendingExpression.Value);
            pendingExpression = null;
        }

        private void AdvanceFace(float movementSpeed)
        {
            // The existing combat motion owns its live attack step, including
            // the local unarmed practice action's fixed speed.
            int step = IsBasicAttacking ? BasicAttack.AnimationStepMilliseconds : SourceStanceAnimation.Timestep(movementSpeed);
            FaceAnimation.Advance(step);
        }

        public CharacterState BodyStance
        {
            get
            {
                if (IsBasicAttacking) return BasicAttack.Stance;
                switch (State)
                {
                    case PlayerState.Walking: return CurrentWeapon?.Walk ?? CharacterState.Walk;
                    case PlayerState.Falling: return CharacterState.Fall;
                    case PlayerState.Jumping:
                    case PlayerState.DoubleJumping: case PlayerState.FlashJumping: return CharacterState.Jump;
                    case PlayerState.Swimming: return CharacterState.Fly;
                    case PlayerState.Climbing: return currentLadder?.IsLadder == false ? CharacterState.Rope : CharacterState.Ladder;
                    case PlayerState.Crouching: return CharacterState.Prone;
                    default: return CurrentWeapon?.Stand ?? CharacterState.Stand;
                }
            }
        }

        public void SynchronizeBodyStance()
        {
            if (!IsBasicAttacking) StanceAnimation.SetStance(BodyStance);
        }

        private void AdvanceBodyStance(NormalMovement.Stance stance, float speed)
        {
            if (IsBasicAttacking) return;
            CharacterState pose;
            switch (stance)
            {
                case NormalMovement.Stance.Walk: pose = CurrentWeapon?.Walk ?? CharacterState.Walk; break;
                case NormalMovement.Stance.Fall: pose = CharacterState.Jump; break;
                case NormalMovement.Stance.Prone: pose = CharacterState.Prone; break;
                case NormalMovement.Stance.Swim: pose = CharacterState.Fly; break;
                case NormalMovement.Stance.Ladder: pose = CharacterState.Ladder; break;
                case NormalMovement.Stance.Rope: pose = CharacterState.Rope; break;
                default: pose = CurrentWeapon?.Stand ?? CharacterState.Stand; break;
            }
            StanceAnimation.SetStance(pose);
            StanceAnimation.Advance(speed);
        }
    }
}
