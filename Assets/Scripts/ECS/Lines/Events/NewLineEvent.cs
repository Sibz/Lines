using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineEvent
    {
        public Entity FromJoinPoint;
        public float3 StartingPosition;
    }
}