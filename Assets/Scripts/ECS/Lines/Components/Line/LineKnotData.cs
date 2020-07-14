using Sibz.Lines.ECS.Enums;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct LineKnotData : IBufferElementData
    {
        public float3        Position;
        public LineKnotFlags Flags;
    }
}