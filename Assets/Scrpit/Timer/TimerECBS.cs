using Anim.RuntimeImage.DeleteSystem;
using Unity.Entities;

namespace Scrpit.Timer
{
    [UpdateInGroup(typeof(EntityLifeSystemGroup)), UpdateAfter(typeof(EntityDeleteLifeSystem))]
    public partial class TimerECBS:EntityCommandBufferSystem
    {
        public static EntityCommandBuffer CreateECB()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimerECBS>();
            return system.CreateCommandBuffer();
        }
        
        public static TimerECBS GetSystem()
        {
            return World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimerECBS>();
        }
        
    }
}