using System.Collections.Generic;
using Anim.RuntimeImage.DeleteSystem;
using Sirenix.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Scrpit.Timer
{
    [UpdateInGroup(typeof(EntityLifeSystemGroup)), UpdateAfter(typeof(TimerECBS))]
    public partial class TimerSystem : SystemBase
    {
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        public partial struct TimeUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter TimerEcb;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, ref DynamicBuffer<TimingBufferComp> buffer)
            {
                if (buffer.Length == 0)
                {
                    TimerEcb.SetComponentEnabled<TimingBufferComp>(index, entity, false);
                    return;
                }

                TimerEcb.SetComponentEnabled<TimingBufferComp>(index, entity, true);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    if (buffer[i].Time <= 0)
                    {
                        buffer.RemoveAt(i);
                    }
                    else
                    {
                        buffer[i] = new TimingBufferComp
                        {
                            Time = buffer[i].Time - DeltaTime,
                            Type = buffer[i].Type
                        };
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            var deltaTime = World.Time.DeltaTime;
            var ecb = TimerECBS.CreateECB();
            var job = new TimeUpdateJob
            {
                DeltaTime = deltaTime,
                TimerEcb = ecb.AsParallelWriter()
            };
            Dependency = job.ScheduleParallel(Dependency);
            TimerECBS.GetSystem().AddJobHandleForProducer(Dependency);
        }
    }
}