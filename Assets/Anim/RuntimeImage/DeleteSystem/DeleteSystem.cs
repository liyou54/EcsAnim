using Unity.Collections;
using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    public partial struct DeleteSystem : ISystem
    {
        EntityQuery m_DeleteQuery;
        public void OnCreate(ref SystemState state)
        {
            m_DeleteQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<DeleteTag>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new DeleteChunkJob
            {
                Ecb = ecb,
                EntityTypeHandle = state.EntityManager.GetEntityTypeHandle()
            };
            state.Dependency = job.Schedule(m_DeleteQuery, state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}