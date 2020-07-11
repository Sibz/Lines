using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct Line2 : IComponentData
    {
        public static EntityArchetype LineArchetype =
            LineDataWorld.World.EntityManager.CreateArchetype(
                typeof(Line2),
                typeof(LineKnotData),
                typeof(LineMeshTriangleData),
                typeof(LineMeshVertexData),
                typeof(NewLine));

        public float3 Position;

        public Line2(float3 position)
        {
            Position = position;
        }

        public static Entity New(EntityCommandBuffer.Concurrent em, int jobIndex, float3 position, EntityArchetype archetype, out Entity initialSection)
        {
            Entity entity = em.CreateEntity(jobIndex, archetype);
            em.AddComponent(jobIndex, entity, new Line2(position));
            initialSection = LineSection.NewLineSection(em, jobIndex, entity, position);
            return entity;
        }
    }
}