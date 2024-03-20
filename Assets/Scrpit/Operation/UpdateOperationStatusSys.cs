using Scrpit.Event;
using Scrpit.Move;
using Scrpit.Operation;
using Scrpit.SystemGroup;
using Scrpit.Timer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

    [UpdateInGroup(typeof(LogicGroup))]
    public partial struct UpdateOperationStatusSys : ISystem
    {
        public ComponentLookup<TargetEntityComp> TargetEntityCompLook;
        public ComponentLookup<LocalToWorld> LocalToWorldLook;
        public EntityStorageInfoLookup TargetEntityStorageInfoLookup;
        public BufferLookup<TimingBufferComp> TimeBufferLookUp;

        public partial struct UpdateOperationTargetPosJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EventEcb;
            [ReadOnly] public ComponentLookup<TargetEntityComp> TargetEntityCompLook;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLook;
            [ReadOnly] public EntityStorageInfoLookup TargetEntityStorageInfoLookup;
            [ReadOnly] public BufferLookup<TimingBufferComp> TimeBufferLookUp;

            public EntityCommandBuffer.ParallelWriter TimerEcb;

            public enum OperationStatus
            {
                None,
                Doing,
                Finish
            }

            private OperationStatus GetOperationStatus(in Entity entity)
            {
                var operationStatus = OperationStatus.None;

                if (!TimeBufferLookUp.HasBuffer(entity))
                {
                    return operationStatus;
                }

                var timeBuffer = TimeBufferLookUp[entity];

                foreach (var time in timeBuffer)
                {
                    if (time.Type == TimingType.Operation)
                    {
                        if (time.Time < 0)
                        {
                            operationStatus = OperationStatus.Finish;
                        }
                        else
                        {
                            operationStatus = OperationStatus.Doing;
                        }
                    }
                }

                return operationStatus;
            }

            private void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                ref OperationTargetPositionComp operationPositionComp,
                ref MovePositionComp movePositionComp,
                ref OperationCurrentComp operationCurrentComp,
                in OperationRangeComp operationRangeComp,
                in OperationGoalComp operationGoalComp,
                in Entity entity)
            {
                var status = GetOperationStatus(entity);

                if (status == OperationStatus.Finish && operationCurrentComp.CurrentOperationType == OperationType.Attack)
                {
                    EventClearSystem.CreateEvent(EventEcb, entity, 0, EventTypeList.OperationChange);
                    operationCurrentComp.CurrentOperationType = OperationType.Idle;
                    return;
                }

                if (operationCurrentComp.CurrentOperationType == operationGoalComp.GoalOperationType)
                {
                    return;
                }

                if (operationGoalComp.GoalOperationType == OperationType.Idle && operationCurrentComp.CurrentOperationType != OperationType.Idle)
                {
                    EventClearSystem.CreateEvent(EventEcb, entity, 0, EventTypeList.OperationChange);
                    operationCurrentComp.CurrentOperationType = OperationType.Idle;
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

                var currentPos = LocalToWorldLook.GetRefRO(entity).ValueRO.Position.xy;
                // 检测是否到达目标位置
                if (math.distance(operationPositionComp.Position, currentPos) < operationRangeComp.Range)
                {
                    EventClearSystem.CreateEvent(EventEcb, entity, entityInQueryIndex, EventTypeList.OperationChange);
                    operationCurrentComp.CurrentOperationType = operationGoalComp.GoalOperationType;
                    TimerEcb.AddBuffer<TimingBufferComp>(entityInQueryIndex, entity);
                    TimerEcb.AppendToBuffer(entityInQueryIndex, entity, new TimingBufferComp
                    {
                        Time = .83f,
                        Type = TimingType.Operation
                    });
                    operationPositionComp.Position = currentPos;
                }
                else if (operationCurrentComp.CurrentOperationType != OperationType.Move)
                {
                    EventClearSystem.CreateEvent(EventEcb, entity, entityInQueryIndex, EventTypeList.OperationChange);
                    operationCurrentComp.CurrentOperationType = OperationType.Move;
                }

                movePositionComp.Position = operationPositionComp.Position;
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            TargetEntityCompLook = state.GetComponentLookup<TargetEntityComp>(true);
            LocalToWorldLook = state.GetComponentLookup<LocalToWorld>(true);
            TargetEntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
            TimeBufferLookUp = state.GetBufferLookup<TimingBufferComp>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            TargetEntityCompLook.Update(ref state);
            LocalToWorldLook.Update(ref state);
            TargetEntityStorageInfoLookup.Update(ref state);
            TimeBufferLookUp.Update(ref state);

            var ecb = EventECBS.CreateECB().AsParallelWriter();
            var timerEcbs = TimerECBS.CreateECB().AsParallelWriter();

            var updateTargetJob = new UpdateOperationTargetPosJob
            {
                TargetEntityCompLook = TargetEntityCompLook,
                LocalToWorldLook = LocalToWorldLook,
                TargetEntityStorageInfoLookup = TargetEntityStorageInfoLookup,
                EventEcb = ecb,
                TimerEcb = timerEcbs,
                TimeBufferLookUp = TimeBufferLookUp
            };
            state.Dependency = updateTargetJob.ScheduleParallel(state.Dependency);
            var system = EventECBS.GetSystem();
            var system2 = TimerECBS.GetSystem();
            system2.AddJobHandleForProducer(state.Dependency);
            system.AddJobHandleForProducer(state.Dependency);
        }
    }
}