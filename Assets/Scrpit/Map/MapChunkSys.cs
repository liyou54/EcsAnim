using Scrpit.SystemGroup;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Map
{
    [UpdateInGroup(typeof(LogicGroup),OrderFirst =  true)]
    public partial class MapChunkSys : SystemBase
    {
        public partial struct CalcEntityJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter BackDynamicEntityMap;
            public int ChunkSize;

            public void Execute(Entity entity, in LocalToWorld translation)
            {
                BackDynamicEntityMap.Add(GetChunkIndex(translation.Position, ChunkSize), entity);
            }
        }

        public partial struct SwapJob : IJobEntity
        {
            public void Execute(MapChunkComp mapChunkComp)
            {
                (mapChunkComp.FrontDynamicEntityMap, mapChunkComp.BackDynamicEntityMap) = (mapChunkComp.BackDynamicEntityMap, mapChunkComp.FrontDynamicEntityMap);
            }
        }

        public static MapChunkComp GetMapChunkComponentData()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<MapChunkSys>();
            var mapChunkComponentData = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<MapChunkComp>(system);
            return mapChunkComponentData;
        }

        public static int2 GetChunkIndex(float3 position, float chunkSize)
        {
            return new int2((int)(position.x / chunkSize), (int)(position.y / chunkSize));
        }

        public static float2 GetChunkCenter(float3 position, int chunkSize)
        {
            return new float2((int)(position.x / chunkSize) * chunkSize, (int)(position.y / chunkSize) * chunkSize) + chunkSize / 2;
        }

        protected override void OnCreate()
        {
            var system = World.GetExistingSystem<MapChunkSys>();
            var mapChunkComponentData = new MapChunkComp();
            mapChunkComponentData.BackDynamicEntityMap = new NativeParallelMultiHashMap<int2, Entity>(0, Allocator.TempJob);
            mapChunkComponentData.FrontDynamicEntityMap = new NativeParallelMultiHashMap<int2, Entity>(0, Allocator.TempJob);
            mapChunkComponentData.ChunkSize = 8;
            EntityManager.AddComponent<MapChunkComp>(system);
            EntityManager.SetComponentData(system, mapChunkComponentData);
        }

        protected override void OnUpdate()
        {
            var query = GetEntityQuery(ComponentType.ReadOnly<BeTargetAbleTag>(), ComponentType.ReadOnly<LocalToWorld>());
            var count = query.CalculateEntityCount();
            var mapComp = GetMapChunkComponentData();

            (mapComp.FrontDynamicEntityMap, mapComp.BackDynamicEntityMap) = (mapComp.BackDynamicEntityMap, mapComp.FrontDynamicEntityMap);
            
            mapComp.BackDynamicEntityMap.Dispose();
            mapComp.BackDynamicEntityMap = new NativeParallelMultiHashMap<int2, Entity>(count, Allocator.TempJob);

            var calcEntityJob = new CalcEntityJob()
            {
                BackDynamicEntityMap = mapComp.BackDynamicEntityMap.AsParallelWriter(),
                ChunkSize = mapComp.ChunkSize
            };
            Dependency = calcEntityJob.ScheduleParallel(query, Dependency);
        }
    }
}