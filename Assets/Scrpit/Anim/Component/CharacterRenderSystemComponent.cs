using System;
using System.Collections.Generic;
using Scrpit.Anim.Asset;
using Scrpit.Config;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Anim.RuntimeImage
{
    public struct AnimBlobKey:IEquatable<AnimBlobKey>
    {
        public int Id;
        public FixedString32Bytes Name;
        
        public AnimBlobKey(int id, string name)
        {
            Id = id;
            Name = name;
        }
        
        public bool Equals(AnimBlobKey other)
        {
            return Id == other.Id && Name == other.Name;
        }
    }
    public class CharacterRendererData
    {
        public int Id;
        private RuntimeImagePacker _imagePacker;
        private readonly ComputeShader equipArrayIndexComputeShader;
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

        private NativeArray<NativeList<int>> EquipList;

        private Dictionary<string, BlobAssetReference<AnimClipBlob>> _clipInfoDic = new Dictionary<string, BlobAssetReference<AnimClipBlob>>();

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
            equipArrayIndexComputeShader.SetBuffer(0, MoveEquipBufferIndexBufferId, _moveEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, EquipTexPosIdBufferId, EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(0, EquipColorBufferId, _equipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt(SpriteCountId, SpriteCount);
            equipArrayIndexComputeShader.Dispatch(0, changeMoveId.Length, 1, 1);
        }

        public void SetEquip(NativeArray<UpdateEquipBufferIndex> updateEquipBuffer)
        {
            _updateEquipBufferIndexBuffer.ResetCount();
            _updateEquipBufferIndexBuffer.AddData(updateEquipBuffer.ToArray());
            equipArrayIndexComputeShader.SetBuffer(1, SetEquipTexPosIdBuffer, _updateEquipBufferIndexBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, EquipTexPosIdBufferId, EquipTexPosIdBuffer.buffer);
            equipArrayIndexComputeShader.SetBuffer(1, EquipColorBufferId, _equipColorBuffer.buffer);
            equipArrayIndexComputeShader.SetInt(SpriteCountId, SpriteCount);
            equipArrayIndexComputeShader.Dispatch(1, updateEquipBuffer.Length, 1, 1);
        }

        public CharacterRendererData(BakedCharacterAsset bakedCharacterAsset, int id)
        {
            SpriteCount = bakedCharacterAsset.SpriteRenderCount;
            Id = id;
            EquipList = new NativeArray<NativeList<int>>(SpriteCount, Allocator.Persistent);
            for (int i = 0; i < SpriteCount; i++)
            {
                EquipList[i] = new NativeList<int>(128, Allocator.Persistent);
            }

            // TODO Buffer大小传入
            _updateEquipBufferIndexBuffer = new BufferDataWithSize<UpdateEquipBufferIndex>(1024, null);
            _equipInfoBuffer = new BufferDataWithSize<RuntimeImagePacker.SpriteData>(1024, OnEquipInfoBufferChange);
            UnLoadIndex = new NativeList<CharacterRenderInstanceComponent>(Allocator.Persistent);
            RegisterToMeshRender(bakedCharacterAsset);
            InitSpriteImagePacker(bakedCharacterAsset);
            InitRenderTexture(bakedCharacterAsset.SpriteRenderCount);
            SetAnimLength(bakedCharacterAsset);
            CreateDefaultEquip(bakedCharacterAsset);
            _characterMaterial.SetBuffer(EquipInfoBufferId, _equipInfoBuffer.buffer);
            _moveEquipBufferIndexBuffer = new BufferDataWithSize<CharacterRenderInstanceComponent>(1024, null);
            equipArrayIndexComputeShader = Object.Instantiate(bakedCharacterAsset.WriteEquipArrayIndexComputeShader);
            SetCharacterBlobInfo(bakedCharacterAsset);
        }

        public void RegisterSprite(int equipTypeIndex, Sprite sprite)
        {
            var hasAdd = _imagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
            _imagePacker.TryGetSpriteDataByIndex(index, out var data);
            if (!hasAdd)
            {
                _equipInfoBuffer.AddData(new RuntimeImagePacker.SpriteData[] { data });
            }

            EquipList[equipTypeIndex].Add(index);
        }


        private static readonly int MoveEquipBufferIndexBufferId = UnityEngine.Shader.PropertyToID("_MoveEquipBufferIndexBuffer");
        private static readonly int EquipTexPosIdBufferId = UnityEngine.Shader.PropertyToID("_EquipTexPosIdBuffer");
        private static readonly int EquipColorBufferId = UnityEngine.Shader.PropertyToID("_EquipColorBuffer");
        private static readonly int SpriteCountId = UnityEngine.Shader.PropertyToID("SpriteCount");
        private static readonly int SpriteQuadCountId = UnityEngine.Shader.PropertyToID("_SpriteQuadCount");
        private static readonly int SetEquipTexPosIdBuffer = UnityEngine.Shader.PropertyToID("_SetEquipTexPosIdBuffer");
        private static readonly int EquipInfoBufferId = UnityEngine.Shader.PropertyToID("_EquipInfoBuffer");
        private static readonly int EquipSpriteTex = UnityEngine.Shader.PropertyToID("_EquipSpriteTex");
        private static readonly int EquipIndexTexSizeData = UnityEngine.Shader.PropertyToID("_EquipIndexTexSizeData");
        private static readonly int AnimTexSizeData = UnityEngine.Shader.PropertyToID("_AnimTexSizeData");
        private static readonly int AnimClipInfoBuffer = UnityEngine.Shader.PropertyToID("_AnimClipInfoBuffer");
        private static readonly int PosAndScaleAnimTex = UnityEngine.Shader.PropertyToID("_PosAndScaleAnimTex");
        private static readonly int RotationRadiusAnimTex = UnityEngine.Shader.PropertyToID("_RotationRadiusAnimTex");


        private void InitRenderTexture(int spriteCount)
        {
            EquipTexPosIdBuffer = new BufferDataWithSize<CharacterRenderInstanceComponent>(spriteCount * 4, OnEquipTexPosIdBufferChange);
            _equipColorBuffer = new BufferDataWithSize<Color>(spriteCount * 4, OnEquipColorBufferSizeChange);
            _characterMaterial.SetBuffer(EquipTexPosIdBufferId, EquipTexPosIdBuffer.buffer);
            _characterMaterial.SetBuffer(EquipColorBufferId, _equipColorBuffer.buffer);
        }


        private void OnEquipTexPosIdBufferChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer(EquipTexPosIdBufferId, buffer);
        }

        private void OnEquipColorBufferSizeChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer(EquipColorBufferId, buffer);
        }

        private void OnEquipInfoBufferChange(ComputeBuffer buffer)
        {
            _characterMaterial.SetBuffer(EquipInfoBufferId, buffer);
        }

        private void InitSpriteImagePacker(BakedCharacterAsset bakedCharacterAsset)
        {
            _imagePacker = new RuntimeImagePacker(512, 512, 2);
            _imagePacker.Create();
            _characterMaterial.SetTexture(EquipSpriteTex, _imagePacker.GetTexture());
            _characterMaterial.SetInt(SpriteQuadCountId, bakedCharacterAsset.SpriteRenderCount);
            _characterMaterial.SetVector(EquipIndexTexSizeData, new Vector4(512, 512));
            _characterMaterial.SetVector(AnimTexSizeData, new Vector4(512, 512));
        }


        private void CreateDefaultEquip(BakedCharacterAsset bakedCharacterAsset)
        {
            List<RuntimeImagePacker.SpriteData> spriteDataAddList = new(bakedCharacterAsset.SpriteRenderCount);
            CharacterRenderInstanceComponent[] equipTexPosIdList = new CharacterRenderInstanceComponent[bakedCharacterAsset.SpriteRenderCount];
            // 创建默认的装备
            for (int i = 0; i < bakedCharacterAsset.EquipInfos.Count; i++)
            {
                var equipInfo = bakedCharacterAsset.EquipInfos[i];

                for (int j = 0; j < equipInfo.EquipCellInfos.Count; j++)
                {
                    var equipCellInfo = equipInfo.EquipCellInfos[j];
                    var sprite = equipCellInfo.DefaultSprite;
                    if (sprite == null)
                    {
                        equipTexPosIdList[equipCellInfo.index] = -1;
                        continue;
                    }

                    var hasAdd = _imagePacker.TryGetOrRegisterSpriteIndex(sprite, out var index);
                    _imagePacker.TryGetSpriteDataByIndex(index, out var data);
                    if (!hasAdd)
                    {
                        spriteDataAddList.Add(data);
                        EquipList[equipCellInfo.index].Add(index);
                    }

                    equipTexPosIdList[equipCellInfo.index] = index + 1;
                }
            }

            Debug.Log(spriteDataAddList.Count);
            _equipInfoBuffer.AddData(spriteDataAddList.ToArray());
            EquipTexPosIdBuffer.AddData(equipTexPosIdList);
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
            _characterMaterial.SetBuffer(AnimClipInfoBuffer, _animLengthBuffer);
        }
        
        
        private void SetCharacterBlobInfo(BakedCharacterAsset bakedCharacterAsset)
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var characterBlob = ref blobBuilder.ConstructRoot<CharacterInfoBlob>();
            var array = blobBuilder.Allocate(ref characterBlob.AnimBlob, bakedCharacterAsset.ClipInfo.Count);
            for (int i = 0; i < bakedCharacterAsset.ClipInfo.Count; i++)
            {
                var clipInfo = bakedCharacterAsset.ClipInfo[i];
                var blobInfo = GetAnimBlobAsset(i, clipInfo);
                var key = new AnimBlobKey(Id, clipInfo.Name);
                BlobCacheManager<AnimBlobKey, AnimClipBlob>.Add(key, blobInfo);
                array[i] = clipInfo.Name;
            }

            var blob = blobBuilder.CreateBlobAssetReference<CharacterInfoBlob>(Allocator.Persistent);
            BlobCacheManager<int, CharacterInfoBlob>.Add(Id, blob);
        }

        private BlobAssetReference<AnimClipBlob> GetAnimBlobAsset(int id, ClipInfo clipInfo)
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            var builder = blobBuilder;
            ref var root = ref builder.ConstructRoot<AnimClipBlob>();
            root.Id = id;
            builder.AllocateString(ref root.Name, clipInfo.Name);
            root.Duration = clipInfo.Duration;
            root.FrameCount = clipInfo.FrameCount;
            root.StartFrameTexIndex = clipInfo.StartFrameTexIndex;
            return builder.CreateBlobAssetReference<AnimClipBlob>(Allocator.Persistent);
        }

        private void RegisterToMeshRender(BakedCharacterAsset bakedCharacterAsset)
        {
            _characterMaterial = bakedCharacterAsset.CharacterMaterial;
            _characterMaterial.SetTexture(PosAndScaleAnimTex, bakedCharacterAsset.PosScaleTex);
            _characterMaterial.SetTexture(RotationRadiusAnimTex, bakedCharacterAsset.RotTex);
            _characterMesh = bakedCharacterAsset.mesh;

            var world = World.DefaultGameObjectInjectionWorld;
            var entityGraphSystem = world.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            BatchMaterialID = entityGraphSystem.RegisterMaterial(_characterMaterial);
            BatchMeshID = entityGraphSystem.RegisterMesh(_characterMesh);
        }
    }

    public class CharacterRenderSystemComponent : IComponentData
    {
        public Dictionary<BakedCharacterAsset, int> CharacterRender = new Dictionary<BakedCharacterAsset, int>();
        public Dictionary<int, CharacterRendererData> CharacterRendererDataDic = new Dictionary<int, CharacterRendererData>();
    }
}