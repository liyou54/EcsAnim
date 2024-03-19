using System;
using System.Collections.Generic;
using Anim.RuntimeImage;
using Anim.Shader;
using Map;
using Scrpit.Move;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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

        [ShowInInspector]
        [TableList]
        [HideReferenceObjectPicker]
        [Button]
        public void InitRegisterSprite()
        {
        }


        public void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var id = CharacterRenderSys.RegisterCharacterRender(characterAsset);
            characterArchetype = entityManager.CreateArchetype(typeof(CharacterRenderInstanceComponent));
        }


        [Button]
        public void Test(int count = 1, int id = 1)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (count > 0)
            {
                var characterRendererData = CharacterRenderSys.GetCharacterRendererData(id);
                var equipList = characterRendererData.GetEquipList();
                var entities = new NativeArray<Entity>(count, Allocator.Temp);

                var archType = entityManager.CreateArchetype(
                    typeof(CharacterRenderIdComponent),
                    typeof(CharacterRenderStateComp),
                    typeof(LocalToWorld),
                    typeof(CharacterAnimationIndexPropertyComp),
                    typeof(CharacterAnimationStartTimePropertyComp),
                    typeof(CharacterBaseColorPropertyComp),
                    typeof(BattleTeamComp),
                    typeof(BeTargetAbleTag),
                    typeof(TargetEntityComp),
                    typeof(OperationPositionComp),
                    typeof(MovePositionComp),
                    typeof(MoveSpeedComp),
                    typeof(MoveStepComp),
                    typeof(CharacterRenderInstanceComponent)
                );
                entityManager.CreateEntity(archType, entities);
                for (int i = 0; i < count; i++)
                {
                    var entity = entities[i];

                    var teamId = Random.Range(0, 2) == 0 ? 1 : 2;
                    var randColor = teamId == 1 ? new float4(1, 0, 0, 1) : new float4(0, 1, 0, 1);
                    var localToWorld = new LocalToWorld();
                    localToWorld.Value = float4x4.identity;
                    localToWorld.Value.c3.xyz = UnityEngine.Random.insideUnitSphere * new float3(100, 100, 0);
                    ecb.SetComponent(entities[i], new BattleTeamComp() { TeamId = teamId });
                    ecb.SetSharedComponent(entity, new CharacterRenderIdComponent() { TypeId = 1 });
                    ecb.SetSharedComponent(entity, new CharacterRenderStateComp() { State = CharacterRenderState.PreCreate });
                    
                    ecb.SetComponent(entities[i], new CharacterBaseColorPropertyComp() { Value = randColor });
                    ecb.SetComponent(entities[i], localToWorld);
                    ecb.SetComponent(entities[i], new CharacterAnimationIndexPropertyComp() { Value = 1 });
                    ecb.SetComponent(entity, new MoveSpeedComp() { Speed = 1 });
                }

                entities.Dispose();
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
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