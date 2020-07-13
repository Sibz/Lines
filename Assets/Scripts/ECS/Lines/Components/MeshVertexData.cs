using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Components
{
    public struct MeshVertexData : IBufferElementData
    {
        public float3 Position;
        public float3 Normal;
        public float2 Uv;
    }
}