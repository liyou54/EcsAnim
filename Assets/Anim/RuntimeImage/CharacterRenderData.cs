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
        public int Id;
        private RuntimeImagePacker ImagePacker;
        private ComputeShader equipArrayIndexComputeShader;
        public BufferDataWithSize<CharacterRenderInstanceComponent> EquipTexPosIdBuffer;
        private BufferDataWithSize<Color> _equipColorBuffer;
        private readonly BufferDataWithSize<CharacterRenderInstanceComponent> _moveEquipBufferIndexBuffer;
        private readonly BufferDataWithSize<UpdateEquipBufferIndex> _updateEquipBufferIndexBuffer;
        private ComputeBuffer _animLengthBuffer;
        private readonly BufferDataWithSize<RuntimeImagePacker.SpriteData> _equipInfoBuffer;
        private Material _characterMaterial;
        private Mesh _characterMesh;
        public BatchMaterialID BatchMaterialID;
        public BatchMeshID BatchMeshID;
        public NativeList<CharacterRenderInstanceComponent> UnLoadIndex;
        public int SpriteCount;

 
        
        public Entity TemplateCharacterEntity;
        private NativeArray<NativeList<int>> EquipList;

        public NativeArray<NativeList<int>> GetEquipList()
        {
            return EquipList;
        }

        public int GetCurrentInstanceId()
        {
            return EquipTexPosIdBuffer.UsedSize / SpriteCount;
        }

        public void RemoveUsedInstance(NativeArray<CharacterRenderInstanceComponent> changeMoveId)
        {
            _moveEquipBufferIndexBuffer.AddData(changeMoveId.ToArray());
            equipArrayIndexComputeShader.SetBuffer(0, "_MoveEquipBufferIndexBuffer", _moveEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, "_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, "_EquipColorBuffer", _equipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt("SpriteCount", SpriteCount);
            equipArrayIndexComputeShader.Dispatch(0, changeMoveId.Length, 1, 1);
        }

        public void SetEquip(NativeArray<UpdateEquipBufferIndex> updateEquipBuffer)
        {
            _updateEquipBufferIndexBuffer.ResetCount();
            _updateEquipBufferIndexBuffer.AddData(updateEquipBuffer.ToArray());
            equipArrayIndexComputeShader.SetBuffer(1, "_SetEquipTexPosIdBuffer", _updateEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, "_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, "_EquipColorBuffer", _equipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt("SpriteCount", SpriteCount);
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

            _updateEquipBufferIndexBuffer = new BufferDataWithSize<UpdateEquipBufferIndex>(1024, null);
            _equipInfoBuffer = new BufferDataWithSize<RuntimeImagePacker.SpriteData>(1024, OnEquipInfoBufferChange);
            UnLoadIndex = new NativeList<CharacterRenderInstanceComponent>(Allocator.Persistent);
            RegisterToMeshRender(bakedCharacterAsset);
            InitSpriteImagePacker(bakedCharacterAsset);
            InitRenderTexture(bakedCharacterAsset.SpriteRenderCount);
            SetAnimLength(bakedCharacterAsset);
            CreateDefaultEquip(bakedCharacterAsset);
            _characterMaterial.SetBuffer("_EquipInfoBuffer", _equipInfoBuffer.buffer);
            _moveEquipBufferIndexBuffer = new BufferDataWithSize<CharacterRenderInstanceComponent>(1024, null);
            equipArrayIndexComputeShader = GameObject.Instantiate(bakedCharacterAsset.WriteEquipArrayIndexComputeShader);
        }

        private void InitRenderTexture(int spriteCount)
        {
            EquipTexPosIdBuffer = new BufferDataWithSize<CharacterRenderInstanceComponent>(spriteCount * 4, OnEquipTexPosIdBufferChange);
            _equipColorBuffer = new BufferDataWithSize<Color>(spriteCount * 4, OnEquipColorBufferSizeChange);
            _characterMaterial.SetBuffer("_EquipTexPosIdBuffer", EquipTexPosIdBuffer.buffer);
            _characterMaterial.SetBuffer("_EquipColorBuffer", _equipColorBuffer.buffer);
        }


        private void OnEquipTexPosIdBufferChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer("_EquipTexPosIdBuffer", buffer);
        }

        private void OnEquipColorBufferSizeChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer("_EquipColorBuffer", buffer);
        }

        private void OnEquipInfoBufferChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer("_EquipInfoBuffer", buffer);
        }

        private void InitSpriteImagePacker(BakedCharacterAsset bakedCharacterAsset)
        {
            ImagePacker = new RuntimeImagePacker(512, 512, 2);
            ImagePacker.Create();
            _characterMaterial.SetTexture("_EquipSpriteTex", ImagePacker.GetTexture());
            _characterMaterial.SetInt("_SpriteQuadCount", bakedCharacterAsset.SpriteRenderCount);
            _characterMaterial.SetVector("_EquipIndexTexSizeData", new Vector4(512, 512));
            _characterMaterial.SetVector("_AnimTexSizeData", new Vector4(512, 512));
        }


        private void CreateDefaultEquip(BakedCharacterAsset bakedCharacterAsset)
        {
            List<RuntimeImagePacker.SpriteData> spriteDataAddList = new(bakedCharacterAsset.DefaultSprites.Count);
            List<CharacterRenderInstanceComponent> equipTexPosIdList = new(bakedCharacterAsset.DefaultSprites.Count);
            // 创建默认的装备
            for (int i = 0; i < bakedCharacterAsset.DefaultSprites.Count; i++)
            {
                var sprite = bakedCharacterAsset.DefaultSprites[i];

                if (sprite == null)
                {
                    equipTexPosIdList.Add(new CharacterRenderInstanceComponent(-1));
                    continue;
                }

                var hasAdd = ImagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
                ImagePacker.TryGetSpriteDataByIndex(index, out var data);

                if (!hasAdd)
                {
                    spriteDataAddList.Add(data);
                    EquipList[i].Add(index);
                }

                equipTexPosIdList.Add(new CharacterRenderInstanceComponent(index + 1));
            }

            Debug.Log(spriteDataAddList.Count);
            _equipInfoBuffer.AddData(spriteDataAddList.ToArray());
            EquipTexPosIdBuffer.AddData(equipTexPosIdList.ToArray());
        }


        private void SetAnimLength(BakedCharacterAsset bakedCharacterAsset)
        {
            _animLengthBuffer = new ComputeBuffer(bakedCharacterAsset.ClipInfo.Count, 12);
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

            _animLengthBuffer.SetData(clipInfoArray);
            _characterMaterial.SetBuffer("_AnimClipInfoBuffer", _animLengthBuffer);
        }

        private void RegisterToMeshRender(BakedCharacterAsset bakedCharacterAsset)
        {
            _characterMaterial = bakedCharacterAsset.CharacterMaterial;
            _characterMaterial.SetTexture("_PosAndScaleAnimTex", bakedCharacterAsset.PosScaleTex);
            _characterMaterial.SetTexture("_RotationRadiusAnimTex", bakedCharacterAsset.RotTex);
            _characterMesh = bakedCharacterAsset.mesh;

            var world = World.DefaultGameObjectInjectionWorld;
            var entityGraphSystem = world.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            BatchMaterialID = entityGraphSystem.RegisterMaterial(_characterMaterial);
            BatchMeshID = entityGraphSystem.RegisterMesh(_characterMesh);
        }




        public void RegisterSprite(int equipTypeIndex, Sprite sprite)
        {
            var hasAdd = ImagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
            ImagePacker.TryGetSpriteDataByIndex(index, out var data);
            if (!hasAdd)
            {
                _equipInfoBuffer.AddData(new RuntimeImagePacker.SpriteData[] { data });
            }

            EquipList[equipTypeIndex].Add(index);
        }
    }
}