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

        public static void Join(
            BufferFromEntity<LineJoinPoint> buffer,
            NewJoinInfo joinInfo)
            => Join(buffer[joinInfo.FromSection], buffer[joinInfo.ToSection], joinInfo);


        public static void Join(
            DynamicBuffer<LineJoinPoint> fromBuffer,
            DynamicBuffer<LineJoinPoint> toBuffer,
            NewJoinInfo joinInfo)
        {
            Update(fromBuffer, joinInfo.FromIndex, joinInfo.ToSection, joinInfo.ToIndex);
            Update(toBuffer, joinInfo.ToIndex, joinInfo.FromSection, joinInfo.FromIndex);
        }

        private static void Update(DynamicBuffer<LineJoinPoint> buff, int fromIndex, Entity toEntity, int toIndex)
        {
            var join = buff[fromIndex];
            join.JoinData.ConnectedEntity = toEntity;
            join.JoinData.ConnectedIndex = toIndex;
            buff[fromIndex] = join;
        }

        public struct NewJoinInfo
        {
            public Entity FromSection;
            public Entity ToSection;
            public int FromIndex;
            public int ToIndex;
        }
    }
}