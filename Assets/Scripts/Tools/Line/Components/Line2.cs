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

        public static Entity New(EntityCommandBuffer.Concurrent em, int jobIndex, float3 position,
            EntityArchetype archetype, out Entity initialSection, out DynamicBuffer<LineJoinPoint> joinBuffer)
        {
            Entity entity = em.CreateEntity(jobIndex, archetype);
            em.AddComponent(jobIndex, entity, new Line2(position));
            initialSection = LineSection.NewLineSection(em, jobIndex, entity, position, out joinBuffer);
            return entity;
        }

        public static void GetSectionsForLine(
            Entity lineEntity, NativeMultiHashMap<Entity, SectionData> sectionsByEntity,
            out NativeList<SectionData> sections)
        {
            sections = new NativeList<SectionData>(Allocator.Temp);
            if (!sectionsByEntity.TryGetFirstValue(lineEntity, out SectionData item,
                out NativeMultiHashMapIterator<Entity> it))
            {
                return;
            }

            do
            {
                sections.Add(item);
            } while (sectionsByEntity.TryGetNextValue(out item, ref it));
        }

        public static void GetSectionsForLine(
            Entity lineEntity, NativeMultiHashMap<Entity, SectionData> sectionsByEntity,
            out NativeList<SectionData> sections,
            out NativeArray<Entity> sectionsEntities)
        {
            GetSectionsForLine(lineEntity, sectionsByEntity, out sections);

            sectionsEntities = new NativeArray<Entity>(sections.Length, Allocator.Temp);
            int len = sections.Length;
            for (int i = 0; i < len; i++)
            {
                sectionsEntities[i] = sections[i].Entity;
            }
        }

        public struct SectionData
        {
            public Entity Entity;
            public LineSection Section;
        }
    }
}