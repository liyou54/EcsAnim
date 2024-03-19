using Unity.Entities;

namespace Anim.RuntimeImage.DeleteSystem
{
    public enum EntityStatus
    {
        Init,
        Worrking,
        Destroy,
    }
    public struct EntityStatusComp : ICleanupSharedComponentData
    {
        public EntityStatus State;
    }
}