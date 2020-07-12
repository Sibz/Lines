using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineEvent : IComponentData
    {
        public Entity FromJoinPointEntity;
        public float3 StartingPosition;

        public static void New(float3 position, Entity fromJoinPointEntity = default)
        {
            Entity entity = LineWorld.Em.CreateEntity(typeof(NewLineEvent));
            LineWorld.Em.SetComponentData(entity, new NewLineEvent
            {
                StartingPosition = position,
                FromJoinPointEntity = fromJoinPointEntity
            });
        }
    }
}