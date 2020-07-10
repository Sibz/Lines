using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines
{
    public struct LineSection : IComponentData
    {
        public static Entity NewLineSection(EntityCommandBuffer.Concurrent em,
            int jobIndex, Entity parentLine, float3 position)
            => NewLineSection(em, jobIndex, parentLine, position, out DynamicBuffer<LineJoinPoint> buffer);

        public static Entity NewLineSection(EntityCommandBuffer.Concurrent em,
            int jobIndex, Entity parentLine, float3 position, out DynamicBuffer<LineJoinPoint> buffer)
        {
            Entity entity = em.CreateEntity(jobIndex);
            em.AddComponent(jobIndex, entity, new LineSection
            {
                ParentLine = parentLine,
                Bezier = new float3x3(position,position,position)
            });
            buffer = em.AddBuffer<LineJoinPoint>(jobIndex,entity);
            buffer.Add(new LineJoinPoint { Position = position });
            buffer.Add(new LineJoinPoint { Position = position });
            return entity;
        }

        public Entity ParentLine;
        public float3x3 Bezier;
        public bool IsStraight => Bezier.c1.IsCloseTo(math.lerp(Bezier.c0, Bezier.c2, 0.5f));

        private readonly int hashCode;

        public LineSection(float3x3 bezier, Entity parentLine = default)
        {
            if (bezier.Equals(float3x3.zero))
            {
                throw new InvalidOperationException("Cannot create section from zero based float3x3");
            }

            hashCode = bezier.GetHashCode();
            Bezier = bezier;
            ParentLine = parentLine;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public struct GetLineSectionKnotsJob : IJob
        {
            public float KnotSpacing;
            public int JoinId;
            public LineSection Section;
            public float4x4 TransformMatrix;
            public NativeList<float3> Results;
            public void Execute()
            {
                if (JoinId > 1 || JoinId < 0)
                {
                    throw new InvalidOperationException("Invalid JoinId");
                }
                float3x3 bezier = Section.Bezier;
                if (JoinId==1)
                {
                    bezier.c0 = Section.Bezier.c2;
                    bezier.c2 = Section.Bezier.c0;
                }
                if (Section.IsStraight)
                {
                    Results.Add(bezier.c0);
                    Results.Add(bezier.c2);
                    return;
                }
                float distanceApprox = (math.distance(bezier.c0, bezier.c1) + math.distance(bezier.c2, bezier.c1) +
                                        math.distance(bezier.c0, bezier.c2)) / 2;
                int numberOfKnots = (int) math.ceil(distanceApprox / KnotSpacing);
                for (int i = 0; i < numberOfKnots; i++)
                {
                    float t = (float) i / (numberOfKnots - 1);
                    float3 worldKnot = Helpers.Bezier.GetVectorOnCurve(bezier, t);
                    Results.Add(TransformMatrix.MultiplyPoint(worldKnot));
                    //Debug.DrawLine(worldKnot, worldKnot + new float3(0, 1, 0), Color.blue, 0.05f);
                }
            }
        }

    }
}