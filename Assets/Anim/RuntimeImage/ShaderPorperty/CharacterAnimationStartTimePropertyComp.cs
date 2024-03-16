using Unity.Entities;
using Unity.Rendering;

namespace Anim.Shader
{
    [MaterialProperty("_AnimationStartTime")]
    public struct CharacterAnimationStartTimePropertyComp:IComponentData
    {
        public  float Value;
    }
}