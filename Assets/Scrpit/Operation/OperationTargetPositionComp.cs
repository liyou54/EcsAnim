using Unity.Entities;
using Unity.Mathematics;

namespace Map
{
    public struct OperationTargetPositionComp : IEnableableComponent,IComponentData
    {
        public float2 Position;
    }
}