using System.Collections.Generic;
using DaVikingCode.RectanglePacking;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Anim.RuntimeImage
{
    public class RuntimeImagePacker
    {
        
        private RectanglePacker _mPacker;
        private RenderTexture _mTexture;
        private Material _blitMaterial;

        public RuntimeImagePacker(int width, int height, int padding)
        {
            _mPacker = new RectanglePacker(width, height, padding);
        }

        public void Create()
        {
            _blitMaterial = CoreUtils.CreateEngineMaterial("Custom/BlitToRect");
            _rectArray = new NativeArray<SpriteData>(1024, Allocator.Persistent);
            _spriteIndexMap = new Dictionary<Sprite, int>();
            _mTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                useMipMap = false,
                enableRandomWrite = true
            };
            _mTexture.Create();
        }

        public  RenderTexture  GetTexture()
        {
            return _mTexture;
        }

        public void Dispose()
        {
            _mTexture.Release();
            _mTexture = null;
            _rectArray.Dispose();
        }

        public void RegisterSprite(Sprite sprite)
        {
            if (_spriteIndexMap.ContainsKey(sprite))
            {
                return;
            }

            UpdatePackingBox(sprite);
            _spriteIndexMap.Add(sprite, CurrentId - 1);
        }

        public bool TryGetSpriteIndex(Sprite sprite, out int index)
        {
            return _spriteIndexMap.TryGetValue(sprite, out index);
        }
        
        public bool TryGetSpriteDataByIndex(int index, out SpriteData data)
        {
            if (index < 0 || index >= _rectArray.Length)
            {
                data = new SpriteData();
                return false;
            }

            data = _rectArray[index];
            return true;
        }
        
        public bool TryGetOrRegisterSpriteIndex(Sprite sprite, out int index)
        {
            if (_spriteIndexMap.TryGetValue(sprite, out index))
            {
                return true;
            }

            UpdatePackingBox(sprite);
            index = CurrentId - 1;
            _spriteIndexMap.Add(sprite, index);
            return false;
        }


        public NativeArray<SpriteData> GetSpriteData()
        {
            return _rectArray;
        }

        private NativeArray<SpriteData> _rectArray;
        private Dictionary<Sprite, int> _spriteIndexMap = new Dictionary<Sprite, int>();

        private static readonly int MainTex = UnityEngine.Shader.PropertyToID("_MainTex");
        private static readonly int SrcTexSt = UnityEngine.Shader.PropertyToID("_SrcTex_ST");
        private static readonly int Rect1 = UnityEngine.Shader.PropertyToID("_Rect");

        private int CurrentId = 0;

        private void UpdatePackingBox(Sprite sprite)
        {
            // 获取精灵的纹理矩形
            var floatRect = sprite.textureRect;
            // 将纹理矩形转换为整数矩形
            var rect = new int4((int)floatRect.x, (int)floatRect.y, (int)floatRect.width, (int)floatRect.height);

            // 插入矩形并进行包装
            _mPacker.insertRectangle(rect.z, rect.w, CurrentId);
            _mPacker.packRectangles();

            // 获取内部矩形和目标矩形
            var innerId = _mPacker.getRectangleId(CurrentId);
            var rectInner = _mPacker.getRectangle(innerId, new IntegerRectangle());
            var destinationRect = new int4(rectInner.x, rectInner.y, rectInner.width, rectInner.height);

            // 填充颜色
            FillColor(rect, destinationRect, sprite.texture);

            // 计算 UV 坐标
            var destinationUV = new float4(
                (float)rectInner.x / _mTexture.width,
                (float)rectInner.y / _mTexture.height,
                (float)rectInner.width / _mTexture.width,
                (float)rectInner.height / _mTexture.height
            );

            // 创建 SpriteData 对象
            var data = new SpriteData
            {
                TillOffset = destinationUV,
                SizePivot = new float4(sprite.rect.size / sprite.pixelsPerUnit, sprite.pivot / sprite.rect.size)
            };

            // 如果需要，扩展数组
            if (CurrentId >= _rectArray.Length)
            {
                var newArray = new NativeArray<SpriteData>(_rectArray.Length * 2, Allocator.Persistent);
                _rectArray.CopyTo(newArray);
                _rectArray.Dispose();
                _rectArray = newArray;
            }

            // 将数据存入数组
            _rectArray[CurrentId] = data;
            CurrentId++;
        }

        private void FillColor(int4 sourceRect, int4 destinationRect, Texture2D sourceTexture)
        {
            float4 sourceUV = new float4();
            sourceUV.xz = (float2)(sourceRect.xz) / sourceTexture.width;
            sourceUV.yw = (float2)(sourceRect.yw) / sourceTexture.height;
            float4 destinationUV = new float4();
            destinationUV.xz = (float2)(destinationRect.xz) / _mTexture.width;
            destinationUV.yw = (float2)(destinationRect.yw) / _mTexture.height;

            _blitMaterial.SetTexture(MainTex, sourceTexture);
            _blitMaterial.SetVector(SrcTexSt, sourceUV);
            _blitMaterial.SetVector(Rect1, destinationUV);
            Graphics.Blit(sourceTexture, _mTexture, _blitMaterial);
        }
        
        public struct SpriteData
        {
            public float4 TillOffset;
            public float4 SizePivot;
        }
    }
}