using System.Runtime.CompilerServices;
using ICSharpCode.NRefactory.Ast;
using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
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
        public NativeArray<LineWithJoinPointData> LineWithJoinData;

        public NativeArray<float2x4> HeightBeziers;

        public void Execute(int index)
        {
            var modifiers = UpdatedNewLines[index].Modifiers;
            var startPos = LineWithJoinData[index].JoinPointA.Pivot;
            var endPos = LineWithJoinData[index].JoinPointB.Pivot;
            var length = math.distance(startPos, endPos);

            var point1 = new float2(0, startPos.y + modifiers.EndHeights.x);
            var point2 = new float2(length, endPos.y+ modifiers.EndHeights.y);
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