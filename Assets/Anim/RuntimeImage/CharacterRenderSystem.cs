using System;
using System.Collections.Generic;
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
        public EntityQuery CharacterRenderDelQuery;


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


        public CreateCharacterJob CreatCreateJob(EntityQuery query, CharacterRendererData characterRendererData, EntityCommandBuffer ecb)
        {
            query.ResetFilter();
            query.SetSharedComponentFilter(new CharacterRenderIdComponent() { TypeId = characterRendererData.Id });
            var characterRenderIdComponent = GetSharedComponentTypeHandle<CharacterRenderIdComponent>();
            EntityTypeHandle = GetEntityTypeHandle();
            var meshId = characterRendererData.BatchMeshID;
            var material = characterRendererData.BatchMaterialID;

            var unUseIndexArray = new NativeArray<CharacterRenderInstanceComponent>(characterRendererData.UnLoadIndex.Length, Allocator.Temp);
            characterRendererData.UnLoadIndex.AsArray().CopyTo(unUseIndexArray);
            var addIndexArray = new NativeArray<CharacterRenderInstanceComponent>(1024, Allocator.Temp);
            
            var job = new CreateCharacterJob()
            {
                EntityType = EntityTypeHandle,
                CharacterRenderStateType = characterRenderIdComponent,
                Ecb = ecb,
                BatchMeshID = meshId,
                BatchMaterialId = material,
                RenderFilterSettings = RenderFilterSettings.Default,
                CurrentUnUseIndex = 0,
                UnUseIndexArray = unUseIndexArray,
                CurrentAddIndex = 0,
                AddIndexArray = addIndexArray,
                StartInstanceId = characterRendererData.GetCurrentInstanceId()
            };


            return job;
        }

        protected override void OnUpdate()
        {
            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            var createRenderJob = queryBuilder.WithAll<CharacterRenderIdComponent>().WithNone<CharacterRenderInstanceComponent>().Build(this);
            var delRenderJob = queryBuilder.WithAll<CharacterRenderInstanceComponent>().WithNone<CharacterRenderIdComponent>().Build(this);
            var ids = GetCharacterRenderStateComponentTypeHandle().CharacterRendererDataDic.Keys; 
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var id in ids)
            {
                CharacterRendererData data = GetCharacterRendererData(id);
                var jobCreate = CreatCreateJob(createRenderJob, data, ecb);
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
                comp.CharacterRendererDataDic.Add(id, new CharacterRendererData(bakedCharacterAsset));
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