using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Anim.RuntimeImage
{
    [MaterialProperty("_BaseColor")]
    public struct CharacterBaseColorPropertyComp:IComponentData
    {
        public float4 Value;
    }
}