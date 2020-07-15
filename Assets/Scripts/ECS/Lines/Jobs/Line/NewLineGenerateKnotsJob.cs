using Sibz.Lines.ECS.Components;
using Sibz.Math;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineGenerateKnotsJob : IJobParallelFor
    {
        public LineTool LineTool;

        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public NativeArray<LineWithJoinPointData> LineWithJoinData;

        [ReadOnly]
        public ComponentDataFromEntity<LineProfile> LineProfiles;

        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<BezierData> BezierData;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<LineKnotData> KnotData;

        [NativeDisableParallelForRestriction]
        private DynamicBuffer<LineKnotData> knotData;

        private Line        line;
        private LineProfile lineProfile;

        public void Execute(int index)
        {
            // If index is different, this means this line entity was already in the
            // array, there for this is a duplicate and we can skip it.
            if (LineEntities.IndexOf<Entity>(LineEntities[index]) != index) return;

            knotData = KnotData[LineEntities[index]];
            knotData.Clear();
            line = LineWithJoinData[index].Line;

            lineProfile = LineProfiles.Exists(line.Profile) ? LineProfiles[line.Profile] : LineProfile.Default();

            var b1 = BezierData[index].B1;
            var b2 = BezierData[index].B2;

            SetKnotsForBezier(b1);

            if (!b1.c2.IsCloseTo(b2.c2, lineProfile.KnotSpacing))
                SetKnotsForBezier(new float3x3(b1.c2, math.lerp(b1.c2, b2.c2, 0.5f), b2.c2));

            SetKnotsForBezier(b2, true);
        }

        private void SetKnotsForBezier(float3x3 b, bool invert = false)
        {
            var lineDistance =
                (math.distance(b.c0, b.c1) + math.distance(b.c1, b.c2) + math.distance(b.c0, b.c2)) / 2;

            var numberOfKnots = (int) math.ceil(lineDistance / lineProfile.KnotSpacing);
            for (var i = 0; i < numberOfKnots; i++)
            {
                var t = (float) i / (numberOfKnots - 1);
                var p = Bezier.Bezier.GetVectorOnCurve(b, invert ? 1f - t : t);
                // Avoid duplicates
                if (knotData.Length == 0 ||
                    !p.IsCloseTo(knotData[knotData.Length - 1].Position, 0.01f))
                    knotData.Add(new LineKnotData
                                 {
                                     Position = p
                                 });
            }
        }
    }
}