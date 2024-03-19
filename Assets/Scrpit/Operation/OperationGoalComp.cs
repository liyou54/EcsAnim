using Unity.Entities;

namespace Scrpit.Operation
{
    public struct OperationGoalComp:IComponentData
    {
        public OperationType GoalOperationType;
    }
}