using Unity.Entities;
using Unity.Mathematics;

namespace Map
{
    public struct OperationPositionComp : IEnableableComponent,IComponentData
    {
        public float2 Position;
    }
}