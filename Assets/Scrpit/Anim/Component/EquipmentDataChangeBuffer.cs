using Unity.Entities;

namespace Anim.RuntimeImage
{
    public struct EquipmentDataChangeBuffer:IBufferElementData
    {
        public int Position;
        public int NewId;
    }
}