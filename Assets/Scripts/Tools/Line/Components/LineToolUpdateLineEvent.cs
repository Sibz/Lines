using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineToolUpdateLineEvent : IComponentData
    {
        public Entity Section;
        public int JoinIndex;
        public float3 Position;

        public static void New(float3 position, Entity section = default, int joinIndex = 0)
        {
            var entity = LineDataWorld.World.EntityManager.CreateEntity();
            LineDataWorld.World.EntityManager.AddComponentData(entity, new LineToolUpdateLineEvent
            {
                Section = section,
                Position =  position,
                JoinIndex = joinIndex
            });
        }
    }
}