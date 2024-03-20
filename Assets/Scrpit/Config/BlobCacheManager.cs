using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using TYPE_HASH = System.Int32;

namespace Scrpit.Config
{
    public interface IBlobCacheManager<TKey,T, T2> where T : unmanaged
    {
    }

    public struct BlobCacheManager<TKey,T> : 
        IBlobCacheManager<TKey,T, BlobAssetReference<T>> where T : unmanaged where TKey : unmanaged, IEquatable<TKey>
    {
        private static SharedStatic<BlobCacheManager<TKey,T>> _staticData =
            SharedStatic<BlobCacheManager<TKey,T>>.GetOrCreate<BlobCacheManager<TKey,T>>();

        private static void TryInit()
        {
            if (_staticData.Data._hashData.IsCreated)
            {
                return;
            }
            _staticData.Data._hashData = new NativeHashMap<TKey, BlobAssetReference<T>>(64, Allocator.Persistent);
        }
        
        public static void Add(TKey key, BlobAssetReference<T> data)
        {
            TryInit();
            _staticData.Data._hashData.Add(key,data);
        }
        
        public static bool TryGet(TKey key, out BlobAssetReference<T> data)
        {
            TryInit();
            return _staticData.Data._hashData.TryGetValue(key, out data);
        }
        
        private NativeHashMap<TKey, BlobAssetReference<T>> _hashData;
    }
}