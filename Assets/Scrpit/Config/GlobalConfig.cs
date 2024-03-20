using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Serialization;
using DATA_OFFSET = System.Int32;
using DATA_POOL = System.IntPtr;
using TYPE_HASH = System.Int32;


public interface ITableCol
{
}

public struct GlobalConfig
{
    private static readonly SharedStatic<GlobalConfig> SharedStatic =
        SharedStatic<GlobalConfig>.GetOrCreate<GlobalConfig>();

    private static NativeParallelHashMap<int, NativeParallelHashMap<int, DATA_OFFSET>> DataDictionary => SharedStatic.Data._dataDictionary;
    private static NativeParallelHashMap<int, TableData> DataList => SharedStatic.Data._data;

    private static void TryInit()
    {
        if (SharedStatic.Data._dataDictionary.IsCreated)
        {
            return;
        }

        SharedStatic.Data._dataDictionary = new NativeParallelHashMap<int, NativeParallelHashMap<int, DATA_OFFSET>>(64, Allocator.Persistent);
        SharedStatic.Data._data = new NativeParallelHashMap<int, TableData>(64, Allocator.Persistent);
    }

    public static void AddTable<T>(int capacity) where T : struct, ITableCol
    {
        TryInit();
        SharedStatic.Data.AddTable_Internal<T>(capacity);
    }

    public static void AddOrUpdateData<T>(T data, int id) where T : struct, ITableCol
    {
        TryInit();
        SharedStatic.Data.AddOrUpdateData_Internal(data, id);
    }

    public static T TryGetData<T>(int id) where T : struct, ITableCol
    {
        TryInit();
        return SharedStatic.Data.TryGetData_Internal<T>(id);
    }

    public static NativeArray<T> GetAllData<T>() where T : struct, ITableCol
    {
        TryInit();
        return SharedStatic.Data.GetAllData_Internal<T>();
    }

    private void AddTable_Internal<T>(int capacity = 64) where T : struct, ITableCol
    {
        var hashMap = new NativeParallelHashMap<int, DATA_OFFSET>(capacity, Allocator.Persistent);
        var hashList = new TableData(UnsafeUtility.SizeOf(typeof(T)), UnsafeUtility.AlignOf<T>());
        DataDictionary.Add(typeof(T).GetHashCode(), hashMap);
        DataList.Add(typeof(T).GetHashCode(), hashList);
    }

    private bool TryGetOrAddCollect_Internal<T>(out NativeParallelHashMap<int, DATA_OFFSET> hashMap,
        out TableData tableData) where T : struct, ITableCol
    {
        var guid = typeof(T).GetHashCode();
        if (!DataList.TryGetValue(guid, out tableData))
        {
            AddTable_Internal<T>();
            DataList.TryGetValue(guid, out tableData);
        }

        tableData = DataList[guid];
        hashMap = DataDictionary[guid];
        return true;
    }

    private void AddOrUpdateData_Internal<T>(T data, int id) where T : struct, ITableCol
    {
        unsafe
        {
            TryGetOrAddCollect_Internal<T>(out var hashMap, out var tableData);
            if (hashMap.TryGetValue(id, out var offset))
            {
                tableData.ChangeData(data, offset);
            }
            else
            {
                tableData.AddData(data);
                hashMap.Add(id, tableData.NextUseIndex - 1);
                // 结构体 修改完成需要写回
                var nativeParallelHashMap = DataList;
                nativeParallelHashMap[typeof(T).GetHashCode()] = tableData;
            }
        }
    }

    private NativeArray<T> GetAllData_Internal<T>() where T : struct, ITableCol
    {
        var ok = TryGetOrAddCollect_Internal<T>(out var hashMap, out var list);
        if (!ok)
        {
            return default;
        }

        return list.GetAllData<T>();
    }

    private T TryGetData_Internal<T>(int id) where T : struct, ITableCol
    {
        TryGetOrAddCollect_Internal<T>(out var hashMap, out var list);
        if (hashMap.TryGetValue(id, out var index))
        {
            var data = list.GetData<T>(index);
            return data;
        }

        return default;
    }


    private struct TableData
    {
        public int DataSize;
        public int DataSizeAlign;
        public int NextUseIndex;
        public int Capacity;
        public DATA_POOL Data;

        public TableData(int size, int align, int defaultCapacity = 64)
        {
            unsafe
            {
                DataSize = size;
                DataSizeAlign = align;
                NextUseIndex = 0;
                Capacity = 64;
                var voidPtr = UnsafeUtility.Malloc(size * Capacity, align, Allocator.Persistent);
                Data = new IntPtr(voidPtr);
            }
        }

        public void AddData<T>(T data) where T : struct, ITableCol
        {
            unsafe
            {
                if (NextUseIndex >= Capacity)
                {
                    var currentSize = Capacity;
                    Capacity *= 2;
                    var voidPtr = UnsafeUtility.Malloc(DataSize * Capacity, DataSizeAlign, Allocator.Persistent);
                    UnsafeUtility.MemCpy(voidPtr, Data.ToPointer(), DataSize * currentSize);
                    UnsafeUtility.Free(Data.ToPointer(), Allocator.Persistent);
                    Data = new IntPtr(voidPtr);
                }

                UnsafeUtility.CopyStructureToPtr(ref data, IntPtr.Add(Data, NextUseIndex * DataSize).ToPointer());
                NextUseIndex++;
            }
        }

        public T GetData<T>(int index) where T : struct, ITableCol
        {
            unsafe
            {
                if (index >= NextUseIndex)
                {
                    return default;
                }

                return UnsafeUtility.AsRef<T>(IntPtr.Add(Data, index * DataSize).ToPointer());
            }
        }

        public NativeArray<T> GetAllData<T>() where T : struct, ITableCol
        {
            unsafe
            {
                var result = new NativeArray<T>(NextUseIndex, Allocator.Temp);
                UnsafeUtility.MemCpy(result.GetUnsafePtr(), Data.ToPointer(), NextUseIndex * DataSize);
                return result;
            }
        }

        public void ChangeData<T>(T data, int index) where T : struct, ITableCol
        {
            unsafe
            {
                if (index >= NextUseIndex)
                {
                    return;
                }

                UnsafeUtility.CopyStructureToPtr(ref data, IntPtr.Add(Data, index * DataSize).ToPointer());
            }
        }
    }


    private NativeParallelHashMap<TYPE_HASH, TableData> _data;
    private NativeParallelHashMap<TYPE_HASH, NativeParallelHashMap<int, DATA_OFFSET>> _dataDictionary;
}