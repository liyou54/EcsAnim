using Scrpit.Move;
using Scrpit.Operation;
using Scrpit.SystemGroup;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Map
{
    // 根据目标以及当前状态更新位置以操作
    // 1 存在目标
    // 1-1 当前行为与目标一致 continue
    // 1-2 当前行为与目标不一致 
    // 1-2-1 检测是否到达目标位置 
    // 1-2-2 未到达目标位置 更新位置
    // 1-2-3 到达目标位置 更新行为
    // 2 不存在目标 是否存在无目标的操作
    // 2-1 存在无目标的操作
    // 2-1-1 更新位置
    // 2-1-2 到达目标位置 更新行为
    // 2-2 不存在无目标的操作
    // 2-2-1 Idle

    // 必须要有的组件 :位置 目标类型 当前行为类型  目标位置 操作范围
    // 另外还可能需要的组件: 目标实体引用 
    // 最终更新: 当前行为类型 移动路径
    // 附带输出 行为变化事件

    [UpdateInGroup(typeof(LogicGroup), OrderLast = true)]
    public partial struct UpdateOperationStatusSys : ISystem
    {
        public ComponentLookup<TargetEntityComp> TargetEntityCompLook;
        public ComponentLookup<LocalToWorld> LocalToWorldLook;
        public EntityStorageInfoLookup TargetEntityStorageInfoLookup;


        public partial struct UpdateOperationTargetPosJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<TargetEntityComp> TargetEntityCompLook;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLook;
            [ReadOnly] public EntityStorageInfoLookup TargetEntityStorageInfoLookup;

            private void Execute(
                ref OperationPositionComp operationPositionComp,
                ref MovePositionComp movePositionComp,
                ref CurrentOperationComp currentOperationComp,
                in OperationRangeComp operationRangeComp,
                in OperationGoalComp operationGoalComp,
                in Entity entity)
            {
                if (currentOperationComp.CurrentOperationType == operationGoalComp.GoalOperationType)
                {
                    return;
                }

                if (operationGoalComp.GoalOperationType == OperationType.Idle)
                {
                    currentOperationComp.CurrentOperationType = OperationType.Idle;
                    return;
                }


                if (TargetEntityCompLook.TryGetComponent(entity, out var targetEntityComp))
                {
                    var targetEntity = targetEntityComp.Entity;
                    if (TargetEntityStorageInfoLookup.Exists(targetEntity))
                    {
                        var targetPosition = LocalToWorldLook.GetRefRO(targetEntity);
                        operationPositionComp.Position = targetPosition.ValueRO.Position.xy;
                    }
                }

                // 检测是否到达目标位置
                if (math.distance(operationPositionComp.Position, LocalToWorldLook.GetRefRO(entity).ValueRO.Position.xy) < operationRangeComp.Range)
                {
                    currentOperationComp.CurrentOperationType = operationGoalComp.GoalOperationType;
                    return;
                }

                movePositionComp.Position = operationPositionComp.Position;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            TargetEntityCompLook = state.GetComponentLookup<TargetEntityComp>(true);
            LocalToWorldLook = state.GetComponentLookup<LocalToWorld>(true);
            TargetEntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        }

        public void OnUpdate(ref SystemState state)
        {
            TargetEntityCompLook.Update(ref state);
            LocalToWorldLook.Update(ref state);
            TargetEntityStorageInfoLookup.Update(ref state);

            var updateTargetJob = new UpdateOperationTargetPosJob
            {
                TargetEntityCompLook = TargetEntityCompLook,
                LocalToWorldLook = LocalToWorldLook,
                TargetEntityStorageInfoLookup = TargetEntityStorageInfoLookup
            };
            state.Dependency = updateTargetJob.ScheduleParallel(state.Dependency);
        }
    }
}