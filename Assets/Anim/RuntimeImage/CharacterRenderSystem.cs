using System;
using System.Collections.Generic;
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
        private static Dictionary<BakedCharacterAsset, int> CharacterRender = new Dictionary<BakedCharacterAsset, int>();
        private static Dictionary<int, CharacterRendererData> characterRendererDataDic = new Dictionary<int, CharacterRendererData>();
        public ComponentTypeHandle<CharacterRenderStateComponent> CharacterRenderStateComponentTypeHandle;
        public EntityTypeHandle EntityTypeHandle;
        public EntityQuery CharacterRenderDelQuery;


        public class CharacterRendererDataChange
        {
            public int SpriteCount;
            public int CurrentInstanceId;
            public int StartInstanceId;
            public NativeList<CharacterEquipInstanceIndex> MoveId;
            public NativeList<CharacterEquipInstanceIndex> AddId;
            public NativeList<UpdateEquipBufferIndex> ChangeEquipId;
            public NativeList<CharacterEquipInstanceIndex> UnLoadIndex;

            public RenderMeshArray TemplateRenderMeshArray;
            public RenderFilterSettings TemplateRenderFilterSettings;
            public CharacterBaseColorPropertyComp TemplateCharacterBaseColorPropertyComp;
            public CharacterAnimationIndexPropertyComp TemplateCharacterAnimationIndexPropertyComp;
            public CharacterAnimationStartTimePropertyComp TemplateCharacterAnimationStartTimePropertyComp;
            public LocalToWorld TemplateLocalToWorld;
            public RenderBounds TemplateRenderBounds;
            public MaterialMeshInfo TemplateMaterialMeshInfo;
            public ChunkWorldRenderBounds TemplateChunkWorldRenderBounds;
            public EntitiesGraphicsChunkInfo TemplateEntitiesGraphicsChunkInfo;

            public readonly List<IComponentData> TemplateList = new List<IComponentData>();

            public void UpdateTemplateList(EntityManager entityManager, Entity templateEntity)
            {
                TemplateRenderMeshArray = entityManager.GetSharedComponentManaged<RenderMeshArray>(templateEntity);
                TemplateRenderFilterSettings = entityManager.GetSharedComponentManaged<RenderFilterSettings>(templateEntity);
                TemplateCharacterBaseColorPropertyComp = entityManager.GetComponentData<CharacterBaseColorPropertyComp>(templateEntity);
                TemplateCharacterAnimationIndexPropertyComp = entityManager.GetComponentData<CharacterAnimationIndexPropertyComp>(templateEntity);
                TemplateCharacterAnimationStartTimePropertyComp = entityManager.GetComponentData<CharacterAnimationStartTimePropertyComp>(templateEntity);
                TemplateMaterialMeshInfo = entityManager.GetComponentData<MaterialMeshInfo>(templateEntity);
                TemplateLocalToWorld = entityManager.GetComponentData<LocalToWorld>(templateEntity);
                TemplateRenderBounds = entityManager.GetComponentData<RenderBounds>(templateEntity);
                TemplateChunkWorldRenderBounds = entityManager.GetChunkComponentData<ChunkWorldRenderBounds>(templateEntity);
                TemplateEntitiesGraphicsChunkInfo = entityManager.GetChunkComponentData<EntitiesGraphicsChunkInfo>(templateEntity);
            }



            [Flags]
            public enum EntitiesGraphicsComponentFlags
            {
                None = 0,
                GameObjectConversion = 1 << 0,
                InMotionPass = 1 << 1,
                LightProbesBlend = 1 << 2,
                LightProbesCustom = 1 << 3,
                DepthSorted = 1 << 4,
                Baking = 1 << 5,
            }

            public ComponentTypeSet GenerateComponentTypes(EntitiesGraphicsComponentFlags flags)
            {
                List<ComponentType> components = new List<ComponentType>()
                {
                    // Absolute minimum set of components required by Entities Graphics
                    // to be considered for rendering. Entities without these components will
                    // not match queries and will never be rendered.
                    ComponentType.ReadWrite<WorldRenderBounds>(),
                    ComponentType.ReadWrite<RenderFilterSettings>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),
                    ComponentType.ChunkComponent<EntitiesGraphicsChunkInfo>(),
                    // Extra transform related components required to render correctly
                    // using many default SRP shaders. Custom shaders could potentially
                    // work without it.
                    ComponentType.ReadWrite<WorldToLocal_Tag>(),
                    // Components required by Entities Graphics package visibility culling.
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<PerInstanceCullingTag>(),
                };

                // RenderMesh is no longer used at runtime, it is only used during conversion.
                // At runtime all entities use RenderMeshArray.
                if (flags.HasFlag(EntitiesGraphicsComponentFlags.GameObjectConversion) | flags.HasFlag(EntitiesGraphicsComponentFlags.Baking))
                    components.Add(ComponentType.ReadWrite<RenderMesh>());

                if (!flags.HasFlag(EntitiesGraphicsComponentFlags.GameObjectConversion) | flags.HasFlag(EntitiesGraphicsComponentFlags.Baking))
                    components.Add(ComponentType.ReadWrite<RenderMeshArray>());

                // Baking uses TransformUsageFlags, and as such should not be explicitly adding LocalToWorld to anything
                if (!flags.HasFlag(EntitiesGraphicsComponentFlags.Baking))
                    components.Add(ComponentType.ReadWrite<LocalToWorld>());

                // Components required by objects that need to be rendered in per-object motion passes.
#if USE_HYBRID_MOTION_PASS
                if (flags.HasFlag(EntitiesGraphicsComponentFlags.InMotionPass))
                    components.Add(ComponentType.ReadWrite<BuiltinMaterialPropertyUnity_MatrixPreviousM>());
#endif

                if (flags.HasFlag(EntitiesGraphicsComponentFlags.LightProbesBlend))
                    components.Add(ComponentType.ReadWrite<BlendProbeTag>());
                else if (flags.HasFlag(EntitiesGraphicsComponentFlags.LightProbesCustom))
                    components.Add(ComponentType.ReadWrite<CustomProbeTag>());

                if (flags.HasFlag(EntitiesGraphicsComponentFlags.DepthSorted))
                    components.Add(ComponentType.ReadWrite<DepthSorted_Tag>());

                return new ComponentTypeSet(components.ToArray());
            }


            public void CopyComponentDataNew(EntityCommandBuffer entityCommandBuffer, Entity entity, int instanceId)
            {
                EntitiesGraphicsComponentFlags flags = EntitiesGraphicsComponentFlags.None;
                // flags |= EntitiesGraphicsComponentFlags.DepthSorted;
                var comp = GenerateComponentTypes(flags);
                entityCommandBuffer.AddComponent(entity, comp);
                entityCommandBuffer.AddComponent(entity, TemplateCharacterBaseColorPropertyComp);
                entityCommandBuffer.AddComponent(entity, new CharacterAnimationIndexPropertyComp(){Value = new Random(instanceId).Next(1,10)});
                entityCommandBuffer.AddComponent(entity, new CharacterAnimationStartTimePropertyComp(){Value =  UnityEngine.Random.value});
                entityCommandBuffer.AddComponent(entity, new CharacterEquipIndexBufferIndexPropertyComp() { Value = instanceId });
                
                
                // entityCommandBuffer.AddComponent(entity, new DepthSorted_Tag());
                entityCommandBuffer.SetSharedComponentManaged(entity, TemplateRenderMeshArray);
                entityCommandBuffer.SetSharedComponentManaged(entity, TemplateRenderFilterSettings);
                entityCommandBuffer.SetComponent(entity, TemplateMaterialMeshInfo);

                var localToWorld = new LocalToWorld();
                localToWorld.Value = float4x4.identity;
                localToWorld.Value.c3.xyz =  UnityEngine.Random.insideUnitSphere * new float3(20, 20, 0);
                entityCommandBuffer.SetComponent(entity,  localToWorld);


            }
        }


        public Dictionary<int, CharacterRendererDataChange> CharacterRendererDataChangeDic = new Dictionary<int, CharacterRendererDataChange>();


        public void UpdateCharacterRendererDataChange()
        {
            foreach (var characterRendererData in characterRendererDataDic)
            {
                CharacterRendererData data = characterRendererData.Value;
                if (!CharacterRendererDataChangeDic.TryGetValue(characterRendererData.Key, out var change))
                {
                    change = new CharacterRendererDataChange();
                    change.AddId = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
                    change.MoveId = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
                    change.UnLoadIndex = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
                    change.ChangeEquipId = new NativeList<UpdateEquipBufferIndex>(Allocator.Persistent);
                    change.SpriteCount = data.SpriteCount;
                    CharacterRendererDataChangeDic.Add(characterRendererData.Key, change);
                }

                change.CurrentInstanceId = data.EquipTexPosIdBuffer.UsedSize / data.SpriteCount;
                change.StartInstanceId = change.CurrentInstanceId;
                change.UpdateTemplateList(World.EntityManager, data.TemplateCharacterEntity);
                change.MoveId.Clear();
                change.AddId.Clear();
                change.UnLoadIndex.CopyFrom(data.UnLoadIndex);
                change.ChangeEquipId.Clear();
            }
        }


        public void ApplyCreateAndDel()
        {
            foreach (var characterRendererData in characterRendererDataDic)
            {
                CharacterRendererData data = characterRendererData.Value;
                if (CharacterRendererDataChangeDic.TryGetValue(characterRendererData.Key, out var change))
                {
                    data.UnLoadIndex.Clear();
                    data.UnLoadIndex.CopyFrom(change.UnLoadIndex);
                    foreach (var move in change.MoveId)
                    {
                        data.UnLoadIndex.Add(move);
                    }

                    if (change.MoveId.Length > 0)
                    {
                        data.RemoveUsedInstance(change.MoveId);
                    }

                    if (change.AddId.Length > 0)
                    {
                        var count = (change.CurrentInstanceId - change.StartInstanceId) * data.SpriteCount;
                        if (count > 0)
                        {
                            var temp = new CharacterEquipInstanceIndex[count];
                            data.EquipTexPosIdBuffer.AddData(temp);
                        }
                    }

                    if (change.ChangeEquipId.Length > 0)
                    {
                    
                        data.SetEquip( change.ChangeEquipId);
                    }
                    
                }
            }
        }


        protected override void OnCreate()
        {
            CharacterRenderStateComponentTypeHandle = GetComponentTypeHandle<CharacterRenderStateComponent>();
            var builder = new EntityQueryBuilder(Allocator.Temp);
            CharacterRenderDelQuery = builder.WithAll<CharacterRenderStateComponent>().WithAbsent<CharacterRenderIdComponent>().Build(this);
            CharacterRenderDelQuery.AddChangedVersionFilter(typeof(CharacterRenderStateComponent));
            builder.Reset();
        }


        protected override void OnUpdate()
        {
            CharacterRenderStateComponentTypeHandle.Update(this);
            EntityTypeHandle.Update(this);
            UpdateCharacterRendererDataChange();
            var entityStorageInfoLookup = GetBufferLookup<EquipmentDataChangeBuffer>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            BufferTypeHandle<EquipmentDataChangeBuffer> bufferTypeHandle = SystemAPI.GetBufferTypeHandle<EquipmentDataChangeBuffer>();
            foreach (var (statusCompRW, entity) in SystemAPI.Query<RefRO<CharacterRenderStateComponent>>().
                         WithChangeFilter<CharacterRenderStateComponent>().WithEntityAccess())
            {
                var statusComp = statusCompRW.ValueRO;
                var change = CharacterRendererDataChangeDic[statusComp.TypeId];

                if (statusComp.State == CharacterRenderState.Destroy)
                {
                    ecb.RemoveComponent<CharacterRenderStateComponent>(entity);
                    ecb.DestroyEntity(entity);
                    change.MoveId.Add(new CharacterEquipInstanceIndex(statusComp.InstanceId));
                }

                if (statusComp.State == CharacterRenderState.PreCreate)
                {
                    ecb.AddSharedComponent(entity, new CharacterRenderIdComponent() { TypeId = statusComp.TypeId });
                    statusComp.State = CharacterRenderState.Worrking;
                    if (change.UnLoadIndex.Length > 0)
                    {
                        statusComp.InstanceId = change.UnLoadIndex[0].InstanceObject;
                        change.UnLoadIndex.RemoveAt(0);
                        ecb.SetComponent(entity, statusComp);
                        change.AddId.Add(new CharacterEquipInstanceIndex(statusComp.InstanceId));
                        change.CopyComponentDataNew(ecb, entity, statusComp.InstanceId);
                    }
                    else
                    {
                        var instanceId = change.CurrentInstanceId;
                        statusComp.InstanceId = instanceId;
                        change.CurrentInstanceId = instanceId + 1;
                        change.AddId.Add(new CharacterEquipInstanceIndex() { InstanceObject = instanceId });
                        ecb.SetComponent(entity, statusComp);
                        change.CopyComponentDataNew(ecb, entity, statusComp.InstanceId);
                        
                    }

                    if (entityStorageInfoLookup.TryGetBuffer(entity,out var changeBuffer))
                    {
                        foreach (var buffer in changeBuffer)
                        {
                            var id = change.SpriteCount * statusComp.InstanceId + buffer.Position;
                            change.ChangeEquipId.Add(new UpdateEquipBufferIndex(id,buffer.NewId));
                        }
                        ecb.RemoveComponent<EquipmentDataChangeBuffer>(entity);
                    }
                }
            }

            foreach (var (changeBuffer,statusComp,entity ) in SystemAPI.Query<DynamicBuffer<EquipmentDataChangeBuffer>,RefRO<CharacterRenderStateComponent>>().WithEntityAccess())
            {
                var status = statusComp.ValueRO;

                if (status.State == CharacterRenderState.PreCreate)
                {
                    continue;
                }
                
                var change = CharacterRendererDataChangeDic[status.TypeId];

                foreach (var buffer in changeBuffer)
                {
                    var id = change.SpriteCount * status.InstanceId + buffer.Position;
                    change.ChangeEquipId.Add(new UpdateEquipBufferIndex(id,buffer.NewId));
                }
                
                ecb.RemoveComponent<EquipmentDataChangeBuffer>(entity);
                
            }

            
            ApplyCreateAndDel();
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        public static void RegisterSprite(int charcterTypeId,int equipTypeId, Sprite sprite)
        {
            if (characterRendererDataDic.TryGetValue(charcterTypeId, out var data))
            {
                data.RegisterSprite(equipTypeId,sprite);
            }
        }
        
        public static int RegisterCharacterRender(BakedCharacterAsset bakedCharacterAsset)
        {
            if (bakedCharacterAsset == null)
            {
                return -1;
            }

            if (CharacterRender.TryGetValue(bakedCharacterAsset, out var id))
            {
                return id;
            }
            else
            {
                id = CharacterRender.Count + 1;
                CharacterRender.Add(bakedCharacterAsset, id);
                characterRendererDataDic.Add(id, new CharacterRendererData(bakedCharacterAsset));
            }

            return id;
        }

        public static CharacterRendererData GetCharacterRendererData(int id)
        {
            if (characterRendererDataDic.TryGetValue(id, out var data))
            {
                return data;
            }

            return null;
        }
    }
}