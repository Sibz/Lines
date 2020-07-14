
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineUpdateEvent : IComponentData
    {
        public Entity JoinPoint;
        public Entity JoinTo;
        public float3 Position;
        public Entity LineEntity;
        public bool UpdateJoinPoints;

        public static Entity New(Entity lineEntity, Entity joinPoint, float3 position, Entity joinTo = default)
        {
            Entity entity = LineWorld.Em.CreateEntity(typeof(NewLineUpdateEvent));
            LineWorld.Em.SetComponentData(entity, new NewLineUpdateEvent
            {
                JoinPoint = joinPoint,
                Position = position,
                JoinTo = joinTo,
                LineEntity = lineEntity,
                UpdateJoinPoints = true,
            });
            return entity;
        }

        public static Entity New(Entity lineEntity)
        {
            Entity ent =  LineWorld.Em.CreateEntity(typeof(NewLineUpdateEvent));
            LineWorld.Em.SetComponentData(ent, new NewLineUpdateEvent
            {
                 LineEntity = lineEntity
            });
            return ent;
        }
    }
}