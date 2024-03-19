using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    public struct DeleteChunkJob : IJobChunk, IJobEntityChunkBeginEnd
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public EntityTypeHandle EntityTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var chunkEntityCount = chunk.Count;
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                Ecb.RemoveComponent<EntityStatusComp>(unfilteredChunkIndex * chunkEntityCount + i, entities[i]);
                Ecb.DestroyEntity(unfilteredChunkIndex * chunkEntityCount + i, entities[i]);
            }

            entities.Dispose();
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var typeList = chunk.Archetype.GetComponentTypes();
            foreach (var type in typeList)
            {
                if ((type.IsCleanupComponent || type.IsCleanupBufferComponent) && type != typeof(EntityStatusComp))
                {
                    typeList.Dispose();
                    return true;
                }
            }

            typeList.Dispose();
            return false;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }

     
    }
}