using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineEvent : IComponentData
    {
        public Entity FromJoinPointEntity;
        public float3 StartingPosition;
    }
}