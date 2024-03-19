using Unity.Entities;
using Unity.Rendering;

namespace Anim.Shader
{
    [MaterialProperty("_AnimationIndex")]
    public struct CharacterAnimationIndexPropertyComp:IComponentData
    {
       public  int Value;
    }
}