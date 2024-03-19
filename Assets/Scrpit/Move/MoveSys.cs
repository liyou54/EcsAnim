using Map;
using Scrpit.SystemGroup;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Scrpit.Move
{
    [UpdateInGroup(typeof(LogicGroup))]
    public partial struct MoveSys : ISystem
    {
        public float DeltaTime;
        public ComponentLookup<MovePositionComp> MovePositionComps;
        public ComponentLookup<MoveSpeedComp> MoveSpeedComps;

        public partial struct MoveJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<MovePositionComp> MovePositionComps;
            [ReadOnly] public ComponentLookup<MoveSpeedComp> MoveSpeedComps;
            public float DeltaTime;

            public void Execute(ref LocalToWorld localToWorld, ref MoveStepComp moveStepComp, in Entity entity)
            {
                if (MovePositionComps.TryGetComponent(entity, out var movePos)
                    && MoveSpeedComps.TryGetComponent(entity, out var moveSpeed))
                {
                    var pos = localToWorld.Position.xy;
                    var targetPos = movePos.Position;
                    var speed = moveSpeed.Speed;
                    var dir = targetPos - pos;
                    var distance = math.length(dir);
                    if (distance > 0.1)
                    {
                        var moveDir = dir / distance;
                        var moveDistance = math.min(speed * DeltaTime, distance);
                        pos += moveDir * moveDistance;
                        moveStepComp.NextPos = pos;
                    }
                }

                if (!float.IsNaN(moveStepComp.NextPos.x) && !float.IsNaN(moveStepComp.NextPos.y))
                {
                    moveStepComp.LastPos = localToWorld.Position.xy;
                    localToWorld.Value.c3.xy = moveStepComp.NextPos;
                    moveStepComp.NextPos = float.NaN;
                }
            }
        }

        public void OnCreate(ref SystemState state)
        {
            MovePositionComps = state.GetComponentLookup<MovePositionComp>(true);
            MoveSpeedComps = state.GetComponentLookup<MoveSpeedComp>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            MoveSpeedComps.Update(ref state);
            MovePositionComps.Update(ref state);
            DeltaTime = SystemAPI.Time.DeltaTime;
            var moveJob = new MoveJob
            {
                MovePositionComps = MovePositionComps,
                MoveSpeedComps = MoveSpeedComps,
                DeltaTime = DeltaTime
            };
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
        }
    }
}