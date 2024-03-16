using Unity.Entities;

namespace Anim.RuntimeImage
{
    
    public enum CharacterRenderState
    {
        PreCreate,
        Worrking,
        Destroy,
    }
    
    // 这个用于过滤
    public struct CharacterRenderIdComponent:ISharedComponentData
    {
        public int TypeId;
    }
    
    // 这个用于标记状态
    public struct CharacterRenderStateComponent:ICleanupComponentData
    {
        public int TypeId;
        public int InstanceId;
        public CharacterRenderState State;
    }
    
    public struct ChangeEquipComponent:IBufferElementData
    {
        public int LastEquipIndex;
        public int NewEquipIndex;
        public int EquipType;
    }
    
}