using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineGetBoundsFromBezierJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<BezierData> BezierData;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float3x2> Bounds;

        public void Execute(int index)
        {
            var points = new float3x4
                         {
                             c0 = BezierData[index].B1.c0,
                             c1 = BezierData[index].B1.c1,
                             c2 = BezierData[index].B2.c0,
                             c3 = BezierData[index].B2.c2
                         };
            Bounds[index] = new float3x2
                            {
                                c0 =
                                {
                                    x = Min(points.c0.x, points.c1.x, points.c2.x, points.c3.x),
                                    y = Min(points.c0.y, points.c1.y, points.c2.y, points.c3.y),
                                    z = Min(points.c0.z, points.c1.z, points.c2.z, points.c3.z)
                                },

                                c1 =
                                {
                                    x = Max(points.c0.x, points.c1.x, points.c2.x, points.c3.x),
                                    y = Max(points.c0.y, points.c1.y, points.c2.y, points.c3.y),
                                    z = Max(points.c0.z, points.c1.z, points.c2.z, points.c3.z)
                                }
                            };;
        }

        private static float Min(float a, float b, float c, float d)
        {
            return math.min(a, math.min(b, math.min(c, d)));
        }

        private static float Max(float a, float b, float c, float d)
        {
            return math.max(a, math.max(b, math.max(c, d)));
        }
    }
}