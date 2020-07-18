using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineCreateHeightBezierJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<NewLine> UpdatedNewLines;

        // We need the updated end positions
        [ReadOnly]
        public NativeArray<JoinPointPair> LineJoinPoints;

        public NativeArray<float2x4> HeightBeziers;

        public void Execute(int index)
        {
            var modifiers = UpdatedNewLines[index].Modifiers;
            var startPos  = LineJoinPoints[index].A.Pivot;
            var endPos    = LineJoinPoints[index].B.Pivot;
            var length    = math.distance(startPos, endPos);
            var point1    = new float2(0, modifiers.EndHeights.x);
            var point2    = new float2(length, modifiers.EndHeights.y);
            var controlPoint1 = new float2(length * modifiers.InnerDistances.x,
                                           math.lerp(point1.y, point2.y, modifiers.InnerDistances.x)
                                         + modifiers.InnerHeights.x);
            var controlPoint2 = new float2(length - length * modifiers.InnerDistances.y,
                                           math.lerp(point1.y, point2.y, 1 - modifiers.InnerDistances.y)
                                         + modifiers.InnerHeights.y);
            HeightBeziers[index] = new float2x4(point1, controlPoint1, controlPoint2, point2);
        }
    }
}