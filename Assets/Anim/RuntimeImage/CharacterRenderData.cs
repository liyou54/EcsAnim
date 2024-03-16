using System;
using System.Collections.Generic;
using Anim.Shader;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anim.RuntimeImage
{
    public class CharacterRendererData
    {
        private RuntimeImagePacker ImagePacker;
        private ComputeShader equipArrayIndexComputeShader;
        public BufferDataWithSize<CharacterEquipInstanceIndex> EquipTexPosIdBuffer;
        private BufferDataWithSize<Color> EquipColorBuffer;

        private BufferDataWithSize<CharacterEquipInstanceIndex> MoveEquipBufferIndexBuffer;

        public BufferDataWithSize<UpdateEquipBufferIndex> UpdateEquipBufferIndexBuffer;
        private ComputeBuffer AnimLengthBuffer;
        private BufferDataWithSize<RuntimeImagePacker.SpriteData> EquipInfoBuffer;
        public Material CharacterMaterial;
        public Mesh CharacterMesh;
        public BatchMaterialID BatchMaterialID;
        public BatchMeshID BatchMeshID;
        public NativeList<CharacterEquipInstanceIndex> UnLoadIndex;
        private int CurrentUnLoadIndex;
        private int CurrentUseIndex;
        public int SpriteCount;

        public Entity TemplateCharacterEntity;
        private NativeArray<NativeList<int>> EquipList;

        public NativeArray<NativeList<int>> GetEquipList()
        {
            return EquipList;
        }

        public void RemoveUsedInstance(NativeList<CharacterEquipInstanceIndex> changeMoveId)
        {
            var array = changeMoveId.ToArray(Allocator.Temp);
            MoveEquipBufferIndexBuffer.AddData(array.ToArray());
            array.Dispose();
            equipArrayIndexComputeShader.SetBuffer(0, "_MoveEquipBufferIndexBuffer", MoveEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, "_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, "_EquipColorBuffer", EquipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt( "SpriteCount", SpriteCount);
            equipArrayIndexComputeShader.Dispatch(0, changeMoveId.Length, 1, 1);
        }
        
        public void SetEquip(NativeList<UpdateEquipBufferIndex> updateEquipBuffer)
        {
            
            var array = updateEquipBuffer.ToArray(Allocator.Temp);
            UpdateEquipBufferIndexBuffer.ResetCount();
            UpdateEquipBufferIndexBuffer.AddData(array.ToArray());
            array.Dispose();
            
            equipArrayIndexComputeShader.SetBuffer(1, "_SetEquipTexPosIdBuffer", UpdateEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, "_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, "_EquipColorBuffer", EquipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt( "SpriteCount", SpriteCount);
            equipArrayIndexComputeShader.Dispatch(1, updateEquipBuffer.Length, 1, 1);
        }

        public CharacterRendererData(BakedCharacterAsset bakedCharacterAsset)
        {
            SpriteCount = bakedCharacterAsset.SpriteRenderCount;
            EquipList = new NativeArray<NativeList<int>>(SpriteCount, Allocator.Persistent);
            for (int i = 0; i < SpriteCount; i++)
            {
                EquipList[i] = new NativeList<int>(128, Allocator.Persistent);
            }

            UpdateEquipBufferIndexBuffer = new BufferDataWithSize<UpdateEquipBufferIndex>(1024, null);
            EquipInfoBuffer = new BufferDataWithSize<RuntimeImagePacker.SpriteData>(1024, OnEquipInfoBufferChange);
            UnLoadIndex = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
            RegisterToMeshRender(bakedCharacterAsset);
            InitSpriteImagePacker(bakedCharacterAsset);
            InitRenderTexture(bakedCharacterAsset.SpriteRenderCount);
            SetAnimLength(bakedCharacterAsset);
            CreateDefaultEquip(bakedCharacterAsset);
            CharacterMaterial.SetBuffer("_EquipInfoBuffer", EquipInfoBuffer.buffer);
            CreateCharacterTemplate();
            MoveEquipBufferIndexBuffer = new BufferDataWithSize<CharacterEquipInstanceIndex>(1024, null);
            equipArrayIndexComputeShader = GameObject.Instantiate(bakedCharacterAsset.WriteEquipArrayIndexComputeShader);
        }
        
        private void InitRenderTexture(int spriteCount)
        {
            EquipTexPosIdBuffer = new BufferDataWithSize<CharacterEquipInstanceIndex>(spriteCount * 4, OnEquipTexPosIdBufferChange);
            EquipColorBuffer = new BufferDataWithSize<Color>(spriteCount * 4, OnEquipColorBufferSizeChange);
            CharacterMaterial.SetBuffer("_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            CharacterMaterial.SetBuffer("_EquipColorBuffer", EquipColorBuffer.buffer);
        }


        private void OnEquipTexPosIdBufferChange(ComputeBuffer buffer)
        {
            CharacterMaterial.SetBuffer("_EquipTexPosIdBuffer", buffer);
        }

        private void OnEquipColorBufferSizeChange(ComputeBuffer buffer)
        {
            CharacterMaterial.SetBuffer("_EquipColorBuffer", buffer);
        }

        private void OnEquipInfoBufferChange(ComputeBuffer buffer)
        {
            CharacterMaterial.SetBuffer("_EquipInfoBuffer", buffer);
        }

        private void InitSpriteImagePacker(BakedCharacterAsset bakedCharacterAsset)
        {
            ImagePacker = new RuntimeImagePacker(512, 512, 2);
            ImagePacker.Create();
            CharacterMaterial.SetTexture("_EquipSpriteTex", ImagePacker.GetTexture());
            CharacterMaterial.SetInt("_SpriteQuadCount", bakedCharacterAsset.SpriteRenderCount);
            CharacterMaterial.SetVector("_EquipIndexTexSizeData", new Vector4(512, 512));
            CharacterMaterial.SetVector("_AnimTexSizeData", new Vector4(512, 512));

        }

        
        private void CreateDefaultEquip(BakedCharacterAsset bakedCharacterAsset)
        {
            List<RuntimeImagePacker.SpriteData> spriteDataAddList = new(bakedCharacterAsset.DefaultSprites.Count);
            List<CharacterEquipInstanceIndex> equipTexPosIdList = new(bakedCharacterAsset.DefaultSprites.Count);
            // 创建默认的装备
            for (int i = 0; i < bakedCharacterAsset.DefaultSprites.Count; i++)
            {
                var sprite = bakedCharacterAsset.DefaultSprites[i];

                if (sprite == null)
                {
                    equipTexPosIdList.Add(new CharacterEquipInstanceIndex(){InstanceObject = -1});
                    continue;
                }

                var hasAdd = ImagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
                ImagePacker.TryGetSpriteDataByIndex(index, out var data);

                if (!hasAdd)
                {
                    spriteDataAddList.Add(data);
                    EquipList[i].Add(index);
                }

                equipTexPosIdList.Add( new CharacterEquipInstanceIndex(){InstanceObject = index + 1});
            }
            Debug.Log(spriteDataAddList.Count);
            EquipInfoBuffer.AddData(spriteDataAddList.ToArray());
            EquipTexPosIdBuffer.AddData(equipTexPosIdList.ToArray());
        }


        private void SetAnimLength(BakedCharacterAsset bakedCharacterAsset)
        {
            AnimLengthBuffer = new ComputeBuffer(bakedCharacterAsset.ClipInfo.Count, 12);
            var clipInfoArray = new List<AnimClipInfo>();
            for (int i = 0; i < bakedCharacterAsset.ClipInfo.Count; i++)
            {
                var clipInfo = bakedCharacterAsset.ClipInfo[i];
                var clipInfoRuntime = new AnimClipInfo();
                clipInfoRuntime.StartFrameTexIndex = clipInfo.StartFrameTexIndex;
                clipInfoRuntime.FrameCount = clipInfo.FrameCount;
                clipInfoRuntime.Duration = clipInfo.Duration;
                clipInfoArray.Add(clipInfoRuntime);
            }

            AnimLengthBuffer.SetData(clipInfoArray);
            CharacterMaterial.SetBuffer("_AnimClipInfoBuffer", AnimLengthBuffer);
        }

        private void RegisterToMeshRender(BakedCharacterAsset bakedCharacterAsset)
        {
            CharacterMaterial = bakedCharacterAsset.CharacterMaterial;
            CharacterMaterial.SetTexture("_PosAndScaleAnimTex", bakedCharacterAsset.PosScaleTex);
            CharacterMaterial.SetTexture("_RotationRadiusAnimTex", bakedCharacterAsset.RotTex);
            CharacterMesh = bakedCharacterAsset.mesh;

            var world = World.DefaultGameObjectInjectionWorld;
            var entityGraphSystem = world.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            BatchMaterialID = entityGraphSystem.RegisterMaterial(CharacterMaterial);
            BatchMeshID = entityGraphSystem.RegisterMesh(CharacterMesh);
        }

        private void CreateCharacterTemplate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            TemplateCharacterEntity = entityManager.CreateEntity(
                // typeof(DisableRendering),
                typeof(CharacterBaseColorPropertyComp),
                typeof(CharacterAnimationIndexPropertyComp),
                typeof(CharacterAnimationStartTimePropertyComp),
                typeof(CharacterEquipIndexBufferIndexPropertyComp)
            );

            var renderMeshDesc = new RenderMeshDescription()
            {
                FilterSettings = RenderFilterSettings.Default,
                LightProbeUsage = LightProbeUsage.Off,
            };

            RenderMeshUtility.AddComponents(
                TemplateCharacterEntity,
                entityManager,
                renderMeshDesc,
                new RenderMeshArray(new Material[]{CharacterMaterial}, new Mesh[]{CharacterMesh}),
                new MaterialMeshInfo(BatchMaterialID, BatchMeshID)
            );
        }


        public void RegisterSprite(int equipTypeIndex ,Sprite sprite)
        {
            var hasAdd = ImagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
            ImagePacker.TryGetSpriteDataByIndex(index, out var data);
            if (!hasAdd)
            {
                EquipList[equipTypeIndex].Add(index);
                EquipInfoBuffer.AddData(new RuntimeImagePacker.SpriteData[]{data});
            }
        }
    }
}