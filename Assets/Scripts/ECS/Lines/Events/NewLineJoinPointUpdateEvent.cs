using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineJoinPointUpdateEvent2 : IComponentData
    {
        public Entity JoinPoint;
        public Entity JoinTo;
        public float3 Position;
        public Entity LineEntity;

        public static Entity New(Entity lineEntity, Entity joinPoint, float3 position, Entity joinTo = default)
        {
            var entity = LineWorld.Em.CreateEntity(typeof(NewLineJoinPointUpdateEvent2));
            LineWorld.Em.SetComponentData(entity, new NewLineJoinPointUpdateEvent2
                                                  {
                                                      JoinPoint  = joinPoint,
                                                      Position   = position,
                                                      JoinTo     = joinTo,
                                                      LineEntity = lineEntity
                                                  });
            return entity;
        }
    }
}