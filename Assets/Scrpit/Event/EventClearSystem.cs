using Unity.Entities;

namespace Scrpit.Event
{
    [UpdateInGroup(typeof(EventGroupSystem))]
    public partial class EventClearSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var system = EntityManager.World.GetExistingSystemManaged<EventECBS>();
            var ecb = system.CreateCommandBuffer().AsParallelWriter();
            
            Dependency = Entities.ForEach((int entityInQueryIndex,in EventComp eventComp,in Entity e) =>
            {
                ecb.DestroyEntity(entityInQueryIndex,e);
            }).ScheduleParallel(Dependency);
            system.AddJobHandleForProducer(Dependency);
        }
        
        public static void CreateEvent(EntityCommandBuffer.ParallelWriter ecb,Entity dispatcher, int index, EventType eventType)
        {
            // var entity = ecb.CreateEntity(index);
            // ecb.AddComponent(index, entity, new EventComp {EventDispatcher = dispatcher});
            // ecb.AddSharedComponent(index, entity, new EventTypeComp {EventTypeID = eventType});
        }
        
    }
}