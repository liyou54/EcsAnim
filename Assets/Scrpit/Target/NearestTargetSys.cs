using Scrpit.Operation;
using Scrpit.SystemGroup;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Map
{
    [UpdateInGroup(typeof(LogicGroup) , OrderLast = true)]
    public partial class NearestTargetSys : SystemBase
    {
        public partial struct NearestTargetJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<int2, Entity> CurrentDynamicEntityMap;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<BattleTeamComp> TeamLookup;

            public void Execute(ref TargetEntityComp targetEntity,in OperationGoalComp operationGoalComp, in BattleTeamComp teamComp, in LocalToWorld localToWorld)
            {

                if (operationGoalComp.GoalOperationType == OperationType.Idle)
                {
                    targetEntity.Entity = Entity.Null;
                    return;
                }
                
                if (targetEntity.Entity != null && EntityStorageInfoLookup.Exists(targetEntity.Entity))
                {
                    return;
                }
            
                targetEntity.Entity = Entity.Null;
                var chunkId = MapChunkSys.GetChunkIndex(localToWorld.Position, 8);
                Entity nearestTarget = Entity.Null;
                float nearestDistance = float.MaxValue;
                for (int i = chunkId.x - 1; i <= chunkId.x + 1; i++)
                {
                    for (int j = chunkId.y - 1; j <= chunkId.y + 1; j++)
                    {
                        var chunkIndex = new int2(i, j);
                        
                        foreach (var entity in CurrentDynamicEntityMap.GetValuesForKey(chunkIndex))
                        {
                            if (!EntityStorageInfoLookup.Exists(entity)
                                || !LocalToWorldLookup.TryGetComponent(entity, out var targetLocalToWorld)
                                || !TeamLookup.TryGetComponent(entity, out var targetTeam)
                                || targetTeam.TeamId == teamComp.TeamId)
                            {
                                continue;
                            }

                            if (nearestTarget == Entity.Null)
                            {
                                nearestTarget = entity;
                            }

                            if (nearestDistance > math.distance(localToWorld.Position, targetLocalToWorld.Position))
                            {
                                nearestTarget = entity;
                                nearestDistance = math.distance(localToWorld.Position, targetLocalToWorld.Position);
                            }
                        }
                    }
                }
                targetEntity.Entity = nearestTarget;
            }
        }

        protected override void OnUpdate()
        {
            var mapChunkComp = MapChunkSys.GetMapChunkComponentData();
            var entityLookup = GetEntityStorageInfoLookup();
            var localToWorldLookup = GetComponentLookup<LocalToWorld>(true);
            var teamLookup = GetComponentLookup<BattleTeamComp>(true);
            var entityChunkHash = mapChunkComp.FrontDynamicEntityMap;

            var job = new NearestTargetJob
            {
                CurrentDynamicEntityMap = entityChunkHash,
                EntityStorageInfoLookup = entityLookup,
                LocalToWorldLookup = localToWorldLookup,
                TeamLookup = teamLookup
            };

            Dependency = job.ScheduleParallel(Dependency);
        }
    }
}