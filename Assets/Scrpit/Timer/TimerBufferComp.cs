using Unity.Entities;

namespace Scrpit.Timer
{
    
    public  struct TimingType
    {
        public static int Operation = 1;
    }
    
    public struct TimingBufferComp:IEnableableComponent,IBufferElementData
    {
        public float Time;
        public int Type;
    }
}