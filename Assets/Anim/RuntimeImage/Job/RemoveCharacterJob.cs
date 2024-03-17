using Anim.RuntimeImage.DeleteSystem;
using Anim.Shader;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

namespace Anim.RuntimeImage.Job
{
    public struct RemoveCharacterJob : IJobChunk,IJobEntityChunkBeginEnd
    {
        public EntityTypeHandle EntityType;
        public ComponentTypeHandle<CharacterRenderInstanceComponent> CharacterEquipInstanceIndexType;
        public NativeArray<CharacterRenderInstanceComponent> UnUseIndexArray;
        public NativeArray<int>  CurrentUnUseIndex;
        public EntityCommandBuffer Ecb;

        public bool IsFull()
        {
            return CurrentUnUseIndex[0] == UnUseIndexArray.Length;
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
                UnUseIndexArray[CurrentUnUseIndex[0]++] = characterRenderIds[id]; 
                Ecb.RemoveComponent<CharacterRenderInstanceComponent>(entity);
                Ecb.RemoveComponent<CharacterRenderIdComponent>(entity);
                Ecb.RemoveComponent<RenderBounds>(entity);
                Ecb.RemoveComponent<WorldRenderBounds>(entity);
                Ecb.RemoveComponent<WorldToLocal_Tag>(entity);
                Ecb.RemoveComponent<PerInstanceCullingTag>(entity);
                Ecb.RemoveComponent<RenderMeshArray>(entity);
                Ecb.RemoveComponent<MaterialMeshInfo>(entity);
                Ecb.RemoveComponent<EntitiesGraphicsChunkInfo>(entity);
                Ecb.RemoveComponent<ChunkWorldRenderBounds>(entity);
                Ecb.RemoveComponent<RenderFilterSettings>(entity);
                Ecb.RemoveComponent<CharacterAnimationIndexPropertyComp>(entity);
                Ecb.RemoveComponent<CharacterAnimationStartTimePropertyComp>(entity);
                Ecb.RemoveComponent<CharacterBaseColorPropertyComp>(entity);
                Ecb.RemoveComponent<CharacterRenderStateComp>(entity);
                // Ecb.AddComponent<DeleteTag>(entity);
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