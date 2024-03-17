using System;
using System.Collections.Generic;
using Anim.RuntimeImage.DeleteSystem;
using Anim.RuntimeImage.Job;
using Anim.Shader;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using CharacterAnimationIndexPropertyComp = Anim.Shader.CharacterAnimationIndexPropertyComp;
using Random = System.Random;

namespace Anim.RuntimeImage
{
    public partial class CharacterRenderSystem : SystemBase
    {
        public ComponentTypeHandle<CharacterRenderInstanceComponent> CharacterRenderStateComponentTypeHandle;
        public EntityTypeHandle EntityTypeHandle;


        public static CharacterRenderSystemComponent GetCharacterRenderStateComponentTypeHandle()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CharacterRenderSystem>();
            return World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentObject<CharacterRenderSystemComponent>(system);
        }

        protected override void OnCreate()
        {
            var system = EntityManager.World.GetExistingSystem<CharacterRenderSystem>();
            var characterRenderSystemComponent = new CharacterRenderSystemComponent();
            EntityManager.AddComponent<CharacterRenderSystemComponent>(system);
            EntityManager.SetComponentData(system, characterRenderSystemComponent);
        }


        public CreateCharacterJob GetCreateJob(EntityQuery query, CharacterRendererData characterRendererData, EntityCommandBuffer ecb)
        {
            query.ResetFilter();
            query.AddSharedComponentFilter(new CharacterRenderIdComponent() { TypeId = characterRendererData.Id });
            query.AddSharedComponentFilter(new CharacterRenderStateComp() { State = CharacterRenderState.PreCreate });
            var characterRenderIdComponent = GetSharedComponentTypeHandle<CharacterRenderIdComponent>();
            var componentStateTypeHandle = GetSharedComponentTypeHandle<CharacterRenderStateComp>();
            EntityTypeHandle = GetEntityTypeHandle();
            var meshId = characterRendererData.BatchMeshID;
            var material = characterRendererData.BatchMaterialID;

            var unUseIndexArray = new NativeArray<CharacterRenderInstanceComponent>(characterRendererData.UnLoadIndex.Length, Allocator.TempJob);
            characterRendererData.UnLoadIndex.AsArray().CopyTo(unUseIndexArray);
            // var addIndexArray = new NativeArray<CharacterRenderInstanceComponent>(1024, Allocator.TempJob);
            var startId = characterRendererData.GetCurrentInstanceId();
            var refData = new NativeArray<int>(3, Allocator.TempJob);
            refData[0] = startId;
            var job = new CreateCharacterJob()
            {
                EntityType = EntityTypeHandle,
                CharacterRenderStateType = componentStateTypeHandle,
                CharacterRenderIdType = characterRenderIdComponent,
                Ecb = ecb,
                BatchMeshID = meshId,
                BatchMaterialId = material,
                RenderFilterSettings = RenderFilterSettings.Default,
                UnUseIndexArray = unUseIndexArray,
                // AddIndexArray = addIndexArray,
                StartInstanceId = startId,
                RefData = refData
            };
            return job;
        }

        protected RemoveCharacterJob GetRemoveJob(EntityQuery query, CharacterRendererData characterRendererData, EntityCommandBuffer ecb)
        {
            query.ResetFilter();
            query.AddSharedComponentFilter(new CharacterRenderStateComp() { State = CharacterRenderState.Destroy });
            query.AddSharedComponentFilter(new CharacterRenderIdComponent() { TypeId = characterRendererData.Id });
            var characterRenderInstanceComponent = GetComponentTypeHandle<CharacterRenderInstanceComponent>();
            EntityTypeHandle = GetEntityTypeHandle();
            var unLoadArray = new NativeArray<CharacterRenderInstanceComponent>(1024, Allocator.TempJob);

            var job = new RemoveCharacterJob()
            {
                EntityType = EntityTypeHandle,
                CharacterEquipInstanceIndexType = characterRenderInstanceComponent,
                Ecb = ecb,
                UnUseIndexArray = unLoadArray,
                CurrentUnUseIndex = new NativeArray<int>(1, Allocator.TempJob)
            };
            return job;
        }

