using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Anim.RuntimeImage.Job
{
    public  struct CharacterEquipChangeJob:IJobChunk,IJobEntityChunkBeginEnd
    {

        public EntityCommandBuffer Ecb;
        public EntityTypeHandle EntityType;
        public BufferTypeHandle<EquipmentDataChangeBuffer> EquipmentDataChangeBufferType;
        public ComponentTypeHandle<CharacterRenderInstanceComponent> CharacterRenderInstanceComponent;
        public NativeArray<UpdateEquipBufferIndex> EquipChangeData;
        public NativeArray<int> CurrentEquipChangeIndex;
        public int SpriteCount;
        
        public bool IsFull()
        {
            return CurrentEquipChangeIndex[0] == EquipChangeData.Length;
        }
        
        public bool IsFull(int count)
        {
            return CurrentEquipChangeIndex[0] + count >= EquipChangeData.Length;
        }
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityType);
            var equipmentDataChangeBuffers = chunk.GetBufferAccessor(ref EquipmentDataChangeBufferType);
            var characterRenderInstanceComponents = chunk.GetNativeArray(ref CharacterRenderInstanceComponent);
            
            if (IsFull(equipmentDataChangeBuffers.Length))
            {
                return;
            }
            
            for (int i = 0; i < entities.Length; i++)
            {
                var instanceId = characterRenderInstanceComponents[i].InstanceId;
                var entity = entities[i];
                var buffer = equipmentDataChangeBuffers[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    var data = buffer[j];
                    EquipChangeData[CurrentEquipChangeIndex[0]] = new UpdateEquipBufferIndex(instanceId * SpriteCount + data.Position , data.NewId+1);
                    CurrentEquipChangeIndex[0]++;
                }
                Ecb.RemoveComponent<EquipmentDataChangeBuffer>(entity);
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