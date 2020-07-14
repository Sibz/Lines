using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Events
{
    public struct NewLineCreateEvent : IComponentData
    {
        public Entity FromJoinPointEntity;
        public float3 StartingPosition;

        public static void New(float3 position, Entity fromJoinPointEntity = default)
        {
            var entity = LineWorld.Em.CreateEntity(typeof(NewLineCreateEvent));
            LineWorld.Em.SetComponentData(entity, new NewLineCreateEvent
                                                  {
                                                      StartingPosition    = position,
                                                      FromJoinPointEntity = fromJoinPointEntity
                                                  });
        }
    }
}