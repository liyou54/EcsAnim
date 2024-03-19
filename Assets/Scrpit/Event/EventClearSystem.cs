using Unity.Entities;

namespace Scrpit.Event
{
    [UpdateInGroup(typeof(EventGroupSystem))]
    public partial class EventClearSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var system = EntityManager.World.GetExistingSystemManaged<EventECBS>();
            var ecb = system.CreateCommandBuffer();
            
            Dependency = Entities.ForEach((in EventComp eventComp,in Entity e) =>
            {
                ecb.DestroyEntity(e);
            }).ScheduleParallel(Dependency);
            system.AddJobHandleForProducer(Dependency);
        }
    }
}