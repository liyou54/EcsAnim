using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    public struct DeleteChunkJob : IJobChunk,IJobEntityChunkBeginEnd
    {
        public EntityCommandBuffer Ecb;
        public EntityTypeHandle EntityTypeHandle;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                Ecb.DestroyEntity(entities[i]);
            }
            entities.Dispose();
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var typeList = chunk.Archetype.GetComponentTypes();
            foreach (var type in typeList)
            {
                if (type.IsCleanupComponent || type.IsCleanupBufferComponent || type.IsCleanupSharedComponent)
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