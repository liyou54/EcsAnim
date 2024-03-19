using Unity.Entities;

namespace Scrpit.Operation
{
    public enum OperationType
    {
        Idle,
        Move,
        Attack,
    }
    
    public struct CurrentOperationComp:IComponentData
    {
        public OperationType CurrentOperationType;
    }
}