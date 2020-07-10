using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineMeshVertexData : IBufferElementData
    {
        public float3 Vertex;
        public float3 Normal;
        public float2 Uv;
    }
}