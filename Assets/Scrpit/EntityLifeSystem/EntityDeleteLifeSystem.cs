using Unity.Collections;
using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    [UpdateInGroup(typeof(EntityLifeSystemGroup), OrderFirst = true)]
    public partial class EntityDeleteLifeSystem : SystemBase
    {
        private EntityLifeECBS entityLifeECBS;

        protected override void OnCreate()
        {
            entityLifeECBS = World.GetExistingSystemManaged<EntityLifeECBS>();

        }



        protected override void OnUpdate()
        {
            var ecb =  entityLifeECBS.CreateCommandBuffer().AsParallelWriter();
            var job = new DeleteChunkJob
            {
                Ecb = ecb,
                EntityTypeHandle = EntityManager.GetEntityTypeHandle()
            };
            var deleteQuery = GetEntityQuery(ComponentType.ReadOnly<EntityStatusComp>());
            deleteQuery.ResetFilter();
            deleteQuery.AddSharedComponentFilter(new EntityStatusComp {State = EntityStatus.Destroy});
            Dependency = job.ScheduleParallel(deleteQuery, Dependency);
            entityLifeECBS.AddJobHandleForProducer(Dependency);
        }
    }
}