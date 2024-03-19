using Unity.Entities;

namespace Scrpit.Event
{
    public struct EventType
    {
        public int Id;

        public static implicit operator EventType(int value)
        {
            return new EventType { Id = value };
        }

        public static implicit operator int(EventType value)
        {
            return value.Id;
        }
    }

    public struct EventTypeList
    {
        public static EventType OperationChange = 1;
    }

    public struct EventComp : IComponentData
    {
        public Entity EventDispatcher;
    }

    public struct EventTypeComp : ISharedComponentData
    {
        public EventType EventTypeID;
    }
}