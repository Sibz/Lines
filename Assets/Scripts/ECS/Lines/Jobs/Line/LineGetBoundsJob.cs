using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sibz.Lines.ECS.Jobs
{
    public struct LineGetBoundsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> Entities;
        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        public NativeArray<float3x2> Bounds;

        public void Execute(int index)
        {
            var line = Lines[Entities[index]];
            Bounds[index] = new float3x2(line.Position, line.BoundingBoxSize);
            Debug.LogFormat("Bounds for new merged line {0}", Bounds[index]);
        }
    }
}