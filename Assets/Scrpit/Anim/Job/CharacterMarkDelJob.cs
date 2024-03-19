using Anim.RuntimeImage.DeleteSystem;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace Anim.RuntimeImage.Job
{
    public partial struct CharacterMarkDelJob:IJobChunk
    {
        public EntityCommandBuffer Ecb;
        public EntityTypeHandle EntityType;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityType);
            foreach (var entity in entities)
            {
                Ecb.SetSharedComponent(entity, new EntityStatusComp() { State = EntityStatus.Destroy });
            }
        }
    }
}