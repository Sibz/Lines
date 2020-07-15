using Sibz.Lines.ECS.Components;
using Sibz.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    [BurstCompile]
    public struct NewLineGetBezierJob : IJobParallelFor
    {
        public LineTool LineTool;

        [ReadOnly]
        public NativeArray<LineWithJoinPointData> LineWithJoinData;

        [NativeDisableParallelForRestriction]
        public NativeArray<BezierData> BezierData;

        public EntityCommandBuffer.Concurrent Ecb;

        [ReadOnly]
        public ComponentDataFromEntity<LineJoinPoint> JoinPoints;

        public void Execute(int index)
        {
            var modifiers = LineTool.Data.Modifiers;


            var jp1 = LineWithJoinData[index].JoinPointA;
            var jp2 = LineWithJoinData[index].JoinPointB;

            var pointAIsConnected = JoinPoints.Exists(jp1.JoinToPointEntity);
            var pointBIsConnected = JoinPoints.Exists(jp2.JoinToPointEntity);

            var pointA = jp1.Pivot;
            var pointB = jp2.Pivot;

            // Two curves  b1, b2
            float3x3 b1, b2;

            // c0 is the origin
            b1.c0 = pointA;
            b2.c0 = pointB;

            // If join points invalid or not connected initialise to facing other point
            jp1.Direction = jp1.Direction.Equals(float3.zero)
                         || float.IsNaN(jp1.Direction.x)
                         || !pointBIsConnected && !pointAIsConnected
                                ? Normalize(pointB - pointA)
                                : jp1.Direction;
            jp2.Direction = jp2.Direction.Equals(float3.zero)
                         || float.IsNaN(jp2.Direction.x)
                         || !pointAIsConnected && !pointBIsConnected
                                ? Normalize(pointA - pointB)
                                : jp2.Direction;

            var forwards = new float3x2(jp1.Direction, jp2.Direction);

            var distances = new float2(
                                       Distance(pointA, pointB, forwards.c0),
                                       Distance(pointB, pointA, forwards.c1));

            var scales = new float2(
                                    Scale(modifiers.From.Size, distances.x),
                                    Scale(modifiers.To.Size, distances.y));

            if (pointAIsConnected && !pointBIsConnected)
            {
                distances.y = 0f;
                b1.c1 = GetOrigin();
                jp2.Direction = Normalize(b1.c1 - b2.c0);
                b1.c2 = Target(b1.c1, b2.c0, distances.x, distances.y, scales.x, scales.y, modifiers.From.Ratio);
                b2.c2 = b2.c1 = b2.c0;
            }
            else if (!pointAIsConnected && pointBIsConnected)
            {
                distances.x = 0f;
                b2.c1       = GetEnd();
                // Do Rotate
                jp1.Direction = Normalize(b2.c1 - b1.c0);

                b2.c2 = Target(b2.c1, b1.c0, distances.y, distances.x, scales.y, scales.x, modifiers.To.Ratio);
                b1.c2 = b1.c1 = b1.c0;
            }
            else
            {
                if (!pointAIsConnected) jp1.Direction = Normalize(b2.c0 - b1.c0);

                if (!pointBIsConnected) jp2.Direction = Normalize(b1.c0 - b2.c0);

                b1.c1 = GetOrigin();
                b2.c1 = GetEnd();
                b1.c2 = Target(b1.c1, b2.c1, distances.x, distances.y, scales.x, scales.y, modifiers.From.Ratio);
                b2.c2 = Target(b2.c1, b1.c1, distances.y, distances.x, scales.y, scales.x, modifiers.To.Ratio);
            }

            BezierData[index] = new BezierData
                                {
                                    B1 = b1,
                                    B2 = b2
                                };

            Ecb.SetComponent(index, LineWithJoinData[index].Line.JoinPointA, jp1);
            Ecb.SetComponent(index, LineWithJoinData[index].Line.JoinPointB, jp2);

            float3 GetOrigin()
            {
                return GetControlPoint(forwards.c0, pointA, distances.x, scales.x, modifiers.From.Ratio);
            }

            float3 GetEnd()
            {
                return GetControlPoint(forwards.c1, pointB, distances.y, scales.y, modifiers.To.Ratio);
            }

            static float3 Target(float3 c1, float3 c2, float h1, float h2, float s1, float s2, float r)
            {
                var d  = Dist(c1, c2);
                var ht = h1 * s1 * (2 - r) + h2 * s2 * (2 - r);
                return c1 + Normalize(c2 - c1) * (ht > d ? d * (h1 * s1 * (2 - r) / ht) : h1 * s1 * (2 - r));
            }

            static float3 GetControlPoint(float3 f, float3 p1, float h, float s, float r)
            {
                return p1 + f * h * s * r;
            }

            static float Scale(float units, float distance)
            {
                return math.min(units / distance, 1);
            }

            static float Distance(float3 p1, float3 p2, float3 forwards)
            {
                var angle = AngleD(forwards, Normalize(p2 - p1));
                angle = angle > 90 ? 90 + (angle - 90) / 2f : angle;
                return Abs(Dist(p2, p1) / SinD(180 - 2 * angle) * SinD(angle));
            }

            static float Abs(float a)
            {
                return math.abs(a);
            }

            static float3 Normalize(float3 a)
            {
                return math.normalize(a);
            }

            static float Dist(float3 a, float3 b)
            {
                return math.distance(a, b);
            }

            static float SinD(float a)
            {
                return math.sin(math.PI / 180 * a);
            }

            static float AngleD(float3 a, float3 b)
            {
                return Mathematics.AngleDegrees(a, b);
            }
        }
    }
}