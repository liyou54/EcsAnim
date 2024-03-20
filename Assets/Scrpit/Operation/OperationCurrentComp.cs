using Unity.Entities;

namespace Scrpit.Operation
{
    public enum OperationType
    {
        Idle,
        Move,
        Attack,
    }
    
    public struct OperationCurrentComp:IComponentData
    {
        public OperationType CurrentOperationType;
    }
    
    public struct OperationGoalComp:IComponentData
    {
        public OperationType GoalOperationType;
    }
    
    public struct OperationHistoryComp:IComponentData
    {
        public OperationType OperationType;
    }
    
}