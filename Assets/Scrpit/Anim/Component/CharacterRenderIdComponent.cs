using Unity.Entities;
using Unity.Rendering;

namespace Anim.RuntimeImage
{
    

    // 这个用于过滤
    public struct CharacterRenderIdComponent:ICleanupSharedComponentData
    {
        public int TypeId;
    }
    
    public struct CharacterRenderReqComp:IComponentData
    {
        public int TypeId;
    }
    
    // 这个用于标记状态 
    [MaterialProperty("_EquipIndexBufferIndex")]
    public struct CharacterRenderInstanceComponent:ICleanupComponentData
    {
        public int InstanceId;

        public CharacterRenderInstanceComponent(int i)
        {
            InstanceId = i;
        }

        public static implicit operator int(CharacterRenderInstanceComponent obj)
        {
            return obj.InstanceId;
        }
        
        public static implicit operator CharacterRenderInstanceComponent(int obj)
        {
            return new CharacterRenderInstanceComponent(obj);
        }
    }
    
}