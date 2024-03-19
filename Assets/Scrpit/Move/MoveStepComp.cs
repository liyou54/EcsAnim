using Unity.Entities;
using Unity.Mathematics;

namespace Scrpit.Move
{

    public enum MoveType
    {
        DefaultWalk,
        Skill,
        ForceMove,
    }
    
    public struct MoveStepComp:IComponentData
    {
        public float2 NextPos;
        public float2 LastPos;
        public MoveType MoveType;
    }
    
    
}