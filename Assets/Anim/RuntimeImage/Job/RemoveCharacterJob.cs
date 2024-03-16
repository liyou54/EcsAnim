using Anim.RuntimeImage.DeleteSystem;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Anim.RuntimeImage.Job
{
    public struct RemoveCharacterJob : IJobChunk,IJobEntityChunkBeginEnd
    {
        public EntityTypeHandle EntityType;
        public SharedComponentTypeHandle<CharacterRenderIdComponent> CharacterRenderIdType;
        public ComponentTypeHandle<CharacterRenderInstanceComponent> CharacterEquipInstanceIndexType;
        public NativeArray<CharacterRenderInstanceComponent> UnUseIndexArray;
        public int CurrentUnUseIndex;
        public EntityCommandBuffer Ecb;

        public bool IsFull()
        {
            return CurrentUnUseIndex == UnUseIndexArray.Length;
        }
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var characterRenderIds = chunk.GetNativeArray(ref CharacterEquipInstanceIndexType);
            ChunkEntityEnumerator entityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (entityEnumerator.NextEntityIndex(out var id))
            {
                if (IsFull())
                {
                    return;
                }
                var entity = entities[id];
                UnUseIndexArray[CurrentUnUseIndex++] = characterRenderIds[id]; 
                Ecb.RemoveComponent<CharacterRenderInstanceComponent>(entity);
                Ecb.RemoveComponent<CharacterRenderIdComponent>(entity);
                Ecb.AddComponent<DeleteTag>(entity);
            }
            
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (IsFull())
            {
                return false;
            }

            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }
    }
}