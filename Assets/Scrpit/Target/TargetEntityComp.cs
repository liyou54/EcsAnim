using Unity.Entities;

namespace Map
{

    public struct TargetEntityComp:IEnableableComponent,IComponentData
    {
        public Entity Entity;
    }
}