using Unity.Collections;
using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    [UpdateInGroup(typeof(EntityLifeSystemGroup)), UpdateAfter(typeof(EntityDeleteLifeSystem))]
    public partial class EntityInitLifeSystem : SystemBase
    {
        public EntityLifeECBS entityLifeECBS;

        protected override void OnCreate()
        {
            entityLifeECBS = World.GetExistingSystemManaged<EntityLifeECBS>();
        }

        protected override void OnUpdate()
        {
            var ecb = entityLifeECBS.CreateCommandBuffer().AsParallelWriter();
            Dependency = Entities.WithSharedComponentFilter(new EntityStatusComp { State = EntityStatus.Init })
                .ForEach((int entityInQueryIndex,Entity entity) =>
                {
                    ecb.SetSharedComponent(entityInQueryIndex, entity, new EntityStatusComp { State = EntityStatus.Worrking });
                }).ScheduleParallel(Dependency);
            entityLifeECBS.AddJobHandleForProducer(Dependency);
        }
    }
}