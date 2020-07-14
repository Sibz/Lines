using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct BezierData : IComponentData
    {
        public float3x3 B1;
        public float3x3 B2;
    }
}