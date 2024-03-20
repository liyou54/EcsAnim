using Anim.RuntimeImage;
using Anim.Shader;
using Scrpit.Config;
using Scrpit.Event;
using Scrpit.Operation;
using Scrpit.SystemGroup;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Scrpit.AnimationMachine
{
    [UpdateInGroup(typeof(ViewGroup))]
    public partial class AnimationStateSystem : SystemBase
    {
        public partial struct ChangeAnimJob : IJobEntity
        {
            [ReadOnly] public float CurrentTime;

            private void Execute([EntityIndexInQuery] int index,
                ref OperationHistoryComp historyComp,
                ref CharacterAnimationIndexPropertyComp charAim,
                ref CharacterAnimationStartTimePropertyComp charStartAnim,
                in OperationCurrentComp currentOperationComp)
            {
                if (currentOperationComp.CurrentOperationType == historyComp.OperationType)
                {
                    return;
                }

                charAim.Value = GetAnimIndexByOperationType(currentOperationComp.CurrentOperationType);
                charStartAnim.Value = CurrentTime;
                historyComp.OperationType = currentOperationComp.CurrentOperationType;
            }
        }


        protected override void OnCreate()
        {
        }

        protected static int GetAnimIndexByOperationType(OperationType operationType)
        {
            var name = "0_idle";
            if (operationType == OperationType.Attack)
            {
                name = "2_Attack_Bow";
            }

            if (operationType == OperationType.Move)
            {
                name = "1_Run";
            }

            var key = new AnimBlobKey(1, name);
            BlobCacheManager<AnimBlobKey, AnimClipBlob>.TryGet(key, out var blob);
            return blob.Value.Id;
        }


        protected override void OnUpdate()
        {
            var query = GetEntityQuery(
                ComponentType.ReadWrite<OperationHistoryComp>(),
                ComponentType.ReadWrite<CharacterAnimationIndexPropertyComp>(),
                ComponentType.ReadWrite<CharacterAnimationStartTimePropertyComp>(),
                ComponentType.ReadOnly<OperationCurrentComp>());
            query.ResetFilter();
            query.AddChangedVersionFilter(typeof(OperationCurrentComp));
            float currentTime = (float)World.Time.ElapsedTime;
            var job = new ChangeAnimJob
            {
                CurrentTime = currentTime,
            };
            Dependency = job.ScheduleParallel(query, Dependency);
        }
    }
}