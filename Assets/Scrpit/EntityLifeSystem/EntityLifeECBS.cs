using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    [UpdateInGroup(typeof(EntityLifeSystemGroup), OrderLast = true)]
    public partial class EntityLifeECBS:EntityCommandBufferSystem
    {
        
    }
}