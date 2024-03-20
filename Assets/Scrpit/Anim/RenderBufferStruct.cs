using System;
using Unity.Entities;
using UnityEngine;

namespace Anim.RuntimeImage
{
    public class BufferDataWithSize<T> where T : unmanaged
    {
        public ComputeBuffer buffer;
        public int UsedSize { get; private set; }
        public int MaxSize { get; private set; }
        private Action<ComputeBuffer> OnBufferSizeChange;
        public void ResetCount()
        {
            UsedSize = 0;
        }
        public BufferDataWithSize(int capacity, Action<ComputeBuffer> onBufferSizeChange)
        {
            unsafe
            {
                buffer = new ComputeBuffer(capacity, sizeof(T));
            }

            UsedSize = 0;
            MaxSize = capacity;
            OnBufferSizeChange = onBufferSizeChange;
        }
        public void AddData(T[] data)
        {
            if (data.Length + UsedSize > MaxSize)
            {
                var newMaxSize = (MaxSize + data.Length) * 2;

                // 拷贝数据
                var temp = new T[buffer.count];
                buffer.GetData(temp);
                buffer.Release();
                unsafe
                {
                    buffer = new ComputeBuffer(newMaxSize, sizeof(T));
                }
                buffer.SetData(temp);
                MaxSize = newMaxSize;
                OnBufferSizeChange?.Invoke(buffer);
            }
            buffer.SetData(data, 0, UsedSize, data.Length);
            UsedSize += data.Length;
        }
    }

    public struct AnimClipInfo
    {
        public int StartFrameTexIndex;
        public int FrameCount;
        public float Duration;
    }

    public struct AnimClipBlob
    {
        public int Id;
        public BlobString Name;
        public int StartFrameTexIndex;
        public int FrameCount;
        public float Duration;
    }
    

    public struct UpdateEquipBufferIndexCache
    {
        public int ObjIndex;
        public int SpriteIndex;
        public int SpriteCount;
        public int Value;

        public UpdateEquipBufferIndexCache(int objIndex, int spriteIndex, int spriteCount, int newValue)
        {
            ObjIndex = objIndex;
            SpriteIndex = spriteIndex;
            SpriteCount = spriteCount;
            Value = newValue;
        }
    }
    

    
    public struct UpdateEquipBufferIndex
    {
        public int IndexEquip;
        public int Value;

        public UpdateEquipBufferIndex(int indexEquip ,int value)
        {
            IndexEquip = indexEquip;
            Value = value;
        }
    }
}