using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineKnotData : IBufferElementData
    {
        public float3 Knot;
        public KnotFlags Flags;
    }

    public enum KnotFlags : ushort
    {
        None = 0,
        End = 1,
        Other = 2
    }
}