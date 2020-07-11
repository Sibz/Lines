using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineJoinPoint : IBufferElementData
    {
        public float3 Direction;
        public float3 Position;
        public LineJoinData JoinData;
        public bool IsJoined => JoinData.IsConnected;
    }
}