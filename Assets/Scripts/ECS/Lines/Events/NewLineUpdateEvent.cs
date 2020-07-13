using Sibz.Lines.ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineUpdateEvent : IComponentData
    {
        public Entity JoinPoint;
        public Entity JoinTo;
        public float3 Position;
        public bool HasData;

        public static Entity New(Entity joinPoint, float3 position, Entity joinTo = default)
        {
            Entity entity = LineWorld.Em.CreateEntity(typeof(NewLineUpdateEvent));
            LineWorld.Em.SetComponentData(entity, new NewLineUpdateEvent
            {
                JoinPoint = joinPoint,
                Position = position,
                JoinTo = joinTo,
                HasData = true
            });
            return entity;
        }

        public static Entity New(Entity lineEntity)
        {
            var ent =  LineWorld.Em.CreateEntity(typeof(NewLineUpdateEvent));
            var line = LineWorld.Em.GetComponentData<Line>(lineEntity);
            LineWorld.Em.SetComponentData(ent, new NewLineUpdateEvent
            {
                JoinPoint = line.JoinPointA
            });
            return ent;
        }
    }
}