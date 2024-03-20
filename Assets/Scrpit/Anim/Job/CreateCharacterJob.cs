using System.Collections.Generic;
using Anim.RuntimeImage.DeleteSystem;
using Anim.Shader;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Anim.RuntimeImage.Job
{
    public struct CreateCharacterJob : IJobChunk, IJobEntityChunkBeginEnd
    {
        public EntityTypeHandle EntityType;
        public SharedComponentTypeHandle<CharacterRenderIdComponent> CharacterRenderIdType;

        public EntityCommandBuffer Ecb;

        public NativeArray<CharacterRenderInstanceComponent> UnUseIndexArray;


        public BatchMaterialID BatchMaterialId;
        public BatchMeshID BatchMeshID;
        public RenderFilterSettings RenderFilterSettings;

        public int StartInstanceId; // 这个结束后结算使用

        // 0 CurrentInstanceId 1 CurrentUnUseIndex 2 CurrentAddIndex
        public NativeArray<int> RefData;

        public enum CurrentCountEnum
        {
            CurrentInstanceId = 0,
            CurrentUnUseIndex = 1,
            CurrentAddIndex = 2
        }

        private bool IsFull()
        {
            bool isFull = RefData[(int)CurrentCountEnum.CurrentUnUseIndex] + RefData[(int)CurrentCountEnum.CurrentInstanceId] - StartInstanceId >= 1024;
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
            Ecb.RemoveComponent<CharacterRenderReqComp>(entity);
        }

        private int GetInstanceId()
        {
            if (RefData[(int)CurrentCountEnum.CurrentUnUseIndex] < UnUseIndexArray.Length)
            {
                return UnUseIndexArray[RefData[(int)CurrentCountEnum.CurrentUnUseIndex]++];
            }
            return RefData[(int)CurrentCountEnum.CurrentInstanceId]++;
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
                // AddIndexArray[RefData[(int)CurrentCountEnum.CurrentAddIndex]++] = instanceId;
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