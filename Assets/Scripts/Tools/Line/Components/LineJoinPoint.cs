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

        public bool Equals(LineJoinPoint other)
        {
            return
                Direction.Equals(other.Direction) &&
                Position.Equals(other.Position) &&
                JoinData.ConnectedEntity.Equals(other.JoinData.ConnectedEntity) &&
                JoinData.ConnectedIndex.Equals(other.JoinData.ConnectedIndex);
        }
    }
}