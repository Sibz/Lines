using Sibz.Lines.ECS.Components;
using Sibz.Math;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineToolUpdateKnotsJob : IJob
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<LineTool> ToolData;
        public NativeArray<bool> DidChange;
        public LineProfile LineProfile;
        public DynamicBuffer<LineKnotData> KnotData;

        public void Execute()
        {
            KnotData.Clear();

            float3x3 b1 = ToolData[0].Data.Bezier1;
            float3x3 b2 = ToolData[0].Data.Bezier2;

            GetKnotsForBezier(b1);

            if (!b1.c2.IsCloseTo(b2.c2, LineProfile.KnotSpacing))
            {
                GetKnotsForBezier(new float3x3(b1.c2, math.lerp(b1.c2, b2.c2, 0.5f), b2.c2));
            }

            GetKnotsForBezier(b2, true);
            DidChange[0] = true;
        }

        private void GetKnotsForBezier(float3x3 b, bool invert = false)
        {
            float lineDistance =
                (math.distance(b.c0, b.c1) + math.distance(b.c1, b.c2) + math.distance(b.c0, b.c2)) / 2;

            int numberOfKnots = (int) math.ceil(lineDistance / LineProfile.KnotSpacing);
            for (int i = 0; i < numberOfKnots; i++)
            {
                float t = (float) i / (numberOfKnots - 1);
                float3 p = Bezier.Bezier.GetVectorOnCurve(b, invert ? 1f - t : t);
                // Avoid duplicates
                if (KnotData.Length == 0 ||
                    !p.IsCloseTo(KnotData[KnotData.Length - 1].Position, 0.01f))
                {
                    KnotData.Add(new LineKnotData
                    {
                        Position = p
                    });
                }
            }
        }
    }
}