        protected override void OnUpdate()
        {
            var ecbMarkDel = new EntityCommandBuffer(Allocator.TempJob);
            CharacterMarkDelJob characterMarkDelJob = new CharacterMarkDelJob()
            {
                Ecb = ecbMarkDel,
                EntityType = GetEntityTypeHandle()
            };
            var markDelQuery = GetEntityQuery(typeof(CharacterRenderStateComp), typeof(DeleteTag));
            markDelQuery.ResetFilter();
            markDelQuery.AddSharedComponentFilter(new CharacterRenderStateComp() { State = CharacterRenderState.Worrking });
            Dependency = characterMarkDelJob.Schedule(markDelQuery, Dependency);
            Dependency.Complete();
            ecbMarkDel.Playback(EntityManager);
            ecbMarkDel.Dispose();


            var ids = GetCharacterRenderStateComponentTypeHandle().CharacterRendererDataDic.Keys;
            var ecbCreateDel = new EntityCommandBuffer(Allocator.TempJob);
            var characterRemoveJobData = new Dictionary<int, RemoveCharacterJob>();
            var characterCreateJobData = new Dictionary<int, CreateCharacterJob>();
            foreach (var id in ids)
            {
                EntityQueryBuilder queryCreateBuilder = new EntityQueryBuilder(Allocator.TempJob);
                var renderJobQueryCreate = queryCreateBuilder.WithAll<CharacterRenderIdComponent, CharacterRenderStateComp>().Build(this);
                CharacterRendererData data = GetCharacterRendererData(id);
                var jobRemove = GetRemoveJob(renderJobQueryCreate, data, ecbCreateDel);
                Dependency = jobRemove.Schedule(renderJobQueryCreate, Dependency);
                Dependency.Complete();

                var jobCreate = GetCreateJob(renderJobQueryCreate, data, ecbCreateDel);
                Dependency = jobCreate.Schedule(renderJobQueryCreate, Dependency);
                Dependency.Complete();

                characterCreateJobData.Add(id, jobCreate);
                characterRemoveJobData.Add(id, jobRemove);

                queryCreateBuilder.Dispose();
            }

            Dependency.Complete();
            ecbCreateDel.Playback(EntityManager);
            ecbCreateDel.Dispose();
            
            var ecbEquipChange = new EntityCommandBuffer(Allocator.TempJob);
            var characterEquipChangeJobData = new Dictionary<int, CharacterEquipChangeJob>();
            foreach (var id in ids)
            {
                EntityQueryBuilder equipChange = new EntityQueryBuilder(Allocator.TempJob);

                var equipChangeQuery = equipChange.WithAll<CharacterRenderIdComponent, EquipmentDataChangeBuffer, CharacterRenderStateComp>().Build(this);
                equipChangeQuery.ResetFilter();
                equipChangeQuery.AddSharedComponentFilter(new CharacterRenderIdComponent() { TypeId = id });
                CharacterRendererData data = GetCharacterRendererData(id);
                var jobEquipChange = new CharacterEquipChangeJob()
                {
                    Ecb = ecbEquipChange,
                    EntityType = GetEntityTypeHandle(),
                    EquipmentDataChangeBufferType = GetBufferTypeHandle<EquipmentDataChangeBuffer>(),
                    CharacterRenderInstanceComponent = GetComponentTypeHandle<CharacterRenderInstanceComponent>(),
                    EquipChangeData = new NativeArray<UpdateEquipBufferIndex>(1024, Allocator.TempJob),
                    CurrentEquipChangeIndex = new NativeArray<int>(1, Allocator.TempJob),
                    SpriteCount = data.SpriteCount
                };
                characterEquipChangeJobData.Add(id, jobEquipChange);
                Dependency = jobEquipChange.Schedule(equipChangeQuery, Dependency);
                Dependency.Complete();
            }

            ecbEquipChange.Playback(EntityManager);
            ecbEquipChange.Dispose();

            foreach (var id in ids)
            {
                CharacterRendererData data = GetCharacterRendererData(id);
                var jobRemove = characterRemoveJobData[id];
                var jobCreate = characterCreateJobData[id];
                var jobEquipChange = characterEquipChangeJobData[id];
                data.UnLoadIndex.Clear();

                for (int i = 0; i < jobRemove.CurrentUnUseIndex[0]; i++)
                {
                    data.UnLoadIndex.Add(jobRemove.UnUseIndexArray[i]);
                }

                for (int i = jobCreate.RefData[(int)CreateCharacterJob.CurrentCountEnum.CurrentUnUseIndex]; i < jobCreate.UnUseIndexArray.Length; i++)
                {
                    data.UnLoadIndex.Add(jobCreate.UnUseIndexArray[i]);
                }

                if (jobRemove.CurrentUnUseIndex[0] > 0)
                {
                    data.RemoveUsedInstance(jobRemove.UnUseIndexArray);
                }


                var count = (jobCreate.RefData[(int)CreateCharacterJob.CurrentCountEnum.CurrentInstanceId] - jobCreate.StartInstanceId) * data.SpriteCount;
                if (count > 0)
                {
                    var temp = new CharacterRenderInstanceComponent[count];
                    data.EquipTexPosIdBuffer.AddData(temp);
                }

                var equipChangeCount = jobEquipChange.CurrentEquipChangeIndex[0];
                if (equipChangeCount > 0)
                {
                    data.SetEquip(jobEquipChange.EquipChangeData);
                }

                jobCreate.RefData.Dispose();
                jobRemove.UnUseIndexArray.Dispose();
                jobCreate.UnUseIndexArray.Dispose();
                // jobCreate.AddIndexArray.Dispose();
                jobEquipChange.EquipChangeData.Dispose();
            }
        }

        public static void RegisterSprite(int charcterTypeId, int equipTypeId, Sprite sprite)
        {
            var comp = GetCharacterRenderStateComponentTypeHandle();
            if (comp.CharacterRendererDataDic.TryGetValue(charcterTypeId, out var data))
            {
                data.RegisterSprite(equipTypeId, sprite);
            }
        }

        public static int RegisterCharacterRender(BakedCharacterAsset bakedCharacterAsset)
        {
            if (bakedCharacterAsset == null)
            {
                return -1;
            }

            var comp = GetCharacterRenderStateComponentTypeHandle();
            if (comp.CharacterRender.TryGetValue(bakedCharacterAsset, out var id))
            {
                return id;
            }
            else
            {
                id = comp.CharacterRender.Count + 1;
                comp.CharacterRender.Add(bakedCharacterAsset, id);
                var data = new CharacterRendererData(bakedCharacterAsset);
                data.Id = id;
                comp.CharacterRendererDataDic.Add(id, data);
            }

            return id;
        }

        public static CharacterRendererData GetCharacterRendererData(int id)
        {
            var comp = GetCharacterRenderStateComponentTypeHandle();

            if (comp.CharacterRendererDataDic.TryGetValue(id, out var data))
            {
                return data;
            }

            return null;
        }
    }
}