using Unity.Entities;

namespace Scrpit.SystemGroup
{
    [UpdateBefore(typeof(ViewGroup))]
    public partial class LogicGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(LogicGroup), OrderLast = true)]
    public partial class EndLogicGroupECBS : EntityCommandBufferSystem
    {
    }

    public partial class ViewGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(ViewGroup), OrderLast = true)]
    public partial class EndViewGroupECBS : EntityCommandBufferSystem
    {
    }
}