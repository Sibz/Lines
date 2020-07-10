using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineKnotData : IBufferElementData
    {
        public float3 Knot;
    }
}