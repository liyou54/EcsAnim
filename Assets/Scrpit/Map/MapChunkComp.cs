using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Map
{
    public class MapChunkComp:IComponentData
    {
        public int ChunkSize;
        public int4 UpdateChunkArea;
        public NativeParallelMultiHashMap<int2, Entity> FrontDynamicEntityMap; 
        public NativeParallelMultiHashMap<int2, Entity> BackDynamicEntityMap; 
    }
}