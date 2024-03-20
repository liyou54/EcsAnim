using Unity.Entities;

namespace Scrpit.Event
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class EventGroupSystem:ComponentSystemGroup

    {
    }
}