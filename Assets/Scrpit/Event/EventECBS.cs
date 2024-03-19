using Unity.Entities;

namespace Scrpit.Event
{
    [UpdateInGroup(typeof(EventGroupSystem), OrderLast = true)]
    public partial class EventECBS:EntityCommandBufferSystem
    {
    }
}