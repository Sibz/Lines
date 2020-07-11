using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineToolUpdateEvent : IComponentData
    {
        public Entity JoinHoldingEntity;
        public int JoinIndex;
        public float3 Position;

        public static void New(float3 position, Entity joinHoldingEntity = default, int joinIndex = 0)
        {
            var entity = LineDataWorld.World.EntityManager.CreateEntity();
            LineDataWorld.World.EntityManager.AddComponentData(entity, new LineToolUpdateEvent
            {
                JoinHoldingEntity = joinHoldingEntity,
                Position =  position,
                JoinIndex = joinIndex
            });
        }
    }
}