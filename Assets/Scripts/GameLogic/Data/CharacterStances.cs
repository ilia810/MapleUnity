using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    public static class CharacterStances
    {
        public static string Name(CharacterState state)
        {
            switch (state) {
                case CharacterState.Stand: return "stand1"; case CharacterState.Stand2: return "stand2";
                case CharacterState.Walk: return "walk1"; case CharacterState.Walk2: return "walk2";
                case CharacterState.Jump: case CharacterState.Fall: return "jump";
                case CharacterState.Alert: return "alert"; case CharacterState.Prone: return "prone";
                case CharacterState.Fly: return "fly"; case CharacterState.Ladder: return "ladder"; case CharacterState.Rope: return "rope";
                case CharacterState.Attack1: return "stabO1"; case CharacterState.StabO2: return "stabO2";
                case CharacterState.Attack2: return "swingO1"; case CharacterState.SwingO2: return "swingO2"; case CharacterState.SwingO3: return "swingO3";
                case CharacterState.SwingT1: return "swingT1"; case CharacterState.SwingT2: return "swingT2"; case CharacterState.SwingT3: return "swingT3";
                case CharacterState.StabT1: return "stabT1"; case CharacterState.SwingP1: return "swingP1";
                case CharacterState.ProneStab: return "proneStab"; case CharacterState.Skill: return "skill";
                case CharacterState.Shoot1: return "shoot1"; case CharacterState.Shoot2: return "shoot2"; case CharacterState.Shot: return "shot";
                default: return "stand1";
            }
        }
    }
}
