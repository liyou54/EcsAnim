using Unity.Entities;

namespace Scrpit.Event
{
    [UpdateInGroup(typeof(EventGroupSystem), OrderLast = true)]
    public partial class EventECBS:EntityCommandBufferSystem
    {
        public static EntityCommandBuffer CreateECB()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EventECBS>();
            return system.CreateCommandBuffer();
        }
        
        public static EventECBS GetSystem()
        {
            return World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EventECBS>();
        }
        
    }
}