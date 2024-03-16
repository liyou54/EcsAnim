using System.Collections.Generic;
using Anim.Shader;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Anim.RuntimeImage.Job
{
    public struct CreateCharacterJob : IJobChunk,IJobEntityChunkBeginEnd
    {
        public EntityTypeHandle EntityType;
        public SharedComponentTypeHandle<CharacterRenderIdComponent> CharacterRenderStateType;
        public EntityCommandBuffer Ecb;
        public int CurrentUnUseIndex;
        public NativeArray<CharacterRenderInstanceComponent> UnUseIndexArray;
        public int CurrentAddIndex;
        public NativeArray<CharacterRenderInstanceComponent> AddIndexArray;
        public BatchMaterialID BatchMaterialId;
        public BatchMeshID BatchMeshID;
        public RenderFilterSettings RenderFilterSettings;
        public  int StartInstanceId; // 这个结束后结算使用
        public int CurrentInstanceId;



        private bool IsFull()
        {
            bool isFull = CurrentAddIndex == AddIndexArray.Length;
            return isFull;
        }

        private void AddComp(Entity entity)
        {
            Ecb.AddComponent<LocalToWorld>(entity);
            Ecb.AddComponent(entity, new MaterialMeshInfo() { MaterialID = BatchMaterialId, MeshID = BatchMeshID });
            Ecb.AddComponent(entity, new RenderBounds());
            Ecb.AddComponent(entity, new WorldRenderBounds());
            Ecb.AddComponent(entity, new WorldToLocal_Tag());
            Ecb.AddComponent(entity, new PerInstanceCullingTag());
            Ecb.AddSharedComponentManaged(entity, new RenderMeshArray());
            Ecb.AddSharedComponent(entity, RenderFilterSettings);

            Ecb.AddComponent<CharacterAnimationIndexPropertyComp>(entity);
            Ecb.AddComponent<CharacterAnimationStartTimePropertyComp>(entity);
            Ecb.AddComponent<CharacterBaseColorPropertyComp>(entity);
        }

        private int GetInstanceId()
        {
            if (CurrentUnUseIndex < UnUseIndexArray.Length)
            {
                return UnUseIndexArray[CurrentUnUseIndex++];
            }

            return CurrentInstanceId++;
        }


        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {

            var entities = chunk.GetNativeArray(EntityType);

            foreach (var entity in entities)
            {
                if (IsFull())
                {
                    return;
                }
                //
                var instanceId = GetInstanceId();
                AddIndexArray[CurrentAddIndex++] = instanceId; 
                Ecb.AddComponent(entity, new CharacterRenderInstanceComponent()
                {
                    InstanceId = instanceId
                });
                AddComp(entity);
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