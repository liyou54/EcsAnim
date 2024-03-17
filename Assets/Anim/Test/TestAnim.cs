using System;
using System.Collections.Generic;
using Anim.RuntimeImage;
using Anim.Shader;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Anim.Test
{
    [Serializable]
    [HideReferenceObjectPicker]
    [TableList]
    public class TestSpriteRegister
    {
        public int SpriteTypeId;
        public List<Sprite> Sprites;
    }

    public class TestAnim : MonoBehaviour
    {
        public BakedCharacterAsset characterAsset;

        public EntityArchetype characterArchetype;
        private EntityManager entityManager;

        [ShowInInspector] [TableList] [HideReferenceObjectPicker]
        public List<TestSpriteRegister> RegisterSprite;
        
        [Button]
        public void InitRegisterSprite()
        {
            RegisterSprite.Clear();
            if (characterAsset)
            {
                for (int i = 0; i < characterAsset.DefaultSprites.Count; i++)
                {
                    var sprite = characterAsset.DefaultSprites[i];
                    RegisterSprite.Add(new TestSpriteRegister() { SpriteTypeId = i, Sprites = new List<Sprite>() { sprite } });
                }
            }
        }


        public void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var id = CharacterRenderSystem.RegisterCharacterRender(characterAsset);
            characterArchetype = entityManager.CreateArchetype(typeof(CharacterRenderInstanceComponent));
            
            for (int i = 0; i < RegisterSprite.Count; i++)
            {
                var register = RegisterSprite[i];
                for (int j = 0; j < register.Sprites.Count; j++)
                {
                    if (register.Sprites[j] == null)
                    {
                        continue;
                    }
                    CharacterRenderSystem.RegisterSprite(id, register.SpriteTypeId, register.Sprites[j]);
                }
            }
        }


        [Button]
        public void Test(int count = 1, int id = 1)
        {
            if (count > 0)
            {
                var characterRendererData = CharacterRenderSystem.GetCharacterRendererData(id);
                var equipList = characterRendererData.GetEquipList();
                var entities = new NativeArray<Entity>(count, Allocator.Temp);
                entityManager.CreateEntity(characterArchetype, entities);
                for (int i = 0; i < count; i++)
                {
                    entityManager.AddSharedComponent(entities[i], new CharacterRenderIdComponent() { TypeId = id });
                    entityManager.AddSharedComponent(entities[i], new CharacterRenderStateComp() { State = CharacterRenderState.PreCreate });
                    var rand = Random.Range(0, 3);
                    var equipTypeId = 0;
                    if (rand == 1)
                    {
                        equipTypeId = 33;
                    }
                    else if (rand == 2)
                    {
                        equipTypeId = 37;
                    }
                    if (equipList[equipTypeId].Length > 1){
                        var buffer = entityManager.AddBuffer<EquipmentDataChangeBuffer>(entities[i]);
                        var randomSprite = Random.Range(0, equipList[equipTypeId].Length);
                        buffer.Add(new EquipmentDataChangeBuffer() { Position = equipTypeId, NewId = equipList[equipTypeId][randomSprite]   });
                    }
                    var localToWorld = new LocalToWorld();
                    localToWorld.Value = float4x4.identity;
                    localToWorld.Value.c3.xyz =  UnityEngine.Random.insideUnitSphere * new float3(11, 11, 0);
                    entityManager.AddComponent<LocalToWorld>(entities[i]);
                    entityManager.AddComponent<CharacterAnimationIndexPropertyComp>(entities[i]);
                    entityManager.AddComponent<CharacterAnimationStartTimePropertyComp>(entities[i]);
                    entityManager.SetComponentData(entities[i], localToWorld);
                    entityManager.SetComponentData(entities[i], new CharacterAnimationIndexPropertyComp()  { Value = Random.Range(1,11) });
                    entityManager.SetComponentData(entities[i], new CharacterAnimationStartTimePropertyComp() { Value = Random.Range(0,1f) });

                }
            
                entities.Dispose();
            }
        }



        private void Update()
        {
            UnityEngine.Shader.SetGlobalFloat("_AnimationTime", Time.time);
            Vector3 screenBottomCenter = new Vector3(Screen.width / 2f, 0f, 0f);

            // 将屏幕底部中心点的屏幕坐标转换为世界坐标
            Vector3 worldBottomCenter = Camera.main.ScreenToWorldPoint(screenBottomCenter);
            UnityEngine.Shader.SetGlobalFloat("_WorldPosButton", worldBottomCenter.y);
        }
    }
}