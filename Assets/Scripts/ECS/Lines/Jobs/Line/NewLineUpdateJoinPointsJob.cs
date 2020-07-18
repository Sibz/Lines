using Sibz.Lines.ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineUpdateJoinPointsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;

        [ReadOnly]
        public ComponentDataFromEntity<Line> Lines;

        [ReadOnly]
        public NativeArray<JoinPointPair> LineJoinPoints;

        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            var line = Lines[LineEntities[index]];
            Ecb.SetComponent(index, line.JoinPointA, LineJoinPoints[index].A);
            Ecb.SetComponent(index, line.JoinPointB, LineJoinPoints[index].B);
        }
    }
}