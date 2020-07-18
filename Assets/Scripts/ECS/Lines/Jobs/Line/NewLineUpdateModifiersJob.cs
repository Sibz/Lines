using Sibz.Lines.ECS.Components;
using Sibz.Lines.ECS.Events;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sibz.Lines.ECS.Jobs
{
    public struct NewLineUpdateModifiersJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> LineEntities;
        [ReadOnly]
        public NativeArray<JoinPointPair> LineJoinPoints;
        [ReadOnly]
        public NativeArray<NewLineUpdateEvent> UpdateEvents;
        [ReadOnly]
        public ComponentDataFromEntity<NewLine> NewLines;

        // In order to use the updating information in other jobs
        // we must provide an array, or the other jobs must run next frame
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<NewLine> UpdatedNewLines;

        public EntityCommandBuffer.Concurrent Ecb;

        public void Execute(int index)
        {
            var newLine = NewLines[LineEntities[index]];
            var modifiers = UpdateEvents[index].Modifiers;

            if (!LineJoinPoints[index].A.IsJoined)
            {
                newLine.Modifiers.EndHeights.x = modifiers.EndHeights.x;
            }

            if (!LineJoinPoints[index].B.IsJoined)
            {
                newLine.Modifiers.EndHeights.y = modifiers.EndHeights.y;
            }

            newLine.Modifiers.InnerHeights.x += modifiers.InnerHeights.x;
            newLine.Modifiers.InnerHeights.y += modifiers.InnerHeights.y;

            Ecb.SetComponent(index, LineEntities[index], newLine);

            UpdatedNewLines[index] = newLine;
        }
    }
}