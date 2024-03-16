using Unity.Entities;
using Unity.Rendering;

namespace Anim.Shader
{
    [MaterialProperty("_EquipIndexBufferIndex")]
    public struct CharacterEquipIndexBufferIndexPropertyComp:IComponentData
    {
        public int Value;
    }
}